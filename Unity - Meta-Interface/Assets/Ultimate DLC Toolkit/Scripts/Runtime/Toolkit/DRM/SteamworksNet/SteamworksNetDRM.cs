#if DLCTOOLKIT_DRM_STEAMWORKSNET || DLCTOOLKIT_DRM_TEST_STEAMWORKSNET
using Steamworks;
using System;
using System.Collections;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A DRM provider to support DLC via the Steamworks platform using the popular Steamworks.Net Unity package.
    /// Supports listing all DLC unique keys even for DLC that the game is not aware of (DLC released after game).
    /// Supports ownership verification and auto-install by Steam client.
    /// Requires that the Steam client is running and Steamworks.Net has been successfully initialized.
    /// Must be explicitly enabled by installing Steamworks.Net and then defining `DLCTOOLKIT_DRM_STEAMWORKSNET` in the player settings.
    /// </summary>
    public sealed class SteamworksNetDRM : IDRMProvider
    {
        // Methods
        DLCAsync<string[]> IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {            
            // Check for steam running
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return DLCAsync<string[]>.Error("Steam is not running");
            }

            // Get count
            int count = SteamApps.GetDLCCount();

            // Create array
            string[] uniqueKeys = new string[count];

            // Process all
            for (int i = 0; i < count; i++)
            {
                // Get dlc metadata
                AppId_t dlcId;
                SteamApps.BGetDLCDataByIndex(i, out dlcId, out _, out _, 256);

                // Insert unique key
                uniqueKeys[i] = dlcId.m_AppId.ToString();
            }

            // Create result async
            return DLCAsync<string[]>.Completed(true, uniqueKeys)
                .UpdateStatus("DLC unique keys fetched from Steam!");
        }

        DLCAsync IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return DLCAsync.Error("Steam is not running!");
            }

            // Get steam id
            AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Check steam installed
            bool installed = SteamApps.BIsDlcInstalled(id);

            // Check installed
            return DLCAsync.Completed(installed)
                .UpdateStatus(installed ? "DLC is available!" : "DLC is not installed or is not owned");
        }

        DLCAsync<DLCStreamProvider> IDRMProvider.GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return DLCAsync<DLCStreamProvider>.Error("Steam is not running!");
            }

            // Get steam id
            AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Try to get install dir
            string folder;
            if (SteamApps.GetAppInstallDir(id, out folder, 256) > 0)
            {
                // Check for null
                if (string.IsNullOrEmpty(folder) == false)
                {
                    // Create directory
                    DLCDirectory directory = new DLCDirectory(folder);

                    // Try to find
                    string filePath = directory.GetDLCFile(uniqueKey);

                    // Create stream
                    if (filePath != null)
                        return DLCAsync<DLCStreamProvider>.Completed(true, DLCStreamProvider.FromFile(filePath))
                            .UpdateStatus("Steam DLC sourced from install location: " + filePath);
                }
            }
            else if (SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out folder, 256) > 0)
            {
                // Check for null
                if (string.IsNullOrEmpty(folder) == false)
                {
                    // Create directory - be sure to make it recursive since we are now scanning the whole game install folder
                    DLCDirectory directory = new DLCDirectory(folder, System.IO.SearchOption.AllDirectories);

                    // Try to find
                    string filePath = directory.GetDLCFile(uniqueKey);

                    // Create stream
                    if (filePath != null)
                        return DLCAsync<DLCStreamProvider>.Completed(true, DLCStreamProvider.FromFile(filePath))
                            .UpdateStatus("Steam DLC sourced from install location: " + filePath);
                }
            }
            return DLCAsync<DLCStreamProvider>.Error("DLC not found. The DLC may not be owned by the user, or may need to be installed: " + uniqueKey);
        }

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return DLCAsync.Error("Steam is not running!");
            }

            // Get steam id
            AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Try to install
            SteamApps.InstallDLC(id);

            // Track progress
            bool installing = SteamApps.GetDlcDownloadProgress(id, out _, out _);

            // Check for installing - Maybe already installed or does not exist
            if (installing == false)
                return DLCAsync.Error("DLC is not installing. It may already be installed or there may be an issue with the network");

            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(TrackInstallAsync(async, id));

            return async;
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            // Check for steam running
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return;
            }

            // Get steam id
            AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Try to install
            SteamApps.UninstallDLC(id);
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Check for steam running
            if (SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return;
            }

            // Get steam id
            AppId_t id = default;
            
            // Check for in use - 0 for not in use or app id for in use
            if(isInUse == true)
                id = UniqueKeyToSteamID(uniqueKey);

            // Set context
            SteamApps.SetDlcContext(id);
        }

        private AppId_t UniqueKeyToSteamID(string uniqueKey)
        {
            // Parse id
            uint id;
            uint.TryParse(uniqueKey, out id);

            // Get app id
            return new AppId_t(id);
        }

        private IEnumerator TrackInstallAsync(DLCAsync async, AppId_t id)
        {
            ulong downloaded = 0;
            ulong total = 0;

            // Track the progress
            while(SteamApps.GetDlcDownloadProgress(id, out downloaded, out total) == true)
            {
                // Update progress and status
                async.UpdateStatus("Installing DLC");
                async.UpdateProgress((int)downloaded, (int)total);

                // Wait a frame
                yield return null;
            }

            // Check for installed
            bool installed = SteamApps.BIsDlcInstalled(id);

            // Complete operation
            async.UpdateStatus(installed ? "DLC was successfully installed!" : "DLC could not be installed at this time");
            async.Complete(installed);
        }
    }
}
#endif