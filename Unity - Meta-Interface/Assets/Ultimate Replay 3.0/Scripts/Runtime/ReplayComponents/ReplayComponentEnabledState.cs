using UltimateReplay.Formatters;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A replay component used to record the enabled state of a behaviour component.
    /// </summary>
    public class ReplayComponentEnabledState : ReplayRecordableBehaviour
    {
        // Private
        private static readonly ReplayEnabledStateFormatter formatter = new ReplayEnabledStateFormatter();

        // Public
        /// <summary>
        /// The behaviour component that will have its enabled state recorded and replayed.
        /// </summary>
        public Behaviour observedComponent;

        // Properties
        /// <summary>
        /// Get the formatter for this replay component.
        /// </summary>
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        // Methods
        /// <summary>
        /// Reset this replay component.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();

            // Try to auto-find component
            if (observedComponent == null)
                observedComponent = GetComponent<Behaviour>();
        }

        private void Start()
        {
            if (observedComponent == null)
                Debug.LogWarningFormat("Replay component enabled state '{0}' will not record or replay because the observed component has not been assigned", this);
        }

        /// <summary>
        /// Called by the replay system when the component should serialize its recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to write to</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedComponent == null)
                return;

            // Update formatter
            formatter.UpdateFromBehaviour(observedComponent);

            // Run formatter
            formatter.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when the component should deserialize previously recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to read from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedComponent == null)
                return;

            // Run formatter
            formatter.OnReplayDeserialize(state);

            // Sync enable state
            formatter.SyncBehaviour(observedComponent);
        }       
    }
}
