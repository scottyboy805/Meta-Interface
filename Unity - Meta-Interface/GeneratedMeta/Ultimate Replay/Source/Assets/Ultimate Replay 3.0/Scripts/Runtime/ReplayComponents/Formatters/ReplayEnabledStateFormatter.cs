/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/ReplayComponents/Formatters/ReplayEnabledStateFormatter.cs
/// </summary>
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UltimateReplay.Formatters
{
    /// <summary>
    /// A dedicated formatter used to serialize and deserialize data for the <see cref = "ReplayEnabledState"/> component.
    /// </summary>
    public sealed class ReplayEnabledStateFormatter : ReplayFormatter
    {
        // Properties
        /// <summary>
        /// The enabled state of the object.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                enabled = value;
            }
        }

        // Methods
        /// <summary>
        /// Invoke this method to serialize the enabled state data to the specified <see cref = "ReplayState"/>.
        /// </summary>
        /// <param name = "state">The state object to write to</param>
        public override void OnReplaySerialize(ReplayState state) => throw new System.NotImplementedException();
        /// <summary>
        /// Invoke this method to deserialize the enabled state from the specified <see cref = "ReplayState"/>.
        /// </summary>
        /// <param name = "state">The state object to read from</param>
        public override void OnReplayDeserialize(ReplayState state) => throw new System.NotImplementedException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFromGameObject(GameObject from) => throw new System.NotImplementedException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateFromBehaviour(Behaviour from) => throw new System.NotImplementedException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncGameObject(GameObject sync) => throw new System.NotImplementedException();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SyncBehaviour(Behaviour sync) => throw new System.NotImplementedException();
    }
}