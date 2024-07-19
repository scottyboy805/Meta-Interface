using UltimateReplay.Formatters;
using UnityEngine;

namespace UltimateReplay
{
    public sealed class ReplayRiggedHumanoid : ReplayRecordableBehaviour
    {
        // Internal
        internal ReplayRiggedHumanoidFormatter.ReplayRiggedHumanoidSerializeFlags serializeFlags = 0;

        // Private
        private static readonly ReplayRiggedHumanoidFormatter formatter = new ReplayRiggedHumanoidFormatter();

        private HumanPoseHandler poseHandler = null;
        private HumanPose currentPose = default;
        private HumanPose lerpPoseFrom = default;
        private HumanPose lerpPoseTo = default;

        // Internal
        [SerializeField]
        [HideInInspector]
        internal RecordFullAxisFlags replayBodyPosition = RecordFullAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision bodyPositionPrecision = RecordPrecision.FullPrecision32Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordFullAxisFlags replayBodyRotation = RecordFullAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision bodyRotationPrecision = RecordPrecision.FullPrecision32Bit;
        [SerializeField]
        [HideInInspector]
        internal bool interpolateMuscleValues = true;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision muscleValuePrecision = RecordPrecision.FullPrecision32Bit;

        // Public
        public Animator observedAnimator;
        public Transform observedRoot;

