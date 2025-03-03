#if DLCTOOLKIT_DRM_LOOTLOCKER || DLCTOOLKIT_DRM_TEST_LOOTLOCKER
using System;
using System.IO;
using System.Linq;
using LootLocker.Requests;
using UnityEngine;

namespace DLCToolkit.DRM
{
    public sealed class LootLockerDRM : IDRMProvider
    {
        // Private
        private DLCDirectory dlcInstallDirectory = null;

        // Constructor
        public LootLockerDRM(string dlcInstallDirectory = null)
        {
            // Use game install directory
            if (string.IsNullOrEmpty(dlcInstallDirectory) == true || Directory.Exists(dlcInstallDirectory) == false)
                dlcInstallDirectory = Directory.GetParent(Application.dataPath).FullName;

            // Create directory
            this.dlcInstallDirectory = new DLCDirectory(dlcInstallDirectory, SearchOption.AllDirectories);

            // Log message
            Debug.Log("Using LootLocker local install directory: " +  dlcInstallDirectory);


#if DLCTOOLKIT_DRM_TEST_LOOTLOCKER
            // Login as guest
            LootLockerSDKManager.StartGuestSession((LootLockerGuestSessionResponse response) =>
            {
                if(response.success == false)
                {
                    Debug.LogError("Error logging in as LootLocker guest user: " + response.text);
                }
            });
#endif
        }

        // Methods
        public DLCAsync<string[]> GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {
            // Does not support listing available DLC contents - fallback to using base game local dlc collection
            throw new NotSupportedException();
        }

        public DLCAsync<DLCStreamProvider> GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            throw new NotImplementedException();
        }

        public DLCAsync<bool> IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check active session
            if (LootLockerSDKManager.CheckInitialized() == false)
                return DLCAsync<bool>.Error("LootLocker is not initialized with current user session");

            // Create async
            DLCAsync<bool> async = new DLCAsync<bool>(false);

            // Request DLC
            LootLockerSDKManager.GetDLCMigrated((LootLockerDlcResponse response) =>
            {
                // Check for success
                if (response.success == true)
                {
                    // Complete operation
                    async.Complete(true, response.dlcs.Contains(uniqueKey));
                }
                else
                {
                    // Fail operation
                    async.Error(response.text);
                }
            });

            return async;
        }

        public DLCAsync RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check active session
            if (LootLockerSDKManager.CheckInitialized() == false)
                return DLCAsync.Error("LootLocker is not initialized with current user session");

            // Create async
            DLCAsync async = new DLCAsync(false);

            // Request migration
            LootLockerSDKManager.InitiateDLCMigration((LootLockerDlcResponse response) =>
            {
                // Check for success
                if(response.success == true)
                {
                    // Complete operation
                    async.Complete(true);
                }
                else
                {
                    // Fail operation
                    async.Error(response.text);
                }
            });

            return async;
        }

        public void RequestUninstallDLC(string uniqueKey)
        {
            throw new NotSupportedException();
        }

        public void TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            throw new NotSupportedException();
        }
    }
}
#endif