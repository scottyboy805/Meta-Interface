
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UltimateReplay.Formatters
{ 
    /// <summary>
    /// A dedicated formatter used to serialize and deserialize data for the <see cref="ReplayEnabledState"/> component.
    /// </summary>
    public sealed class ReplayEnabledStateFormatter : ReplayFormatter
    {
        // Private
        [ReplayTokenSerialize("Enabled")]
        private bool enabled = true;

        // Properties
        /// <summary>
        /// The enabled state of the object.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        // Methods
        /// <summary>
        /// Invoke this method to serialize the enabled state data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object to write to</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            state.Write(enabled);
        }

        /// <summary>
        /// Invoke this method to deserialize the enabled state from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object to read from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            enabled = state.ReadBool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFromGameObject(GameObject from)
        {
            // Check for null
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            enabled = from.activeSelf;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFromBehaviour(Behaviour from)
        {
            // Check for null
            if (from == null)
                throw new ArgumentNullException(nameof(from));

            enabled = from.enabled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncGameObject(GameObject sync)
        {
            // Check for null
            if (sync == null)
                throw new ArgumentNullException(nameof(sync));

            // Set enabled
            if (sync.activeSelf != enabled)
                sync.SetActive(enabled);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncBehaviour(Behaviour sync)
        {
            // Check for null
            if (sync == null)
                throw new ArgumentNullException(nameof(sync));

            // Set enabled
            if (sync.enabled != enabled)
                sync.enabled = enabled;
        }
    }
}
