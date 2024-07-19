/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/ReplayComponents/ReplayEnabledState.cs
/// </summary>
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
        // Properties
        /// <summary>
        /// Get the formatter for this replay component.
        /// </summary>
        public override ReplayFormatter Formatter
        {
            get
            {
                return formatter;
            }
        }

        // Methods
        /// <summary>
        /// Called by the replay system when recorded data should be captured.
        /// </summary>
        /// <param name = "state">The <see cref = "ReplayState"/> used to store the recorded data</param>
        public override void OnReplaySerialize(ReplayState state) => throw new System.NotImplementedException();
        /// <summary>
        ///  Called by the replay system when replay data should be restored.
        /// </summary>
        /// <param name = "state">The <see cref = "ReplayState"/> containing the previously recorded data</param>
        public override void OnReplayDeserialize(ReplayState state) => throw new System.NotImplementedException();
    }
}