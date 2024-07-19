using System;
using UltimateReplay.Formatters;
using UnityEngine;

namespace UltimateReplay
{
    [DisallowMultipleComponent]
    public class ReplayTransform : ReplayRecordableBehaviour
    {
        // Internal
        internal ReplayTransformFormatter.ReplayTransformSerializeFlags serializeFlags = 0;

        // Private
        private static readonly ReplayTransformFormatter formatter = new ReplayTransformFormatter();

        private Transform thisTransform = null;
        private Vector3 lerpPositionFrom = Vector3.zero;
        private Vector3 lerpPositionTo = Vector3.zero;
        private Quaternion lerpRotationFrom = Quaternion.identity;
        private Quaternion lerpRotationTo = Quaternion.identity;
        private Vector3 lerpScaleFrom = Vector3.one;
        private Vector3 lerpScaleTo = Vector3.one;

        // Internal
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayPosition = RecordAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordSpace positionSpace = RecordSpace.World;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision positionPrecision = RecordPrecision.FullPrecision32Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayRotation = RecordAxisFlags.XYZInterpolate;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision rotationPrecision = RecordPrecision.HalfPrecision16Bit;
        [SerializeField]
        [HideInInspector]
        internal RecordSpace rotationSpace = RecordSpace.World;
        [SerializeField]
        [HideInInspector]
        internal RecordAxisFlags replayScale = RecordAxisFlags.None;
        [SerializeField]
        [HideInInspector]
        internal RecordPrecision scalePrecision = RecordPrecision.FullPrecision32Bit;

        // Properties
        public override ReplayFormatter Formatter
        {
            get { return formatter; }
        }

        public RecordAxisFlags ReplayPosition
        {
            get { return replayPosition; }
            set
            {
                replayPosition = value;
                UpdateSerializeFlags();
            }
        }

        public RecordSpace PositionSpace
        {
            get { return positionSpace; }
            set
            {
                positionSpace = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision PositionPrecision
        {
            get { return positionPrecision; }
            set
            {
                positionPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public RecordAxisFlags ReplayRotation
        {
            get { return replayRotation; }
            set
            {
                replayRotation = value;
                UpdateSerializeFlags();
            }
        }

        public RecordSpace RotationSpace
        {
            get { return rotationSpace; }
            set
            {
                rotationSpace = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision RotationPrecision
        {
            get { return rotationPrecision; }
            set
            {
                rotationPrecision = value;
                UpdateSerializeFlags();
            }
        }

        public RecordAxisFlags ReplayScale
        {
            get { return replayScale; }
            set
            {
                replayScale = value;
                UpdateSerializeFlags();
            }
        }

        public RecordPrecision ScalePrecision
        {
            get { return scalePrecision; }
            set
            {
                scalePrecision = value;
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
            if(transform.parent != null)
            {
                // Use local space by default if object is a child (It can improve performance)
                positionSpace = RecordSpace.Local;
                rotationSpace = RecordSpace.Local;
            }
#endif
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Get transform
            thisTransform = transform;

            // Update serialize flags
            UpdateSerializeFlags();

            // Update current targets
            lerpPositionTo = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.LocalPos) != 0) ? thisTransform.localPosition : thisTransform.position;
            lerpRotationTo = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.LocalRot) != 0) ? thisTransform.localRotation : thisTransform.rotation;
            lerpScaleTo = thisTransform.localScale;
        }

        protected override void OnReplayReset()
        {
            lerpPositionFrom = lerpPositionTo;
            lerpRotationFrom = lerpRotationTo;
            lerpScaleFrom = lerpScaleTo;
        }

        protected override void OnReplaySpawned(Vector3 position, Quaternion rotation)
        {
#if UNITY_EDITOR
            thisTransform = transform;
#endif

            lerpPositionFrom = position;
            lerpPositionTo = position;
            lerpRotationFrom = rotation;
            lerpRotationTo = rotation;
            lerpScaleFrom = thisTransform.localScale;
            lerpScaleTo = thisTransform.localScale;
        }

        protected override void OnReplayUpdate(float t)
        {
#if UNITY_EDITOR
            thisTransform = transform;
#endif

            // Position
            if ((replayPosition & RecordAxisFlags.Interpolate) != 0)
            {
                // Interpolate position
                Vector3 updatePosition = Vector3.Lerp(lerpPositionFrom, lerpPositionTo, t);

                // Check for local
                if((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.LocalPos) != 0)
                {
                    // Update selected axis
                    updatePosition.x = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.PosX) != 0) ? updatePosition.x : thisTransform.localPosition.x;
                    updatePosition.y = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.PosY) != 0) ? updatePosition.y : thisTransform.localPosition.y;
                    updatePosition.z = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.PosZ) != 0) ? updatePosition.z : thisTransform.localPosition.z;

