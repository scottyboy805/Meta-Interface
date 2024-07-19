using System;
using UltimateReplay.Formatters;
using UltimateReplay.Statistics;
using UnityEngine;

namespace UltimateReplay
{
    [DisallowMultipleComponent]
    public sealed class ReplayAnimator : ReplayRecordableBehaviour
    {
        // Type
        [Flags]
        public enum ReplayIKFlags
        {
            None = 0,
            Position = 1 << 1,
            Rotation = 1 << 2,
            Weights = 1 << 3,
        }

        // Internal
        internal ReplayAnimatorFormatter.ReplayAnimatorSerializeFlags serializeFlags = 0;

        // Private
        private static readonly ReplayAnimatorFormatter formatter = new ReplayAnimatorFormatter();

        private AnimatorControllerParameter[] parameters = new AnimatorControllerParameter[0];
        private float initialAnimatorSpeed = 0f;

        private ReplayAnimatorFormatter.ReplayAnimatorState[] targetStates = null;
        private ReplayAnimatorFormatter.ReplayAnimatorState[] lastStates = null;
        private ReplayAnimatorFormatter.ReplayAnimatorParameter[] targetParameters = null;
        private ReplayAnimatorFormatter.ReplayAnimatorParameter[] lastParameters = null;
        private ReplayAnimatorFormatter.ReplayAnimatorIKTarget[] lastIKTargets = null;
        private ReplayAnimatorFormatter.ReplayAnimatorIKTarget[] targetIKTargets = null;
        private ReplayAnimatorFormatter.ReplayAnimatorIKTarget[] currentIKTargets = null;

        // Public
        public Animator observedAnimator;

        // Internal
        [SerializeField]
        internal bool replayParameters = true;
        [SerializeField]
        internal ReplayIKFlags replayIK = ReplayIKFlags.None;
        [SerializeField]
        internal RecordPrecision recordPrecision = RecordPrecision.FullPrecision32Bit;
        [SerializeField]
        internal bool interpolate = true;
        [SerializeField]
        internal bool interpolateParameters = true;
        [SerializeField]
        internal bool interpolateIK = true;

        // Properties
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        public bool ReplayParameters
        {
            get { return replayParameters; }
            set
            {
                replayParameters = value;
                UpdateSerializeFlags();
            }
        }

        public bool ReplayIKPositionTargets
        {
            get { return (replayIK & ReplayIKFlags.Position) != 0; }
            set
            {
                // Clear bit
                replayIK &= ~ReplayIKFlags.Position;

                // Set bit
                if(value == true) replayIK |= ReplayIKFlags.Position;
            }
        }

        public bool ReplayIKRotationTargets
        {
            get { return (replayIK & ReplayIKFlags.Rotation) != 0; }
            set
            {
                // Clear bit
                replayIK &= ~ReplayIKFlags.Rotation;

                // Set bit
                if (value == true) replayIK |= ReplayIKFlags.Rotation;
            }
        }

        public bool ReplayIKWeights
        {
            get { return (replayIK & ReplayIKFlags.Weights) != 0; }
            set
            {
                // Clear bit
                replayIK&= ~ReplayIKFlags.Weights;

                // Set bit
                if (value == true) replayIK |= ReplayIKFlags.Weights;
            }
        }

