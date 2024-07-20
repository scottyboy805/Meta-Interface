using DLCToolkit.Profile;
using System;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    internal class DLCOptionsWizardPage : DLCWizardPage
    {
        // Private
        private static readonly GUIContent signingLabel = new GUIContent("DLC Signing", "Should the DLC content be signed during build so that it will only be loadable by this game project");
        private static readonly GUIContent signingWithVersionLabel = new GUIContent("DLC Version Signing", "Should the DLC content be version signed, meaning that it will only be loadable by the version of the game project it was built for");
        private static readonly GUIContent useCompressionLabel = new GUIContent("Use Compression", "Should the DLC be built with compression for the selected platform");
        private static readonly GUIContent strictBuildLabel = new GUIContent("Strict Build", "Should the DLC asset bundles be built in strict mode for the selected platform");
        private static readonly GUIContent preloadSharedAssetsLabel = new GUIContent("Preload Shared Assets", "Should the DLC preload shared assets when it is loaded into the game for the selected platform. Shared assets will be loaded on demand if disabled");
        private static readonly GUIContent preloadSceneAssetsLabel = new GUIContent("Preload Scene Assets", "Should the DLC preload scene assets when it is loaded into the game for the selected platform. Scene assets will be loaded on demand if disabled");


        private bool tableStyle = true;

        // Properties
        public override string PageName => "Options";

        // Constructor
        public DLCOptionsWizardPage(DLCProfile profile)
            : base(profile) 
        {
        }

        // Methods
        public override void OnGUI()
        {
            EditorGUILayout.HelpBox("Signing ensures that the DLC content can only be loaded by this game project. Can be changed later", MessageType.Info);

            // Signing
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(signingLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                bool result = EditorGUILayout.Toggle(Profile.SignDLC);

                // Check for changed
                if (result != Profile.SignDLC)
                    Profile.SignDLC = result;
            }
            GUILayout.EndHorizontal();

            // Sign with version
            EditorGUI.BeginDisabledGroup(Profile.SignDLC == false);
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(signingWithVersionLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                bool result = EditorGUILayout.Toggle(Profile.SignDLCVersion);

                // Check for changed
                if (result != Profile.SignDLCVersion)
                    Profile.SignDLCVersion = result;
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();


            // Use compression
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(useCompressionLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                bool result = EditorGUILayout.Toggle(Profile.Platforms[0].UseCompression);

                // Check for changed
                if (result != Profile.Platforms[0].UseCompression)
                {
                    foreach(DLCPlatformProfile platformProfile in Profile.Platforms)
                        platformProfile.UseCompression = result;
                }
            }
            GUILayout.EndHorizontal();

            // Strict build
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(strictBuildLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                bool result = EditorGUILayout.Toggle(Profile.Platforms[0].StrictBuild);

                // Check for changed
                if (result != Profile.Platforms[0].StrictBuild)
                {
                    foreach (DLCPlatformProfile platformProfile in Profile.Platforms)
                        platformProfile.StrictBuild = result;
                }
            }
            GUILayout.EndHorizontal();

            // Preload shared assets
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(preloadSharedAssetsLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                bool result = EditorGUILayout.Toggle(Profile.Platforms[0].PreloadSharedAssets);

                // Check for changed
                if (result != Profile.Platforms[0].PreloadSharedAssets)
                {
                    foreach (DLCPlatformProfile platformProfile in Profile.Platforms)
                        platformProfile.PreloadSharedAssets = result;
                }
            }
            GUILayout.EndHorizontal();

            // Preload scene assets
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(preloadSceneAssetsLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                bool result = EditorGUILayout.Toggle(Profile.Platforms[0].PreloadSceneAssets);

                // Check for changed
                if (result != Profile.Platforms[0].PreloadSceneAssets)
                {
                    foreach (DLCPlatformProfile platformProfile in Profile.Platforms)
                        platformProfile.PreloadSceneAssets = result;
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
