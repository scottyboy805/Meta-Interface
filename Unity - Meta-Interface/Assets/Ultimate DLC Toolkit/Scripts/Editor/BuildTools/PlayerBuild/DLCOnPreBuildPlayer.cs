using System.IO;
using UnityEditor;

namespace DLCToolkit.BuildTools.PlayerBuild
{
    [InitializeOnLoad]
    internal static class DLCOnPreBuildPlayer
    {
        // Constructor
        static DLCOnPreBuildPlayer()
        {
            // Take over build so that we can generate ship on build DLC
            BuildPlayerWindow.RegisterBuildPlayerHandler((BuildPlayerOptions options) =>
            {
                // Try to get directory
                string buildDirectory = options.locationPathName;
                
                // Check for extension
                if(Path.HasExtension(buildDirectory) == true)
                    buildDirectory = Directory.GetParent(options.locationPathName).FullName;

                // Start build of shipping DLC
                DLCBuildPipeline.BuildAllDLCShipWithGameContent(options.target, true, buildDirectory);

                // Build the player as normal
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
            });
        }
    }
}
