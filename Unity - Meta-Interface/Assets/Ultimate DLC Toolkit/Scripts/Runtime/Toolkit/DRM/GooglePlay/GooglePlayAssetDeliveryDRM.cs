#if DLCTOOLKIT_DRM_GOOGLEPLAY || DLCTOOLKIT_DRM_TEST_GOOGLEPLAY
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A DRM provider to support DLC via the GooglePlay store using Google Play Asset Delivery.
    /// Supports listing DLC unique keys but may not list all DLC content available on the store. Instead only content available at the time of building the game may be available.
    /// Supports ownership verification and auto-install or on demand install by the Play store.
    /// Must be explicitly enabled by defining `DLCTOOLKIT_DRM_GOOGLEPLAY` in the player settings.
    /// </summary>
    public sealed class GooglePlayAssetDeliveryDRM : IDRMProvider
    {
        // Properties
        DLCAsync<string[]> IDRMProvider.DLCUniqueKeysAsync
        {
            get
            {
                // Get count
                string[] uniqueKeys = AndroidAssetPacks.GetCoreUnityAssetPackNames();

                // Log warning
                Debug.LogWarning("All available DLC may not be listed on this platform unless game APK is up to date");

                return DLCAsync<string[]>.Completed(true, uniqueKeys);
            }
        }

        // Methods
        DLCStreamProvider IDRMProvider.GetDLCStream(string uniqueKey)
        {
            // Try to get dlc path
            string path = AndroidAssetPacks.GetAssetPackPath(uniqueKey);

            // Check for null
            if (path != null)
                return DLCStreamProvider.FromFile(path);

            return null;
        }

        DLCAsync<bool> IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync<bool> async = new DLCAsync<bool>();

            // Run async
            asyncProvider.RunAsync(TrackStateAsync(async, uniqueKey));

            return async;
        }

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(TrackInstallAsync(async, uniqueKey));

            return async;
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            // Cancel any downloads
            AndroidAssetPacks.CancelAssetPackDownload(new string[] { uniqueKey });

            // Remove pack
            AndroidAssetPacks.RemoveAssetPack(uniqueKey);
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Do nothing
        }

        private IEnumerator TrackStateAsync(DLCAsync<bool> async, string uniqueKey)
        {
            // Create request
            GetAssetPackStateAsyncOperation operation = AndroidAssetPacks.GetAssetPackStateAsync(new string[] { uniqueKey });

            // Wait for completed
            yield return operation;

            // Get the state
            AndroidAssetPackState state = operation.states[0];

            // Check for error
            if(state.error != AndroidAssetPackError.NoError)
            {
                async.Error(state.error.ToString());
                yield break;
            }

            // Check status
            async.Complete(true, state.status == AndroidAssetPackStatus.Completed);
        }

        private IEnumerator TrackInstallAsync(DLCAsync async, string uniqueKey)
        {
            // Create request
            DownloadAssetPackAsyncOperation operation = AndroidAssetPacks.DownloadAssetPackAsync(new string[] { uniqueKey });

            // Wait for completed
            while(operation.isDone == false)
            {
                // Update progress
                async.UpdateProgress(operation.progress);

                // Wait a frame
                yield return null;
            }

            // Complete operation
            async.Complete(Array.Exists(operation.downloadedAssetPacks, k => k == uniqueKey));
        }
    }
}
#endif