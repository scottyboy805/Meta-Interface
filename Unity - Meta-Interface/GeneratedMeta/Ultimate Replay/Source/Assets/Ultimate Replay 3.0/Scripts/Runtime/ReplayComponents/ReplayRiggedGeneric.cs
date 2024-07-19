/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/ReplayComponents/ReplayRiggedGeneric.cs
/// </summary>
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
        // Public
        public Transform observedRootBone;
        public Transform[] observedBones;
        // Properties
        public override ReplayFormatter Formatter
        {
            get => throw new System.NotImplementedException();
        }

        public RecordAxisFlags ReplayBonePosition
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public RecordPrecision BonePositionPrecision
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public RecordAxisFlags ReplayBoneRotation
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public RecordPrecision BoneRotationPrecision
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public RecordAxisFlags ReplayBoneScale
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        public RecordPrecision BoneScalePrecision
        {
            get => throw new System.NotImplementedException();
            set => throw new System.NotImplementedException();
        }

        // Methods
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update serialize flags
            UpdateSerializeFlags();
        }
#endif
        protected override void Reset() => throw new System.NotImplementedException();
        protected override void Awake() => throw new System.NotImplementedException();
        public void AutoDetectRigBones() => throw new System.NotImplementedException();
        protected override void OnReplayReset() => throw new System.NotImplementedException();
        protected override void OnReplayStart() => throw new System.NotImplementedException();
        protected override void OnReplayUpdate(float t) => throw new System.NotImplementedException();
        public override void OnReplaySerialize(ReplayState state) => throw new System.NotImplementedException();
        public override void OnReplayDeserialize(ReplayState state) => throw new System.NotImplementedException();
    }
}