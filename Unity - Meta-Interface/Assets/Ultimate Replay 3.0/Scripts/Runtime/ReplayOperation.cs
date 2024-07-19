using System;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// The update method used by the replay manager for all recording and replaying samples.
    /// </summary>
    public enum ReplayUpdateMode
    {
        /// <summary>
        /// The user must manually update the replay operation.
        /// </summary>
        Manual = 0,
        /// <summary>
        /// Use the Update method.
        /// </summary>
        Update,
        /// <summary>
        /// Use the late update method.
        /// </summary>
        LateUpdate,
        /// <summary>
        /// Use the fixed update method.
        /// </summary>
        FixedUpdate,
    }

    /// <summary>
    /// Represents a dedicated replay operation in progress.
    /// Provides access to API's common to both recording and playback operations.
    /// </summary>
    public abstract class ReplayOperation : IDisposable
    {
        // Protected
        /// <summary>
        /// The replay manager instance.
        /// </summary>
        protected ReplayManager manager = null;
        /// <summary>
        /// The replay scene associated with this replay operation.
        /// </summary>
        protected ReplayScene scene = null;
        /// <summary>
        /// The replay storage associated with this replay operation.
        /// </summary>
        protected ReplayStorage storage = null;

        // Properties
        /// <summary>
        /// Check if this replay operation has been disposed.
        /// </summary>
        public abstract bool IsDisposed { get; }

        /// <summary>
        /// Get the <see cref="ReplayUpdateMode"/> for this replay operation.
        /// This value determines at what stage in the Unity game loop the replay operation is updated. 
        /// </summary>
        public abstract ReplayUpdateMode UpdateMode { get; }

        /// <summary>
        /// Get the replay scene associated with this replay operation.
        /// The replay scene contains information about all replay objects currently being recorded or replayed by this operation.
        /// Note that it is possible for multiple replay objects to appear in many different replay scenes.
        /// </summary>
        public ReplayScene Scene
        {
            get 
            {
                CheckDisposed();
                return scene;
            }
        }

        /// <summary>
        /// Get the replay storage associated with this replay operation.
        /// </summary>
        public ReplayStorage Storage
        {
            get 
            {
                CheckDisposed();
                return storage; 
            }
        }

        // Constructor
        /// <summary>
        /// Create a new replay operation.
        /// </summary>
        /// <param name="manager">The replay manager instance that will perform updates for this operation</param>
        /// <param name="scene">The replay scene associated with this replay operation</param>
        /// <param name="storage">The replay storage associated with this replay operation</param>
        /// <exception cref="ArgumentNullException">The specified replay manager is null</exception>
        protected ReplayOperation(ReplayManager manager, ReplayScene scene, ReplayStorage storage)
        {
            // Check for null manager
            if (manager == null && Application.isPlaying == true)
                throw new ArgumentNullException(nameof(manager));

            // Check for null scene
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            // Check for null storage
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));

            this.manager = manager;
            this.scene = scene;
            this.storage = storage;
        }

        // Methods
        /// <summary>
        /// Should be called with a delta time value to update the replay operation manually.
        /// Make sure that <see cref="UpdateMode"/> is set to <see cref="ReplayUpdateMode.Manual"/> to take full control over the update cycle.
        /// Delta time should be the amount of time in seconds since the last <see cref="ReplayTick(float)"/> call was made.
        /// Can be called multiple times per frame, but note that replay objects in the scene may not have moved since the last tick in this case.
        /// </summary>
        /// <param name="delta"></param>
        public abstract void ReplayTick(float delta);

        /// <summary>
        /// Dispose this replay operation.
        /// This will cause the operation to be stopped and this operation should no longer be used.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Should be called from Unity 'Update' method to update the replay operation.
        /// Will not do anything if <see cref="UpdateMode"/> is not set to <see cref="ReplayUpdateMode.Update"/>.
        /// See also <see cref="ReplayTick(float)"/> to update the operation manually.
        /// </summary>
        /// <param name="deltaTime">The amount of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public void ReplayTickUpdate(float deltaTime)
        {
            // Check for correct mode and update
            if (UpdateMode == ReplayUpdateMode.Update)
                ReplayTick(deltaTime);
        }

        /// <summary>
        /// Should be called from Unity 'LateUpdate' method to update the replay operation.
        /// Will not do anything if <see cref="UpdateMode"/> is not set to <see cref="ReplayUpdateMode.LateUpdate"/>.
        /// See also <see cref="ReplayTick(float)"/> to update the operation manually.
        /// </summary>
        /// <param name="deltaTime">The amount of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public void ReplayTickLateUpdate(float deltaTime)
        {
            // Check for correct mode and update
            if (UpdateMode == ReplayUpdateMode.LateUpdate)
                ReplayTick(deltaTime);
        }

        /// <summary>
        /// Should be called from Unity 'FixedUpdate' method to update the replay operation.
        /// Will not do anything if <see cref="UpdateMode"/> is not set to <see cref="ReplayUpdateMode.FixedUpdate"/>.
        /// See also <see cref="ReplayTick(float)"/> to update the operation manually.
        /// </summary>
        /// <param name="deltaTime">The amount of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public void ReplayTickFixedUpdate(float deltaTime)
        {
            // Check for correct mode and update
            if (UpdateMode == ReplayUpdateMode.FixedUpdate)
                ReplayTick(deltaTime);
        }

        /// <summary>
        /// Throw an exception if this replay operation has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The replay operation was disposed</exception>
        protected abstract void CheckDisposed();
    }
}
