#if DLCTOOLKIT_DRM_STEAMWORKSNET || DLCTOOLKIT_DRM_TEST_STEAMWORKSNET
using Steamworks;
using System;
using System.Collections;
using UnityEngine;

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
        // Properties
        DLCAsync<string[]> IDRMProvider.DLCUniqueKeysAsync
        {
            get
            {
                // Check for steam running
                if (Steamworks.SteamAPI.IsSteamRunning() == false)
                {
                    Debug.LogWarning("Steam is not running!");
                    return DLCAsync<string[]>.Completed(true, Array.Empty<string>());
                }

                // Get count
                int count = Steamworks.SteamApps.GetDLCCount();

                // Create array
                string[] uniqueKeys = new string[count];

                // Process all
                for(int i = 0; i < count; i++)
                {
                    // Get dlc metadata
                    Steamworks.AppId_t dlcId;
                    Steamworks.SteamApps.BGetDLCDataByIndex(i, out dlcId, out _, out _, 256);

                    // Insert unique key
                    uniqueKeys[i] = dlcId.m_AppId.ToString();
                }

                // Create result async
                return DLCAsync<string[]>.Completed(true, uniqueKeys);
            }
        }

        // Methods
        DLCAsync<bool> IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return DLCAsync<bool>.Error("Steam is not running!");
            }

            // Get steam id
            Steamworks.AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Check installed
            return DLCAsync<bool>.Completed(true, Steamworks.SteamApps.BIsDlcInstalled(id));
        }

        DLCStreamProvider IDRMProvider.GetDLCStream(string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return null;
            }

            // Get steam id
            Steamworks.AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Try to get install dir
            string folder;
            if (Steamworks.SteamApps.GetAppInstallDir(id, out folder, 256) > 0)
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
                        return DLCStreamProvider.FromFile(filePath);
                }
            }
            else if (Steamworks.SteamApps.GetAppInstallDir(SteamUtils.GetAppID(), out folder, 256) > 0)
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
                        return DLCStreamProvider.FromFile(filePath);
                }
            }
            return null;
        }

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return DLCAsync.Error("Steam is not running!");
            }

            // Get steam id
            Steamworks.AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Try to install
            Steamworks.SteamApps.InstallDLC(id);

            // Track progress
            bool installing = Steamworks.SteamApps.GetDlcDownloadProgress(id, out _, out _);

            // Check for installing - Maybe already installed or does not exist
            if (installing == false)
                return DLCAsync.Completed(false);

            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(TrackInstallAsync(async, id));

            return async;
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return;
            }

            // Get steam id
            Steamworks.AppId_t id = UniqueKeyToSteamID(uniqueKey);

            // Try to install
            Steamworks.SteamApps.UninstallDLC(id);
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Check for steam running
            if (Steamworks.SteamAPI.IsSteamRunning() == false)
            {
                Debug.LogWarning("Steam is not running!");
                return;
            }

            // Get steam id
            Steamworks.AppId_t id = default;
            
            // Check for in use - 0 for not in use or app id for in use
            if(isInUse == true)
                id = UniqueKeyToSteamID(uniqueKey);

            // Set context
            Steamworks.SteamApps.SetDlcContext(id);
        }

        private Steamworks.AppId_t UniqueKeyToSteamID(string uniqueKey)
        {
            // Parse id
            uint id;
            uint.TryParse(uniqueKey, out id);

            // Get app id
            return new Steamworks.AppId_t(id);
        }

        private IEnumerator TrackInstallAsync(DLCAsync async, Steamworks.AppId_t id)
        {
            ulong downloaded = 0;
            ulong total = 0;

            // Track the progress
            while(Steamworks.SteamApps.GetDlcDownloadProgress(id, out downloaded, out total) == true)
            {
                // Update progress
                async.UpdateProgress((int)downloaded, (int)total);

                // Wait a frame
                yield return null;
            }

            // Complete operation
            async.Complete(Steamworks.SteamApps.BIsDlcInstalled(id));
        }
    }
}
#endif