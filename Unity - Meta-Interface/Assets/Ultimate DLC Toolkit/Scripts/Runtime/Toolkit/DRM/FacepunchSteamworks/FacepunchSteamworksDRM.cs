#if DLCTOOLKIT_DRM_FACEPUNCHSTEAMWORKS || DLCTOOLKIT_DRM_TEST_FACEPUNCHSTEAMWORKS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A DRM provider to support DLC via the Steamworks platform using the popular Facepunch.Steamworks Unity package.
    /// Supports listing all DLC unique keys even for DLC that the game is not aware of (DLC released after game).
    /// Supports ownership verification and auto-install by Steam client.
    /// Requires that the Steam client is running and Facepunch.Steamworks has been successfully initialized.
    /// Must be explicitly enabled by installing Facepunch.Steamworks and then defining `DLCTOOLKIT_DRM_FACEPUNCHSTEAMWORKS` in the player settings.
    /// </summary>
    public sealed class FacepunchSteamworksDRM : IDRMProvider
    {
        // Properties
        DLCAsync<string[]> IDRMProvider.DLCUniqueKeysAsync
        {
            get
            {
                // Check for steam running
                if (Steamworks.SteamClient.IsValid == false)
                {
                    Debug.WriteLine("Steam is not running!");
                    return DLCAsync<string[]>.Completed(true, Array.Empty<string>());
                }

                List<string> uniqueKeys = new List<string>();

                // Get dlc count
                foreach (Steamworks.Data.DlcInformation info in Steamworks.SteamApps.DlcInformation())
                {
                    // Add unique key which is appid for steam
                    uniqueKeys.Add(info.AppId.Value.ToString());
                }

                // Get async result
                return DLCAsync<string[]>.Completed(true, uniqueKeys.ToArray());
            }
        }

        // Methods
        DLCStreamProvider IDRMProvider.GetDLCStream(string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamClient.IsValid == false)
            {
                Debug.WriteLine("Steam is not running!");
                return null;
            }

            // Get steam id
            Steamworks.AppId id = UniqueKeyToSteamID(uniqueKey);

            // Try to get install folder
            string folder = Steamworks.SteamApps.AppInstallDir(id);

            // Check for null
            if(string.IsNullOrEmpty(folder) == false)
            {
                // Create directory
                DLCDirectory directory = new DLCDirectory(folder);

                // Try to find
                string file = directory.GetDLCFileFromUniqueKey(uniqueKey);

                // Check for null
                if (file != null)
                    return DLCStreamProvider.FromFile(file);
            }
            return null;
        }

        DLCAsync<bool> IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamClient.IsValid == false)
            {
                Debug.WriteLine("Steam is not running!");
                return DLCAsync<bool>.Error("Steam is not running!");
            }

            // Get steam id
            Steamworks.AppId id = UniqueKeyToSteamID(uniqueKey);

            // Check installed
            return DLCAsync<bool>.Completed(true, Steamworks.SteamApps.IsDlcInstalled(id));
        }

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamClient.IsValid == false)
            {
                Debug.WriteLine("Steam is not running!");
                return DLCAsync<bool>.Error("Steam is not running!");
            }

            // Get steam id
            Steamworks.AppId id = UniqueKeyToSteamID(uniqueKey);

            // Try to install
            Steamworks.SteamApps.InstallDlc(id);

            // Track progress
            Steamworks.Data.DownloadProgress progress = Steamworks.SteamApps.DlcDownloadProgress(id);

            // Check for installing - Maybe already installed or does not exist
            if (progress.Active == false)
                return DLCAsync.Completed(false);

            // Create async
            DLCAsync async = new DLCAsync();

            // RUn async
            asyncProvider.RunAsync(TrackInstallAsync(async, id));

            return async;
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            // Check for steam running
            if (Steamworks.SteamClient.IsValid == false)
            {
                Debug.WriteLine("Steam is not running!");
                return;
            }

            // Get steam id
            Steamworks.AppId id = UniqueKeyToSteamID(uniqueKey);

            // Request uninstall
            Steamworks.SteamApps.UninstallDlc(id);
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Do nothing
        }

        private Steamworks.AppId UniqueKeyToSteamID(string uniqueKey)
        {
            // Parse id
            uint id;
            uint.TryParse(uniqueKey, out id);

            // Get app id
            return new Steamworks.AppId { Value = id };
        }

        private IEnumerator TrackInstallAsync(DLCAsync async, Steamworks.AppId id)
        {
            Steamworks.Data.DownloadProgress progress;

            // Track progress
            do
            {
                // Get progress
                progress = Steamworks.SteamApps.DlcDownloadProgress(id);

                // Update progress
                async.UpdateProgress((int)progress.BytesDownloaded, (int)progress.BytesTotal);

                // Wait a frame
                if (progress.Active == true)
                    yield return null;
            }
            while (progress.Active == true);

            // Complete operation
            async.Complete(Steamworks.SteamApps.IsDlcInstalled(id));
        }
    }
}
#endif