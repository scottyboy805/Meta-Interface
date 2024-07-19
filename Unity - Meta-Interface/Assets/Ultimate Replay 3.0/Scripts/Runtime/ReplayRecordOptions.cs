using System;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A number of options that can be used to control the record behaviour.
    /// </summary>
    [Serializable]
    public sealed class ReplayRecordOptions : ISerializationCallbackReceiver
    {
        // Internal
        internal float recordInterval = (1000f / DefaultRecordFPS) / 1000;

        [Range(MinRecordFPS, MaxRecordFPS)]
        [SerializeField]
        internal float recordFPS = DefaultRecordFPS;

        [SerializeField]
        internal ReplayUpdateMode recordUpdateMode = ReplayUpdateMode.Update;

        // Public
        /// <summary>
        /// The minimum allowable record frame rate.
        /// </summary>
        public const float MinRecordFPS = 1;
        /// <summary>
        /// The maximum allowable record frame rate.
        /// </summary>
        public const float MaxRecordFPS = 60;

        /// <summary>
        /// The default fps value for record operations.
        /// </summary>
        public const float DefaultRecordFPS = 12f;

        // Properties
        /// <summary>
        /// The target record frame rate.
        /// Higher frame rates will result in more storage consumption but better replay accuracy.
        /// </summary>
        public float RecordFPS
        {
            get { return recordFPS; }
            set 
            { 
                recordFPS = value;

                if (recordFPS > 0)
                {
                    // Check for too low
                    if(recordFPS < MinRecordFPS)
                        recordFPS = MinRecordFPS;

                    // Check for too high
                    if (recordFPS > MaxRecordFPS)
                        Debug.LogWarning("Record FPS is set higher than the recommended maximum. This is allowable but may lead to performance issues in some cases. Record FPS: " + recordFPS);

                    // Convert fps to time interval in seconds
                    recordInterval = (1000f / recordFPS) / 1000f;
                }
                else
                {
                    recordInterval = (1000f / DefaultRecordFPS) / 1000f;
                }
            }
        }

        internal float RecordInterval
        {
            get { return recordInterval; }
        }

        /// <summary>
        /// The update method used to update the record operation.
        /// Used for compatibility with other systems that update objects in other update methods such as LateUpdate.
        /// </summary>
        public ReplayUpdateMode RecordUpdateMode
        {
            get { return recordUpdateMode; }
            set { recordUpdateMode = value; }
        }

        // Methods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (recordFPS > 0)
            {
                // Convert fps to time interval in seconds
                recordInterval = (1000f / Mathf.Clamp(recordFPS, MinRecordFPS, MaxRecordFPS)) / 1000f;
            }
            else
            {
                recordInterval = (1000f / DefaultRecordFPS) / 1000f;
            }
        }
    }
}
