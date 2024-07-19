using UltimateReplay.Formatters;

namespace UltimateReplay
{
    /// <summary>
    /// Derive from this base class to create custom recorder components. 
    /// </summary>
    public abstract class ReplayRecordableBehaviour : ReplayBehaviour, IReplaySerialize
    {
        // Properties
        /// <summary>
        /// An optional <see cref="ReplayFormatter"/> that is used to serialize a particular component.
        /// Providing a formatter via his property can greatly reduce the amount of data that a <see cref="ReplayObject"/> needs to store.
        /// </summary>
        public virtual ReplayFormatter Formatter { get => null; }

        // Methods
        /// <summary>
        /// Called by the replay system when the recorder component should deserialize any necessary data during playback.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> containing the recorded data</param>
        public abstract void OnReplayDeserialize(ReplayState state);

        /// <summary>
        /// Called by the replay system when the recorder component should serialize and necessary data during recording.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> used to store the serialized data</param>
        public abstract void OnReplaySerialize(ReplayState state);

        /// <summary>
        /// Called by Unity.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Inform component removed
            if(ReplayObject != null)
                ReplayObject.RebuildComponentList();
        }
    }
}
