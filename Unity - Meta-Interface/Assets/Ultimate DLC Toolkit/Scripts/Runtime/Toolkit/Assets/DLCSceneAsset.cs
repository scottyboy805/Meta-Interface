using DLCToolkit.Format;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DLCToolkit.Assets
{
    /// <summary>
    /// Represents a scene asset included in the DLC.
    /// Scene assets are simply Unity scenes.
    /// </summary>
    public sealed class DLCSceneAsset : DLCAsset
    {
        // Private
        private AsyncOperation pendingLoadRequest = null;

        // Constructor
        internal DLCSceneAsset(IDLCAsyncProvider asyncProvider, DLCContentBundle contentBundle, DLCLoadMode loadMode, int assetID, string fullName, string relativeName)
            : base(asyncProvider, contentBundle, loadMode, assetID, typeof(Scene), fullName, relativeName)
        {
            this.loadMode = loadMode;
        }

        // Methods
        /// <summary>
        /// Load the <see cref="DLCSceneAsset"/>. 
        /// </summary>
        /// <param name="loadSceneMode">The mode to use when loading the scene</param>
        public void Load(LoadSceneMode loadSceneMode)
        {
            Load(new LoadSceneParameters(loadSceneMode));
        }

        /// <summary>
        /// Load the <see cref="DLCSceneAsset"/>.
        /// </summary>
        /// <param name="loadSceneParameters">The load scene parameters to use when loading the scene</param>
        public void Load(LoadSceneParameters loadSceneParameters)
        {
            // Check for loadable
            CheckLoaded();

            // Check for bundle
            if(contentBundle.IsNotLoaded == true)
            {
                Debug.Log("Loading scene assets bundle on demand...");
                contentBundle.RequestLoad();
            }

            // Issue load request
            Debug.Log("Loading scene: " + relativeName);
            SceneManager.LoadScene(name, loadSceneParameters);
        }

        /// <summary>
        /// Load the <see cref="DLCSceneAsset"/> asynchronously. 
        /// </summary>
        /// <param name="loadSceneMode">Should the scene be additively loaded into the current scene</param>
        /// <param name="allowSceneActivation">Should the scene be activated as soon as it is loaded</param>
        public DLCAsync LoadAsync(LoadSceneMode loadSceneMode, bool allowSceneActivation = true)
        {
            // Check for loadable
            CheckLoaded();

            // Check for pending
            if (pendingLoadRequest != null)
                throw new InvalidOperationException("Cannot start a new load scene request because a pending request is waiting for activation. Use `ActivatePendingScene` to switch to the pending scene");


            // Create async
            DLCAsync async = new DLCAsync();

            // Create load request
            asyncProvider.RunAsync(LoadSceneFromBundleAsync(async, new LoadSceneParameters(loadSceneMode), allowSceneActivation));

            return async;
        }

        /// <summary>
        /// Load the <see cref="DLCSceneAsset"/> asynchronously. 
        /// </summary>
        /// <param name="loadSceneParameters">The parameters to use when loading the scene</param>
        /// <param name="allowSceneActivation">Should the scene be activated as soon as it is loaded</param>
        public DLCAsync LoadAsync(LoadSceneParameters loadSceneParameters, bool allowSceneActivation = true)
        {
            // Check for loadable
            CheckLoaded();

            // Check for pending
            if (pendingLoadRequest != null)
                throw new InvalidOperationException("Cannot start a new load scene request because a pending request is waiting for activation. Use `ActivatePendingScene` to switch to the pending scene");

            // Create async
            DLCAsync async = new DLCAsync();

            // Create load request
            asyncProvider.RunAsync(LoadSceneFromBundleAsync(async, loadSceneParameters, allowSceneActivation));

            return async;
        }

        /// <summary>
        /// Attempt to switch activation to the current streamed scene loaded during the last async load operation.
        /// This is the same as setting `allowSceneActivation` to true and can be used to switch from a loading screen at a suitable time.
        /// You must use one of the <see cref="LoadAsync(LoadSceneMode, bool)"/> methods first with `allowSceneActivation` set to `false`.
        /// </summary>
        /// <exception cref="InvalidOperationException">There is no pending load scene operation</exception>
        public void ActivatePendingScene()
        {
            if (pendingLoadRequest == null)
                throw new InvalidOperationException("There is no pending scene request waiting for activation. Use `LoadAsync` first with `allowSceneActivation` disabled");

            // Allow the activation
            pendingLoadRequest.allowSceneActivation = true;
            pendingLoadRequest = null;
        }

        private IEnumerator LoadSceneFromBundleAsync(DLCAsync async, LoadSceneParameters loadSceneParameters, bool allowSceneActivation)
        {
            bool didLoadBundle = false;

            // Check for bundle
            if(contentBundle.IsNotLoaded == true)
            {
                // Request load of bundle
                Debug.Log("Loading scene assets bundle async on demand...");
                DLCAsync asyncBundle = contentBundle.RequestLoadAsync(asyncProvider);

                // Update status
                async.UpdateStatus("Loading content bundle");
                didLoadBundle = true;

                // Wait for load
                while(asyncBundle.IsDone == false)
                {
                    // Update progress
                    async.UpdateProgress(asyncBundle.Progress * 0.5f);

                    // Wait a frame
                    yield return null;
                }

                // Check for success
                if(asyncBundle.IsSuccessful == false)
                {
                    async.UpdateStatus("Failed to load content bundle");
                    async.Complete(false);
                    yield break;
                }
            }

            // Create load request
            Debug.Log("Load scene async: " + relativeName);
            AsyncOperation request = SceneManager.LoadSceneAsync(name, loadSceneParameters);

            // Update activation
            if (allowSceneActivation == false)
            {
                request.allowSceneActivation = false;
                pendingLoadRequest = request;
            }

            // Update status
            async.UpdateStatus("Loading scene: " + relativeName);

            // Wait for level load
            while(allowSceneActivation ? request.isDone == false : request.progress < 0.9f)
            {
                if (allowSceneActivation == true)
                {
                    // Update progress
                    async.UpdateProgress(didLoadBundle ? request.progress * 0.5f : request.progress);
                }
                else
                {
                    async.UpdateProgress(didLoadBundle 
                        ? Mathf.InverseLerp(0f, 0.9f, request.progress) * 0.5f
                        : Mathf.InverseLerp(0f, 0.9f, request.progress));
                }

                // Wait a frame
                yield return null;
            }

            // Update loaded object
            loadedObject = request;

            // Update status
            async.UpdateStatus("Loading complete");

            // Complete operation
            async.Complete(true);
        }

        private void CheckLoaded()
        {
            if (loadMode != DLCLoadMode.Full)
                throw new DLCNotLoadedException("Cannot load assets because the DLC is loaded in metadata only mode");
        }
    }
}
