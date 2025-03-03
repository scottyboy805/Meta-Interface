using DLCToolkit.Profile;
using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace DLCToolkit.BuildTools.PlayerBuild
{
    [InitializeOnLoad]
    internal static class DLCOnBuildPlayer
    {
        // Constructor
        static DLCOnBuildPlayer()
        {
            // Take over build so that we can generate ship on build DLC
            BuildPlayerWindow.RegisterBuildPlayerHandler(OnRequestBuildPlayer);
        }

        // Methods
        private static void OnRequestBuildPlayer(BuildPlayerOptions options)
        {
            // Try to get directory
            string buildDirectory = options.locationPathName;

            // Check for extension
            if (Path.HasExtension(buildDirectory) == true)
                buildDirectory = Directory.GetParent(options.locationPathName).FullName;

            // Start build of shipping DLC - pre build step
            DLCBuildResult dlcBuildResult = DLCBuildPipeline.BuildAllDLCShipWithGameContent(options.target, true, buildDirectory);

            // Create streaming assets folder
            if (Directory.Exists(DLCBuildPipeline.StreamingAssetsPath) == false)
                Directory.CreateDirectory(DLCBuildPipeline.StreamingAssetsPath);

            // Create manifest streaming file
            DLCBuildPipeline.GetProjectManifest(options.target)
                .ToJsonFile(DLCBuildPipeline.StreamingAssetsPath + "/" + DLCManifest.ManifestName);


            // Check for error
            if (dlcBuildResult.AllSuccessful == false)
            {
                // Show dialog
                if (EditorUtility.DisplayDialog("There were errors building DLC content!", "One or more `Ship With Game` DLC content could not be built. Do you want to continue building the standalone player, or cancel the operation?", "Continue", "Cancel") == false)
                {
                    UnityEngine.Debug.LogError("Error building DLC Content!");
                    return;
                }
            }

            // Build the player as normal
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);

            // Attempt to install in build directory after player build has completed
            DLCBuildPipeline.InstallDLCShipWithGameContent(dlcBuildResult.BuildTasks.ToArray(), buildDirectory);


            // Cleanup files after build
            CleanupStreamingContent(dlcBuildResult, buildDirectory);
        }

        private static void CleanupStreamingContent(DLCBuildResult buildResult, string buildDirectory)
        {
            // Delete the manifest - it is generated per platform so makes no sense to keep it in project between builds
            string manifestPath = DLCBuildPipeline.StreamingAssetsPath + "/" + DLCManifest.ManifestName;

            // Delete the file
            if (File.Exists(manifestPath) == true)
            {
                // Delete the manifest
                File.Delete(manifestPath);

                // Delete meta file if found
                if (File.Exists(manifestPath + ".meta") == true)
                    File.Delete(manifestPath + ".meta");
            }

            // Get all streaming profiles for the c
            foreach(DLCBuildTask buildTask in buildResult.BuildTasks)
            {
                // Get the platform
                DLCPlatformProfile platformProfile = buildTask.PlatformProfile;

                // Check for ship with game
                if (platformProfile.ShipWithGame == false || platformProfile.ShipWithGameDirectory != ShipWithGameDirectory.StreamingAssets)
                    continue;

                // Get file name
                string dlcFileName = Path.GetFileName(buildTask.OutputPath);

                // Get install path
                string shipWithGamePath = Path.Combine(DLCBuildPipeline.StreamingAssetsPath, platformProfile.ShipWithGamePath, dlcFileName);

                UnityEngine.Debug.Log("Path: " + shipWithGamePath);
                // Delete file if found
                if (File.Exists(shipWithGamePath) == true)
                    File.Delete(shipWithGamePath);
            }
        }
    }
}
