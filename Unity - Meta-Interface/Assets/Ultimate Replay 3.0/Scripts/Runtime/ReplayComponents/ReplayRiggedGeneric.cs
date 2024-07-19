using System;
using System.Collections.Generic;
using UltimateReplay.Formatters;
using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    [DisallowMultipleComponent]
    public sealed class ReplayRiggedGeneric : ReplayRecordableBehaviour
    {
        // Type
        private struct BoneUpdate
        {
            // Public
            public Vector3 lerpPositionFrom;
            public Vector3 lerpPositionTo;
            public Quaternion lerpRotationFrom;
            public Quaternion lerpRotationTo;
            public Vector3 lerpScaleFrom;
            public Vector3 lerpScaleTo;
        }

        // Internal
        internal ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags serializeFlags = 0;

        // Private
        private static readonly ReplayRiggedGenericFormatter formatter = new ReplayRiggedGenericFormatter();

        private BoneUpdate updateRootBone = new BoneUpdate();
        private BoneUpdate[] updateBones = Array.Empty<BoneUpdate>();
        private bool bonesInitialized = false;

        // Internal
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayRootBonePosition = RecordAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision rootBonePositionPrecision = RecordPrecision.FullPrecision32Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayRootBoneRotation = RecordAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision rootBoneRotationPrecision = RecordPrecision.HalfPrecision16Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayRootBoneScale = RecordAxisFlags.None;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision rootBoneScalePrecision = RecordPrecision.FullPrecision32Bit;

        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayBonePosition = RecordAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision bonePositionPrecision = RecordPrecision.HalfPrecision16Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayBoneRotation = RecordAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision boneRotationPrecision = RecordPrecision.HalfPrecision16Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayBoneScale = RecordAxisFlags.None;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision boneScalePrecision = RecordPrecision.FullPrecision32Bit;

        // Public
        public Transform observedRootBone;
        public Transform[] observedBones;

        // Properties
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        public RecordAxisFlags ReplayBonePosition
        {
            get { return replayBonePosition; }
            set
            {
                replayBonePosition = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision BonePositionPrecision
        {
            get { return bonePositionPrecision; }
            set
            {
                bonePositionPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public RecordAxisFlags ReplayBoneRotation
        {
            get { return replayBoneRotation; }
            set
            {
                replayBoneRotation = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision BoneRotationPrecision
        {
            get { return boneRotationPrecision; }
            set
            {
                boneRotationPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public RecordAxisFlags ReplayBoneScale
        {
            get { return replayBoneScale; }
            set
            {
                replayBoneScale = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision BoneScalePrecision
        {
            get { return boneScalePrecision; }
            set
            {
                boneScalePrecision = value;
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

            // Try to auto-find component
            if(observedBones == null || observedBones.Length == 0)
            {
                // Detect bones from skinned renderers
                AutoDetectRigBones();
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // Update serialize flags
            UpdateSerializeFlags();
        }

        private void Start()
        {
            // Check for null
            if (observedBones == null || observedBones.Length == 0)
                Debug.LogWarningFormat("Replay rigged generic '{0}' will not record or replay because the observed bones has not been assigned!", this);

            // Init Bones
            InitializeBonesArrays();
        }

        public void AutoDetectRigBones()
        {
            List<Transform> rootBones = new List<Transform>();
            List<Transform> bones = new List<Transform>(100);

            // Find all skinned renderers
            SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            // Check for any renderers
            if (skinnedRenderers.Length > 0)
            {
                foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
                {
                    // Add root bone
                    if (rootBones.Contains(skinnedRenderer.rootBone) == false)
                        rootBones.Add(skinnedRenderer.rootBone);

                    // Add all bones
                    foreach (Transform bone in skinnedRenderer.bones)
                    {
                        if (bones.Contains(bone) == false && rootBones.Contains(bone) == false)
                            bones.Add(bone);
                    }
                }

                // Store the target root bone
                observedRootBone = (rootBones.Count > 1)
                    ? FindHighestLevelBone(rootBones)
                    : rootBones[0];

                // Store other bones collection
                observedBones = bones.ToArray();

                // Check for bones
                if(observedRootBone != null && observedRootBone != transform && GetComponent<ReplayTransform>() == null)
                {
#if UNITY_EDITOR
                    Undo.AddComponent<ReplayTransform>(gameObject);
#else
                    gameObject.AddComponent<ReplayTransform>();
#endif
                }
                else
                    Debug.LogWarningFormat("Replay rigged generic '{0}' found skinned mesh renderers '{1]' but there were no associated bones! You will need to assigned observed bones manually", this, skinnedRenderers.Length);
            }
            else
                Debug.LogWarningFormat("Replay rigged generic '{0}' did not find any bones! No skinned mesh rendered found - You will need to assign observed bones manually", this);

//            // Try to get skinned renderer
//            SkinnedMeshRenderer skinnedRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

//            // Check for valid
//            if (skinnedRenderer != null)
//            {
//                // Check for any bones
//                if (skinnedRenderer.bones != null)
//                {
//                    // Get all bones as list
//                    List<Transform> allBones = new List<Transform>(skinnedRenderer.bones);

//                    // Remove root bone
//                    if (skinnedRenderer.rootBone != null && allBones.Contains(skinnedRenderer.rootBone) == true)
//                        allBones.Remove(skinnedRenderer.rootBone);

//                    // Store the bones
//                    observedRootBone = skinnedRenderer.rootBone;
//                    observedBones = allBones.ToArray();

//                    // Check for root transform
//                    if (observedRootBone != null && observedRootBone != transform && GetComponent<ReplayTransform>() == null)
//                    {
//#if UNITY_EDITOR
//                        Undo.AddComponent<ReplayTransform>(gameObject);
//#else
//                            gameObject.AddComponent<ReplayTransform>();
//#endif
//                    }
//                }
//                else
//                    Debug.LogWarningFormat("Replay rigged generic '{0}' found skinned mesh renderer '{1]' but it has no bones! You will need to assigned observed bones manually", this, skinnedRenderer);
//            }
//            else
                
        }

        protected override void OnReplayReset()
        {
            // Update root bone
            updateRootBone.lerpPositionFrom = updateRootBone.lerpPositionTo;
            updateRootBone.lerpRotationFrom = updateRootBone.lerpRotationTo;
            updateRootBone.lerpScaleFrom = updateRootBone.lerpScaleTo;

            // Update child bones
            for(int i = 0; i < updateBones.Length; i++)
            {
                updateBones[i].lerpPositionFrom = updateBones[i].lerpPositionTo;
                updateBones[i].lerpRotationFrom = updateBones[i].lerpRotationTo;
                updateBones[i].lerpScaleFrom = updateBones[i].lerpScaleTo;
            }
        }

        protected override void OnReplayStart()
        {
            // It is possible for playback to begin before 'Start' event has been called, so we will need to initialize bones in that case
            InitializeBonesArrays();
        }

        protected override void OnReplayUpdate(float t)
        {
            // Update root bone
            if (observedRootBone != null)
            {
                // Root position
                if ((replayRootBonePosition & RecordAxisFlags.Interpolate) != 0)
                {
                    // Interpolate position
                    Vector3 updatePosition = Vector3.Lerp(updateRootBone.lerpPositionFrom, updateRootBone.lerpPositionTo, t);

                    // Update selected axis
                    updatePosition.x = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootPosX) != 0) ? updatePosition.x : observedRootBone.localPosition.x;
                    updatePosition.y = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootPosY) != 0) ? updatePosition.y : observedRootBone.localPosition.y;
                    updatePosition.z = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootPosZ) != 0) ? updatePosition.z : observedRootBone.localPosition.z;

                    // Update position
                    observedRootBone.localPosition = updatePosition;
                }

                // Root rotation
                if ((replayRootBoneRotation & RecordAxisFlags.Interpolate) != 0)
                {
                    // Interpolate rotation
                    Quaternion updateRotation = Quaternion.Lerp(updateRootBone.lerpRotationFrom, updateRootBone.lerpRotationTo, t);

                    // Check for full rotation
                    if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotX) != 0 && (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotY) != 0 && (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotZ) != 0)
                    {
                        // Update rotation direct
                        observedRootBone.localRotation = updateRotation;
                    }
                    else
                    {
                        Vector3 euler = updateRotation.eulerAngles;

                        // Update axis
                        if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotX) == 0) euler.x = observedRootBone.localEulerAngles.x;
                        if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotY) == 0) euler.y = observedRootBone.localEulerAngles.y;
                        if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotZ) == 0) euler.z = observedRootBone.localEulerAngles.z;

                        // Update local rotation
                        observedRootBone.localRotation = Quaternion.Euler(euler);
                    }
                }

                // Root scale
                if((replayRootBoneScale & RecordAxisFlags.Interpolate) != 0)
                {
                    // Interpolate scale
                    Vector3 updateScale = Vector3.Lerp(updateRootBone.lerpScaleFrom, updateRootBone.lerpScaleTo, t);

                    // Update selected axis
                    updateScale.x = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootScaX) != 0) ? updateScale.x : observedRootBone.localScale.x;
                    updateScale.y = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootScaY) != 0) ? updateScale.y : observedRootBone.localScale.y;
                    updateScale.z = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootScaZ) != 0) ? updateScale.z : observedRootBone.localScale.z;

                    // Update scale
                    observedRootBone.localScale = updateScale;
                }
            }

            // Update child bones
            if(observedBones != null)
            {
                for(int i = 0; i < updateBones.Length; i++)
                {
                    // Root position
                    if ((replayBonePosition & RecordAxisFlags.Interpolate) != 0)
                    {
                        // Interpolate position
                        Vector3 updatePosition = Vector3.Lerp(updateBones[i].lerpPositionFrom, updateBones[i].lerpPositionTo, t);

                        // Update selected axis
                        updatePosition.x = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.PosX) != 0) ? updatePosition.x : observedBones[i].localPosition.x;
                        updatePosition.y = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.PosY) != 0) ? updatePosition.y : observedBones[i].localPosition.y;
                        updatePosition.z = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.PosZ) != 0) ? updatePosition.z : observedBones[i].localPosition.z;

                        // Update position
                        observedBones[i].localPosition = updatePosition;
                    }

                    // Root rotation
                    if ((replayBoneRotation & RecordAxisFlags.Interpolate) != 0)
                    {
                        // Interpolate rotation
                        Quaternion updateRotation = Quaternion.Lerp(updateBones[i].lerpRotationFrom, updateBones[i].lerpRotationTo, t);

                        // Check for full rotation
                        if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotX) != 0 && (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotY) != 0 && (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotZ) != 0)
                        {
                            // Update rotation direct
                            observedBones[i].localRotation = updateRotation;
                        }
                        else
                        {
                            Vector3 euler = updateRotation.eulerAngles;

                            // Update axis
                            if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotX) == 0) euler.x = observedBones[i].localEulerAngles.x;
                            if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotY) == 0) euler.y = observedBones[i].localEulerAngles.y;
                            if ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotZ) == 0) euler.z = observedBones[i].localEulerAngles.z;

                            // Update local rotation
                            observedBones[i].localRotation = Quaternion.Euler(euler);
                        }
                    }

                    // Root scale
                    if ((replayBoneScale & RecordAxisFlags.Interpolate) != 0)
                    {
                        // Interpolate scale
                        Vector3 updateScale = Vector3.Lerp(updateBones[i].lerpScaleFrom, updateBones[i].lerpScaleTo, t);

                        // Update selected axis
                        updateScale.x = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.ScaX) != 0) ? updateScale.x : observedBones[i].localScale.x;
                        updateScale.y = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.ScaY) != 0) ? updateScale.y : observedBones[i].localScale.y;
                        updateScale.z = ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.ScaZ) != 0) ? updateScale.z : observedBones[i].localScale.z;

                        // Update scale
                        observedBones[i].localScale = updateScale;
                    }
                }
            }
        }

        public override void OnReplaySerialize(ReplayState state)
        {
#if UNITY_EDITOR
            // Update flags while in editor so that sample statistics can update
            if (Application.isPlaying == false)
                UpdateSerializeFlags();
#endif

            // Check for null 
            if (observedBones == null || observedBones == null)
                return;

            // Update formatter with data from this rigged object
            formatter.UpdateFromTransforms(observedRootBone, observedBones, serializeFlags);

            // Serialize the data
            formatter.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Update last values
            updateRootBone.lerpPositionFrom = updateRootBone.lerpPositionTo;
            updateRootBone.lerpRotationFrom = updateRootBone.lerpRotationTo;
            updateRootBone.lerpScaleFrom = updateRootBone.lerpScaleTo;

            for(int i = 0; i < updateBones.Length; i++)
            {
                updateBones[i].lerpPositionFrom = updateBones[i].lerpPositionTo;
                updateBones[i].lerpRotationFrom = updateBones[i].lerpRotationTo;
                updateBones[i].lerpScaleFrom = updateBones[i].lerpScaleTo;
            }

            // Deserialize data
            formatter.OnReplayDeserialize(state);

            // Fetch values
            if(formatter.BoneCount != observedBones.Length)
            {
                Debug.LogWarningFormat("Replay rigged generic '{0}' deserialization state does not match current observed bones collection! Replay may fail", this);
                return;
            }

            // Root values
            updateRootBone.lerpPositionTo = formatter.RootPosition;
            updateRootBone.lerpRotationTo = formatter.RootRotation;
            updateRootBone.lerpScaleTo = formatter.RootScale;


            // Update root bone
            if (observedRootBone != null)
            {
                // Update position if interpolation is disabled
                if ((replayRootBonePosition & RecordAxisFlags.Interpolate) == 0 && ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootPosX) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootPosY) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootPosZ) != 0))
                {
                    // Update position
                    formatter.SyncRootBoneTransformPosition(observedRootBone, serializeFlags);
                }

                // Update rotation if interpolation is disabled
                if((replayRootBoneRotation & RecordAxisFlags.Interpolate) == 0 && ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotX) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotY) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootRotZ) != 0))
                {
                    // Update rotation
                    formatter.SyncRootBoneTransformRotation(observedRootBone, serializeFlags);
                }

                // Update scale if interpolation is disabled
                if((replayRootBoneScale & RecordAxisFlags.Interpolate) == 0 && ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootScaX) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootScaY) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RootScaZ) != 0))
                {
                    // Update scale
                    formatter.SyncRootBoneTransformScale(observedRootBone, serializeFlags);
                }
            }

            // Bone values
            for(int i = 0; i < observedBones.Length; i++)
            {
                // Fetch bone transform
                formatter.GetBoneTransform(i, out updateBones[i].lerpPositionTo, out updateBones[i].lerpRotationTo, out updateBones[i].lerpScaleTo);


                // Update child bones
                if(observedRootBone != null)
                {
                    // Update bone position if interpolation is disabled
                    if((replayBonePosition & RecordAxisFlags.Interpolate) == 0 && ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.PosX) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.PosY) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.PosZ) != 0))
                    {
                        // Update position
                        formatter.SyncBoneTransformPosition(observedBones[i], i, serializeFlags);
                    }

                    // Update rotation if interpolation is disabled
                    if((replayBoneRotation & RecordAxisFlags.Interpolate) == 0 && ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotX) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotY) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.RotZ) != 0))
                    {
                        // Update rotation
                        formatter.SyncBoneTransformRotation(observedBones[i], i, serializeFlags);
                    }

                    // Update scale if interpolation is disabled
                    if((replayBoneScale & RecordAxisFlags.Interpolate) == 0 && ((serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.ScaX) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.ScaY) != 0 || (serializeFlags & ReplayRiggedGenericFormatter.ReplayRiggedGenericSerializeFlags.ScaZ) != 0))
                    {
                        // Update scale
                        formatter.SyncBoneTransformScale(observedBones[i], i, serializeFlags);
                    }
                }
            }
        }

        private void InitializeBonesArrays()
        {
            // Check for already initialized
            if (bonesInitialized == true)
                return;

            // Init root
            updateRootBone.lerpRotationFrom = Quaternion.identity;
            updateRootBone.lerpRotationTo = Quaternion.identity;
            updateRootBone.lerpScaleFrom = Vector3.one;
            updateRootBone.lerpScaleTo = Vector3.one;

            // Init array
            updateBones = new BoneUpdate[observedBones.Length];

            for (int i = 0; i < observedBones.Length; i++)
            {
                updateBones[i].lerpRotationFrom = Quaternion.identity;
                updateBones[i].lerpRotationTo = Quaternion.identity;
                updateBones[i].lerpScaleFrom = Vector3.one;
                updateBones[i].lerpScaleTo = Vector3.one;
            }

            // Set initialized flag
            bonesInitialized = true;
        }

        private void UpdateSerializeFlags()
        {
            // Check for error
            if(observedBones == null)
            {
                serializeFlags = 0;
                return;
            }

            // Get packed flags value contianing all serialize data
            serializeFlags = ReplayRiggedGenericFormatter.GetSerializeFlags(observedBones.Length, 
                replayRootBonePosition, replayBonePosition, 
                replayRootBoneRotation, replayBoneRotation,
                replayRootBoneScale, replayBoneScale,
                rootBonePositionPrecision, bonePositionPrecision,
                rootBoneRotationPrecision, boneRotationPrecision,
                rootBoneScalePrecision, boneScalePrecision);
        }

        private static Transform FindHighestLevelBone(IEnumerable<Transform> bones)
        {
            // Store best found bone so far
            int numberOfParents = int.MaxValue;
            Transform root = null;

            // Check all bones for best match
            foreach (Transform bone in bones)
            {
                // Assign first
                if (root == null)
                    root = bone;

                int parentCount = 0;
                Transform current = bone;

                // Count parents
                while (current.parent != null)
                {
                    current = current.parent;
                    parentCount++;
                }

                // Check for better match
                if (parentCount < numberOfParents)
                {
                    numberOfParents = parentCount;
                    root = bone;
                }
            }

            return root;
        }
    }
}