        // Properties
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        public RecordFullAxisFlags ReplayBodyPosition
        {
            get { return replayBodyPosition; }
            set
            {
                replayBodyPosition = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision BodyPositionPrecision
        {
            get { return bodyPositionPrecision; }
            set
            {
                bodyPositionPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public RecordFullAxisFlags ReplayBodyRotation
        {
            get { return replayBodyRotation; }
            set
            {
                replayBodyRotation = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision BodyRotationPrecision
        {
            get { return bodyRotationPrecision; }
            set
            {
                bodyRotationPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision MuslceValuesPrecision
        {
            get { return muscleValuePrecision; }
            set
            {
                muscleValuePrecision = value;
                UpdateSerializeFlags();
            }
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

#if UNITY_EDITOR
            // Try to auto-find the animator
            if (observedAnimator == null)
                observedAnimator = GetComponent<Animator>();

            // Try to auto find the root
            if (observedRoot == null && observedAnimator != null)
                observedRoot = observedAnimator.transform;
#endif
        }

        protected override void Awake()
        {
            base.Awake();

            // Update serialize flags
            UpdateSerializeFlags();
        }

        private void Start()
        {
            // Check for no assigned animator
            if(observedAnimator == null)
            {
                Debug.LogWarningFormat("Replay rigged humanoid '{0}' will not record or replay because the observed animator has not been assigned!", this);
                return;
            }

            // Check for no animator avatar
            if(observedAnimator.avatar == null)
            {
                Debug.LogWarningFormat("Replay rigged humanoid '{0}' will not record or replay beacause the assigned observed animator does not have a valid avatar associated with it!", this);
            }

            // Check for no assigned root
            if(observedRoot == null)
            {
                Debug.LogWarningFormat("Replay rigged humanoid '{0}' will not record or replay because the observed root transform has not been assigned!", this);
                return;
            }

            // Create pose handler#
            UpdatePoseHandler();
        }

        protected override void OnReplayReset()
        {
            lerpPoseTo = currentPose;
            lerpPoseFrom = currentPose;
        }

        protected override void OnReplayStart()
        {
            lerpPoseTo = currentPose;
            lerpPoseFrom = currentPose;
        }

        protected override void OnReplayUpdate(float t)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                UpdatePoseHandler();
                UpdateSerializeFlags();
            }
#endif

            // Check for error
            if (poseHandler == null)
                return;

            bool updatePose = false;

            // Body positon
            if((replayBodyPosition & RecordFullAxisFlags.Interpolate) != 0 && (replayBodyPosition & RecordFullAxisFlags.XYZ) != 0)
            {
                // Interpolate position
                currentPose.bodyPosition = Vector3.Lerp(lerpPoseFrom.bodyPosition, lerpPoseTo.bodyPosition, t);
                updatePose = true;
            }

            // Body rotation
            if((replayBodyRotation & RecordFullAxisFlags.Interpolate) != 0 && (replayBodyRotation & RecordFullAxisFlags.XYZ) != 0)
            {
                currentPose.bodyRotation = Quaternion.Lerp(lerpPoseFrom.bodyRotation, lerpPoseTo.bodyRotation, t);
                updatePose = true;
            }

            // Muscle values
            if(interpolateMuscleValues == true && currentPose.muscles.Length > 0)
            {
                for (int i = 0; i < currentPose.muscles.Length; i++)
                    currentPose.muscles[i] = Mathf.Lerp(lerpPoseFrom.muscles[i], lerpPoseTo.muscles[i], t);

                updatePose = true;
            }

            // Check for update pose
            if (updatePose == true && poseHandler != null)
                poseHandler.SetHumanPose(ref currentPose);
        }

        public override void OnReplaySerialize(ReplayState state)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                UpdatePoseHandler();
                UpdateSerializeFlags();
            }
#endif

            // Check for error
            if (poseHandler == null)
                return;

            // Get the current humanoid pose
            poseHandler.GetHumanPose(ref currentPose);

            // Update formatter
            formatter.UpdateFromPose(currentPose, serializeFlags);

            // Serialize the data
            formatter.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                UpdatePoseHandler();
                UpdateSerializeFlags();
            }
#endif

            // Check for error
            if (poseHandler == null)
                return;

            // Update last values
            lerpPoseFrom.bodyPosition = lerpPoseTo.bodyPosition;
            lerpPoseFrom.bodyRotation = lerpPoseTo.bodyRotation;

            for (int i = 0; i < lerpPoseFrom.muscles.Length; i++)
                lerpPoseFrom.muscles[i] = lerpPoseTo.muscles[i];

            // Deserialize the data
            formatter.OnReplayDeserialize(state);

            // Fetch values
            lerpPoseTo.bodyPosition = formatter.BodyPosition;
            lerpPoseTo.bodyRotation = formatter.BodyRotation;

            for (int i = 0; i < formatter.MuscleValues.Count; i++)
                lerpPoseTo.muscles[i] = formatter.MuscleValues[i];

            bool updatePose = false;

            // Update body position if interpolate is disabled
            if((replayBodyPosition & RecordFullAxisFlags.Interpolate) == 0)
            {
                currentPose.bodyPosition = lerpPoseTo.bodyPosition;
                updatePose = true;
            }

            // update body rotation if interpolation is dsabled
            if((replayBodyRotation & RecordFullAxisFlags.Interpolate) == 0)
            {
                currentPose.bodyRotation = lerpPoseTo.bodyRotation;
                updatePose = true;
            }

            // Update muslce values if interpolation is disabled
            if(interpolateMuscleValues == true)
            {
                for (int i = 0; i < currentPose.muscles.Length; i++)
                    currentPose.muscles[i] = lerpPoseTo.muscles[i];

                updatePose = true;
            }

            // Check for update pose
            if (updatePose == true && poseHandler != null)
                poseHandler.SetHumanPose(ref currentPose);
        }

        private void UpdatePoseHandler()
        {
            // Update pose handler
            if (poseHandler == null && observedAnimator != null && observedRoot != null && observedAnimator.avatar != null)
                poseHandler = new HumanPoseHandler(observedAnimator.avatar, observedRoot);
        }

        private void UpdateSerializeFlags()
        {
            serializeFlags = ReplayRiggedHumanoidFormatter.GetSerializeFlags(replayBodyPosition, bodyPositionPrecision,
                replayBodyRotation, bodyRotationPrecision,
                muscleValuePrecision);
        }
    }
}
