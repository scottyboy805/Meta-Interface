using UltimateReplay.Formatters;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A replay component used to record the enabled state of a game object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReplayEnabledState : ReplayRecordableBehaviour
    {
        // Private
        private static readonly ReplayEnabledStateFormatter formatter = new ReplayEnabledStateFormatter();

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
        /// Called by the replay system when recorded data should be captured.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> used to store the recorded data</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Update formatter
            formatter.UpdateFromGameObject(gameObject);

            // Run formatter
            formatter.OnReplaySerialize(state);
        }

        /// <summary>
        ///  Called by the replay system when replay data should be restored.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> containing the previously recorded data</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Run formatter
            formatter.OnReplayDeserialize(state);

            // Sync enable state
            formatter.SyncGameObject(gameObject);
        }        
    }
}