        public RecordPrecision RecordPrecision
        {
            get { return recordPrecision; }
            set
            {
                recordPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public bool Interpolate
        {
            get { return interpolate; }
            set { interpolate = value; }
        }

        public bool InterpolateParameters
        {
            get { return interpolateParameters; }
            set { interpolateParameters = value; }
        }

        // Methods
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update serialize flags
            UpdateSerializeFlags();
        }
#endif

        protected override void Reset()
        {
            base.Reset();

            // Try to auto-find animator component
            if(observedAnimator == null)
                observedAnimator = GetComponent<Animator>();
        }

        protected override void Awake()
        {
            base.Awake();

            // Update serialize flags
            UpdateSerializeFlags();
        }

        private void Start()
        {
            // Check for observed animator
            if (observedAnimator == null || observedAnimator.runtimeAnimatorController == null)
            {
                Debug.LogWarningFormat("Replay animator '{0}' will not record or replay because the observed animator has not been assigned or does not have a controller assigned", this);
                parameters = new AnimatorControllerParameter[0];
            }
            else
            {
                // Get parameters
                parameters = observedAnimator.parameters;
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // Only update IK in playback mode
            if (IsReplayingOrPaused == true)
            {
                // Perform interpolation
                if((replayIK & ReplayIKFlags.Position) != 0)
                {
                    for(int i = 0; i < targetIKTargets.Length; i++)
                    {
                        // Set Ik target position
                        observedAnimator.SetIKPosition((AvatarIKGoal)i, (currentIKTargets != null ? currentIKTargets : targetIKTargets)[i].targetPosition);
                    }
                }

                if((replayIK & ReplayIKFlags.Rotation) != 0)
                {
                    for(int i = 0; i < targetIKTargets.Length; i++)
                    {
                        // Set IK target rotation
                        observedAnimator.SetIKRotation((AvatarIKGoal)i, (currentIKTargets != null ? currentIKTargets : targetIKTargets)[i].targetRotation);
                    }
                }

                if((replayIK & ReplayIKFlags.Weights) != 0)
                {
                    for(int i = 0; i < targetIKTargets.Length; i++)
                    {
                        // Set IK target weights
                        observedAnimator.SetIKPositionWeight((AvatarIKGoal)i, (currentIKTargets != null ? currentIKTargets : targetIKTargets)[i].positionWeight);
                        observedAnimator.SetIKRotationWeight((AvatarIKGoal)i, (currentIKTargets != null ? currentIKTargets : targetIKTargets)[i].rotationWeight);
                    }
                }
            }

            // Only capture IK in record mode
            if(IsRecording == true)
            {
                // Sample all replayed IK values from animator
                formatter.UpdateIKFromAnimator(observedAnimator, serializeFlags);
            }
        }
        /// <summary>
        /// Called by the replay system when preserved data should be reset.
        /// </summary>
        protected override void OnReplayReset()
        {
            lastStates = targetStates;
            lastParameters = targetParameters;
            lastIKTargets = targetIKTargets;
        }

        /// <summary>
        /// Called by the replay system when playback is about to begin.
        /// </summary>
        protected override void OnReplayStart()
        {
            // CHeck for component
            if (observedAnimator == null || observedAnimator.runtimeAnimatorController == null)
                return;

            // Get the current animator speed
            initialAnimatorSpeed = observedAnimator.speed;
        }

        /// <summary>
        /// Called by the replay system when playback will end.
        /// </summary>
        protected override void OnReplayEnd()
        {
            // Check for component
            if (observedAnimator == null || observedAnimator.runtimeAnimatorController == null)
                return;

            // Restore the original animator speed
            observedAnimator.speed = initialAnimatorSpeed;
        }

        /// <summary>
        /// Called by the replay system when playback will be paused or resumed.
        /// </summary>
        /// <param name="paused">True if playback is pausing or false if it is resuming</param>
        protected override void OnReplayPlayPause(bool paused)
        {
            // Check for component
            if (observedAnimator == null || observedAnimator.runtimeAnimatorController == null)
                return;

            if (paused == true)
            {
                // Disable animator causing pause
                observedAnimator.enabled = false;
            }
        }

        /// <summary>
        /// Called by the replay system when the playback will be updated.
        /// Use this method to perform interpolation and smoothing processes.
        /// </summary>
        /// <param name="t">The delta value from 0-1 between current replay snapshots</param>
        protected override void OnReplayUpdate(float t)
        {
            // Check for component
            if (observedAnimator == null || observedAnimator.runtimeAnimatorController == null)
                return;

            // Make sure the animator is active and speed is set to 0
            observedAnimator.enabled = true;
            observedAnimator.speed = 0f;


            // Update all states
            if (interpolate == true && targetStates != null)
            {
                // Update all states
                for (int i = 0; i < targetStates.Length; i++)
                {
                    // Get the target state
                    ReplayAnimatorFormatter.ReplayAnimatorState updateState = targetStates[i];

                    // Check for interpolate
                    if(lastStates != null)
                    {
                        // Get the previous state
                        ReplayAnimatorFormatter.ReplayAnimatorState lastState = lastStates[i];

                        // Interpolate time
                        updateState.normalizedTime = Mathf.Lerp(lastState.normalizedTime, updateState.normalizedTime, t);
                    }

                    // Play the animator
                    observedAnimator.Play(updateState.stateHash, i, updateState.normalizedTime);
                }
            }

            // Update all parameters
            if (interpolateParameters == true && targetParameters != null)
            {
                for (int i = 0; i < parameters.Length && i < targetParameters.Length && i < lastParameters.Length; i++)
                {
                    // Ignore curve parameters
                    if (observedAnimator.IsParameterControlledByCurve(parameters[i].nameHash) == true)
                        continue;

                    // Get the target parameter
                    ReplayAnimatorFormatter.ReplayAnimatorParameter updateParameter = targetParameters[i];

                    switch (parameters[i].type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            {
                                observedAnimator.SetBool(updateParameter.nameHash, updateParameter.boolValue);
                                break;
                            }

                        case AnimatorControllerParameterType.Trigger:
                            {
                                if (updateParameter.boolValue == true)
                                    observedAnimator.SetTrigger(updateParameter.nameHash);
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                // Set param
                                observedAnimator.SetInteger(updateParameter.nameHash, updateParameter.intValue);
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                if (interpolateParameters == true)
                                {
                                    // Get last state
                                    ReplayAnimatorFormatter.ReplayAnimatorParameter lastParameter = lastParameters[i];

                                    // Interpolate
                                    updateParameter.floatValue = Mathf.Lerp(lastParameter.floatValue, updateParameter.floatValue, t);
                                }

                                // Set param
                                observedAnimator.SetFloat(updateParameter.nameHash, updateParameter.floatValue);
                                break;
                            }
                    }
                }
            }

            // Update all IK
            if (interpolateIK == true && targetIKTargets != null)
            {
                for (int i = 0; i < targetIKTargets.Length; i++)
                {
                    // Check for last
                    if (lastIKTargets != null && currentIKTargets != null)
                    {
                        currentIKTargets[i].targetPosition = Vector3.Lerp(lastIKTargets[i].targetPosition, targetIKTargets[i].targetPosition, t);
                        currentIKTargets[i].targetRotation = Quaternion.Lerp(lastIKTargets[i].targetRotation, targetIKTargets[i].targetRotation, t);
                        currentIKTargets[i].positionWeight = Mathf.Lerp(lastIKTargets[i].positionWeight, targetIKTargets[i].positionWeight, t);
                        currentIKTargets[i].rotationWeight = Mathf.Lerp(lastIKTargets[i].rotationWeight, targetIKTargets[i].rotationWeight, t);
                    }
                    else if(lastIKTargets == null)
                    {
                        currentIKTargets[i].targetPosition = targetIKTargets[i].targetPosition;
                        currentIKTargets[i].targetRotation = targetIKTargets[i].targetRotation;
                        currentIKTargets[i].positionWeight = targetIKTargets[i].positionWeight;
                        currentIKTargets[i].rotationWeight = targetIKTargets[i].rotationWeight;
                    }
                }
            }
        }

        /// <summary>
        /// Called by the replay system when recorded data should be captured and serialized.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> used to store the recorded data</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedAnimator == null || observedAnimator.enabled == false || observedAnimator.runtimeAnimatorController == null)
                return;

            // Check for playing
            if (Application.isPlaying == false)
            {
                ReplayRecordableStatistics.SupressStatisticsDuringEditMode();
                return;
            }

            // Update formatter
            formatter.UpdateFromAnimator(observedAnimator, parameters, serializeFlags);

            // Serialize the data
            formatter.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when replay data should be deserialized and restored.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> containing the previously recorded data</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedAnimator == null || observedAnimator.enabled == false || observedAnimator.runtimeAnimatorController == null)
                return;

            // Update last values
            OnReplayReset();

            // Deserialize the data
            formatter.OnReplayDeserialize(state);

            // Fetch elements
            targetStates = formatter.States;
            targetParameters = formatter.Parameters;
            targetIKTargets = formatter.IKTargets;

            // Create temp arrays
            if(currentIKTargets == null && interpolateIK == true)
                currentIKTargets = new ReplayAnimatorFormatter.ReplayAnimatorIKTarget[targetIKTargets.Length];

            // Check for no interpolation
            if(interpolate == false || targetStates == null)
            {
                // Update immediate
                formatter.SyncAnimator(observedAnimator, parameters, serializeFlags);
            }
        }

        private void UpdateSerializeFlags()
        {
            bool ikPosition = (replayIK & ReplayIKFlags.Position) != 0;
            bool ikRotation = (replayIK & ReplayIKFlags.Rotation) != 0;
            bool ikWeights = (replayIK & ReplayIKFlags.Weights) != 0;

            // Get packed flags containing all serialize data
            serializeFlags = ReplayAnimatorFormatter.GetSerializeFlags(replayParameters, ikPosition, ikRotation, ikWeights, recordPrecision);
        }
    }
}
