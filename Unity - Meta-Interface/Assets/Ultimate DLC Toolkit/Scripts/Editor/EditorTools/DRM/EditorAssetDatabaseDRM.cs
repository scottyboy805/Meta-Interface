using DLCToolkit.BuildTools;
using DLCToolkit.DRM;
using DLCToolkit.Profile;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools.DRM
{
    public sealed class EditorAssetDatabaseDRM : IDRMProvider
    {
        // Private
        private List<DLCProfile> profiles = new List<DLCProfile>();
        private BuildTarget editorBuildTarget = 0;

        // Constructor
        public EditorAssetDatabaseDRM()
        {
            // Select build target
            switch(Application.platform)
            {
                case RuntimePlatform.WindowsEditor: editorBuildTarget = BuildTarget.StandaloneWindows64; break;
                case RuntimePlatform.LinuxEditor: editorBuildTarget = BuildTarget.StandaloneLinux64; break;
                case RuntimePlatform.OSXEditor: editorBuildTarget = BuildTarget.StandaloneOSX; break;
            }

            // Load profiles for current build target
            profiles.AddRange(DLCBuildPipeline.GetAllDLCProfiles(new[] { editorBuildTarget }));
        }

        // Methods
        public DLCAsync<string[]> GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {
            return DLCAsync<string[]>.Completed(true, profiles
                .Select(p => p.GetPlatform(editorBuildTarget).DlcUniqueKey)
                .ToArray());
        }

        public DLCAsync<DLCStreamProvider> GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Find DLC profile
            DLCProfile profile;
            DLCPlatformProfile platformProfile;
            GetProfileForPlatform(uniqueKey, out profile, out platformProfile);

            // Check for found
            string dlcLoadPath;
            if (platformProfile == null || File.Exists(dlcLoadPath = profile.GetPlatformOutputPath(editorBuildTarget)) == false)
                throw new IOException("Cannot load the DLC stream because the DLC has not yet been built for the current target: " + editorBuildTarget);

            // Open for reading
            return DLCAsync<DLCStreamProvider>.Completed(true, DLCStreamProvider.FromFile(dlcLoadPath));
        }

        public DLCAsync IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Find DLC profile
            DLCProfile profile;
            DLCPlatformProfile platformProfile;
            GetProfileForPlatform(uniqueKey, out profile, out platformProfile);

            // Check for found
            if (platformProfile == null || File.Exists(profile.GetPlatformOutputPath(editorBuildTarget)) == false)
                return DLCAsync.Completed(false).UpdateStatus("DLC is not available for loading because it has not yet been built for the current target: " + editorBuildTarget);

            // Must be available for loading
            return DLCAsync.Completed(true);
        }

        public DLCAsync RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // DLC is already installed for this DRM
            return DLCAsync.Completed(true);
        }

        public void RequestUninstallDLC(string uniqueKey)
        {
            // Do nothing
        }

        public void TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Do nothing
        }

        private void GetProfileForPlatform(string uniqueKey, out DLCProfile profile, out DLCPlatformProfile platformProfile)
        {
            DLCPlatformProfile platform = null;

            // Try to find profile
            profile = profiles.FirstOrDefault(p => 
                p.GetPlatform(editorBuildTarget) != null && 
                (platform = p.GetPlatform(editorBuildTarget))
                    .DlcUniqueKey == uniqueKey);

            // Get platform
            platformProfile = profile != null 
                ? platform
                : null;
        }
    }
}
