using DLCToolkit.BuildTools;
using DLCToolkit.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace DLCToolkit.EditorTools.DRM
{
    internal sealed class EditorTestModeDRM
    {
        [PostProcessScene]
        private static void OnEnterPlayMode()
        {
            // This event can be called during the build process, but we only need to receive it when entering play mode
            if (Application.isPlaying == false)
                return;

            // Initialize test environment
            if(DLC.Config.EnableEditorTestMode == true)
            {
                // Setup test mode
                DLC.RegisterDRMEditorTestMode(new EditorAssetDatabaseDRM());
            }

            // Register the manifest
            DLC.RegisterEditorManifest(CreateEditorManifest());
        }

        private static DLCManifest CreateEditorManifest()
        {
            BuildTarget editorBuildTarget = 0;

            // Select build target
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor: editorBuildTarget = BuildTarget.StandaloneWindows64; break;
                case RuntimePlatform.LinuxEditor: editorBuildTarget = BuildTarget.StandaloneLinux64; break;
                case RuntimePlatform.OSXEditor: editorBuildTarget = BuildTarget.StandaloneOSX; break;
            }

            // Get the manifest
            return DLCBuildPipeline.GetProjectManifest(editorBuildTarget, true);
        }
    }
}
