using System;
using UnityEngine;
using UnityEditor;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayTransform))]
    public class ReplayTransformInspector : ReplayRecordableBehaviourInspector
    {
        // Private
        private SerializedProperty position = null;
        private SerializedProperty positionSpace = null;
        private SerializedProperty positionPrecision = null;
        private SerializedProperty rotation = null;
        private SerializedProperty rotationSpace = null;
        private SerializedProperty rotationPrecision = null;
        private SerializedProperty scale = null;
        private SerializedProperty scalePrecision = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Find properties
            position = serializedObject.FindProperty(nameof(ReplayTransform.replayPosition));
            positionSpace = serializedObject.FindProperty(nameof(ReplayTransform.positionSpace));
            positionPrecision = serializedObject.FindProperty(nameof(ReplayTransform.positionPrecision));
            rotation = serializedObject.FindProperty(nameof(ReplayTransform.replayRotation));
            rotationSpace = serializedObject.FindProperty(nameof(ReplayTransform.rotationSpace));
            rotationPrecision = serializedObject.FindProperty(nameof(ReplayTransform.rotationPrecision));
            scale = serializedObject.FindProperty(nameof(ReplayTransform.replayScale));
            scalePrecision = serializedObject.FindProperty(nameof(ReplayTransform.scalePrecision));
        }

        public override void OnInspectorGUI()
        {
            float spaceUIWidth = 60;
            float precisionUIWidth = 44;

            // Display main properties
            DisplayDefaultInspectorProperties();

            // Check for changed
            EditorGUI.BeginChangeCheck();

            // Draw position
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(position);
                EditorGUILayout.PropertyField(positionSpace, GUIContent.none, GUILayout.Width(spaceUIWidth));
                EditorGUILayout.PropertyField(positionPrecision, GUIContent.none, GUILayout.Width(precisionUIWidth));
            }
            GUILayout.EndHorizontal();

            // Draw rotation
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(rotation);
                EditorGUILayout.PropertyField(rotationSpace, GUIContent.none, GUILayout.Width(spaceUIWidth));
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

            // Update changes
            if (EditorGUI.EndChangeCheck() == true)
                serializedObject.ApplyModifiedProperties();

            // Draw data statistics
            DisplayReplayStorageStatistics();
        }
    }
}