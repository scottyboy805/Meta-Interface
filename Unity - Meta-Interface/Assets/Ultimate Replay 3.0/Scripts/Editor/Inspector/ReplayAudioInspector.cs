using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayAudio))]
    public class ReplayAudioInspector : ReplayRecordableBehaviourInspector
    {
        // Private
        private SerializedProperty observedAudio = null;

        private SerializedProperty pitch = null;
        private SerializedProperty volume = null;
        private SerializedProperty stereoPan = null;
        private SerializedProperty spatialBlend = null;
        private SerializedProperty reverbZoneMix = null;
        private SerializedProperty interpolate = null;
        private SerializedProperty lowPrecision = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Find properties
            observedAudio = serializedObject.FindProperty(nameof(ReplayAudio.observedAudio));

            pitch = serializedObject.FindProperty(nameof(ReplayAudio.replayPitch));
            volume = serializedObject.FindProperty(nameof(ReplayAudio.replayVolume));
            stereoPan = serializedObject.FindProperty(nameof(ReplayAudio.replaySteroPan));
            spatialBlend = serializedObject.FindProperty(nameof(ReplayAudio.replaySpatialBlend));
            reverbZoneMix = serializedObject.FindProperty(nameof(ReplayAudio.replayReverbZoneMix));
            interpolate = serializedObject.FindProperty(nameof(ReplayAudio.interpolate));
            lowPrecision = serializedObject.FindProperty(nameof(ReplayAudio.recordPrecision));
        }

        public override void OnInspectorGUI()
        {
            // Display main properties
            DisplayDefaultInspectorProperties();

            // Check for observed audio
            if (observedAudio.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(observedAudio.displayName + " must have a valid audio source assigned!", MessageType.Warning);
            }
            else
            {
                // Display extra options
                EditorGUI.BeginChangeCheck();

                // Draw properties
                EditorGUILayout.PropertyField(pitch);
                EditorGUILayout.PropertyField(volume);
                EditorGUILayout.PropertyField(stereoPan);
                EditorGUILayout.PropertyField(spatialBlend);
                EditorGUILayout.PropertyField(reverbZoneMix);
                EditorGUILayout.PropertyField(interpolate);
                EditorGUILayout.PropertyField(lowPrecision);

                // Update changed
                if (EditorGUI.EndChangeCheck() == true)
                    serializedObject.ApplyModifiedProperties();
            }

            // Display replay stats
            DisplayReplayStorageStatistics();
        }
    }
}