                    // Update position
                    thisTransform.localPosition = updatePosition;
                }
                // Use world space
                else
                {
                    // Update selected axis
                    updatePosition.x = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.PosX) != 0) ? updatePosition.x : thisTransform.position.x;
                    updatePosition.y = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.PosY) != 0) ? updatePosition.y : thisTransform.position.y;
                    updatePosition.z = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.PosZ) != 0) ? updatePosition.z : thisTransform.position.z;

                    // Update position
                    thisTransform.position = updatePosition;
                }
            }

            // Rotation
            if((replayRotation & RecordAxisFlags.Interpolate) != 0)
            {
                // Interpolate rotation
                Quaternion updateRotation = Quaternion.Lerp(lerpRotationFrom, lerpRotationTo, t);

                // Check for local
                if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.LocalRot) != 0)
                {
                    // Check for full rotation
                    if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotX) != 0 && (serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotY) != 0 && (serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotZ) != 0)
                    {
                        // Update local rotation
                        thisTransform.localRotation = updateRotation;
                    }
                    else
                    {
                        Vector3 euler = updateRotation.eulerAngles;

                        // Update axis
                        if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotX) == 0) euler.x = thisTransform.localEulerAngles.x;
                        if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotY) == 0) euler.y = thisTransform.localEulerAngles.y;
                        if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotZ) == 0) euler.z = thisTransform.localEulerAngles.z;

                        // Update local rotation
                        thisTransform.localRotation = Quaternion.Euler(euler);
                    }
                }
                // Use world space
                else
                {
                    // Check for full rotation
                    if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotX) != 0 && (serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotY) != 0 && (serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotZ) != 0)
                    {
                        // Update local rotation
                        thisTransform.rotation = updateRotation;
                    }
                    else
                    {
                        Vector3 euler = updateRotation.eulerAngles;

                        // Update axis
                        if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotX) == 0) euler.x = thisTransform.eulerAngles.x;
                        if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotY) == 0) euler.y = thisTransform.eulerAngles.y;
                        if ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.RotZ) == 0) euler.z = thisTransform.eulerAngles.z;

                        // Update local rotation
                        thisTransform.rotation = Quaternion.Euler(euler);
                    }
                }
            }

            // Scale
            if((replayScale & RecordAxisFlags.Interpolate) != 0)
            {
                // Interpolate scale
                Vector3 updateScale = Vector3.Lerp(lerpScaleFrom, lerpScaleTo, t);

                // Update selected axis
                updateScale.x = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.ScaX) != 0) ? updateScale.x : thisTransform.localScale.x;
                updateScale.y = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.ScaY) != 0) ? updateScale.y : thisTransform.localScale.y;
                updateScale.z = ((serializeFlags & ReplayTransformFormatter.ReplayTransformSerializeFlags.ScaZ) != 0) ? updateScale.z : thisTransform.localScale.z;

                // Update scale
                thisTransform.localScale = updateScale;
            }
        }

        public override void OnReplaySerialize(ReplayState state)
        {
#if UNITY_EDITOR
            thisTransform = transform;

            // Update flags while in editor so that sample statistics can update
            if (Application.isPlaying == false)
                UpdateSerializeFlags();
#endif
            // Update formatter with data from this transform
            formatter.UpdateFromTransform(thisTransform, serializeFlags);

            // Serialize the data
            formatter.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
#if UNITY_EDITOR
            thisTransform = transform;
#endif

            // Update last values
            lerpPositionFrom = lerpPositionTo;
            lerpRotationFrom = lerpRotationTo;
            lerpScaleFrom = lerpScaleTo;

            // Deserialize the data
            formatter.OnReplayDeserialize(state);

            // Fetch values
            lerpPositionTo = formatter.Position;
            lerpRotationTo = formatter.Rotation;
            lerpScaleTo = formatter.Scale;

            // Update position if interpolation is disabled
            if((replayPosition & RecordAxisFlags.Interpolate) == 0)
            {
                // Update position
                formatter.SyncTransformPosition(thisTransform, serializeFlags);
            }

            // Update rotation if interpolation is disabled
            if((replayRotation & RecordAxisFlags.Interpolate) == 0)
            {
                // Update rotation
                formatter.SyncTransformRotation(thisTransform, serializeFlags);
            }

            // Update scale if interpolation is disabled
            if((replayScale & RecordAxisFlags.Interpolate) == 0)
            {
                // Update scale
                formatter.SyncTransformScale(thisTransform, serializeFlags);
            }
        }

        private void UpdateSerializeFlags() =>
            // Get packed flags value containing all serialize data
            serializeFlags = ReplayTransformFormatter.GetSerializeFlags(replayPosition, replayRotation, replayScale,
                positionSpace, rotationSpace, positionPrecision, rotationPrecision, scalePrecision);
    }
}
