using System;
using UnityEngine;
using UnityEditor;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayRiggedGeneric))]
    public class ReplayRiggedGenericInspector : ReplayRecordableBehaviourInspector
    {
        // Private
        private SerializedProperty observedRootBone = null;
        private SerializedProperty observedBones = null;

        private SerializedProperty rootPosition = null;
        private SerializedProperty rootPositionPrecision = null;
        private SerializedProperty rootRotation = null;
        private SerializedProperty rootRotationPrecision = null;
        private SerializedProperty rootScale = null;
        private SerializedProperty rootScalePrecision = null;

        private SerializedProperty position = null;
        private SerializedProperty positionPrecision = null;
        private SerializedProperty rotation = null;
        private SerializedProperty rotationPrecision = null;
        private SerializedProperty scale = null;
        private SerializedProperty scalePrecision = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Find properties
            observedRootBone = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.observedRootBone));
            observedBones = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.observedBones));

            rootPosition = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.replayRootBonePosition));
            rootPositionPrecision = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.rootBonePositionPrecision));
            rootRotation = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.replayRootBoneRotation));
            rootRotationPrecision = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.rootBoneRotationPrecision));
            rootScale = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.replayRootBoneScale));
            rootScalePrecision = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.rootBoneScalePrecision));

            position = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.replayBonePosition));
            positionPrecision = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.bonePositionPrecision));
            rotation = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.replayBoneRotation));
            rotationPrecision = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.boneRotationPrecision));
            scale = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.replayBoneScale));
            scalePrecision = serializedObject.FindProperty(nameof(ReplayRiggedGeneric.boneScalePrecision));
        }

        public override void OnInspectorGUI()
        {
            float spaceUIWidth = 60;
            float precisionUIWidth = 44;

            // Display main properties
            DisplayDefaultInspectorProperties();

            // Check for changed
            EditorGUI.BeginChangeCheck();


            // Root bone section
            GUILayout.Space(6);
            GUILayout.Label("Replay Root Bone", EditorStyles.boldLabel);

            // Check for no root bone
            if (observedRootBone.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No observed root bone assigned!", MessageType.Warning);
            }
            else
            {
                // Draw position
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(rootPosition);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup(RecordSpace.Local, GUILayout.Width(spaceUIWidth));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.PropertyField(rootPositionPrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
                }
                GUILayout.EndHorizontal();

                // Draw rotation
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(rootRotation);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup(RecordSpace.Local, GUILayout.Width(spaceUIWidth));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.PropertyField(rootRotationPrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
                }
                GUILayout.EndHorizontal();

                // Draw scale
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(rootScale);
                    EditorGUILayout.PropertyField(rootScalePrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
                }
                GUILayout.EndHorizontal();
            }


            // Bones section
            GUILayout.Space(6);
            GUILayout.Label("Replay Bones", EditorStyles.boldLabel);

            // Check for no observed bones
            if (observedBones.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No observed bones assigned!", MessageType.Warning);
            }
            else
            {
                // Draw position
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(position);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup(RecordSpace.Local, GUILayout.Width(spaceUIWidth));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.PropertyField(positionPrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
                }
                GUILayout.EndHorizontal();

                // Draw rotation
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(rotation);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.EnumPopup(RecordSpace.Local, GUILayout.Width(spaceUIWidth));
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.PropertyField(rotationPrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
                }
                GUILayout.EndHorizontal();

                // Draw scale
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PropertyField(scale);
                    EditorGUILayout.PropertyField(scalePrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
                }
                GUILayout.EndHorizontal();
            }

            // Update changes
            if (EditorGUI.EndChangeCheck() == true)
                serializedObject.ApplyModifiedProperties();

            // Draw data statistics
            DisplayReplayStorageStatistics();
        }
    }
}
