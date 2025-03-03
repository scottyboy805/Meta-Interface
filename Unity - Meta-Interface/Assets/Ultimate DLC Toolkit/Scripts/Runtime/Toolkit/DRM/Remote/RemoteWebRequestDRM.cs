using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A DRM provider for fetching DLC content using web requests.
    /// This provider relies on the <see cref="DLC.ManifestAsync"/> to fetch information about available DLC.
    /// </summary>
    public sealed class RemoteWebRequestDRM : IDRMProvider
    {
        // Methods
        DLCAsync<string[]> IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {
            throw new NotSupportedException();
        }

        DLCAsync IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync async = new DLCAsync(false);

            // Run async
            asyncProvider.RunAsync(IsDLCAvailableRoutine());
            IEnumerator IsDLCAvailableRoutine()
            {
                // Wait for manifest to be fetched
                yield return DLC.ManifestAsync;

                // Check for manifest
                if (DLC.ManifestAsync.IsSuccessful == false)
                {
                    async.Error("Could not fetch DLC manifest: " + DLC.ManifestAsync.Status);
                    yield break;
                }

                // Get the path
                string url = GetDLCUrl(uniqueKey, DLC.ManifestAsync.Result);

                // Check for any
                if (string.IsNullOrEmpty(url) == true)
                {
                    async.Error("DLC url path could not be determined! Only DLC known to the game and marked as `Ship With Game` will be loadable");
                    yield break;
                }

                // Create request
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    // Wait for completed
                    yield return request.SendWebRequest();

                    // Check for success
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Check for file found on server
                        async.Complete(request.responseCode < 400);
                    }
                    else
                    {
                        // Report error
                        async.Error(request.error);
                    }
                }
            };
            return async;
        }

        DLCAsync<DLCStreamProvider> IDRMProvider.GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync<DLCStreamProvider> async = new DLCAsync<DLCStreamProvider>(false);

            // Run async
            asyncProvider.RunAsync(GetDLCStreamRoutine());
            IEnumerator GetDLCStreamRoutine()
            {
                // Wait for manifest to be fetched
                yield return DLC.ManifestAsync;

                // Check for manifest
                if(DLC.ManifestAsync.IsSuccessful == false)
                {
                    async.Error("Could not fetch DLC manifest: " + DLC.ManifestAsync.Status);
                    yield break;
                }

                // Get the path
                string url = GetDLCUrl(uniqueKey, DLC.ManifestAsync.Result);

                // Check for any
                if (string.IsNullOrEmpty(url) == true)
                {
                    async.Error("DLC url path could not be determined! Only DLC known to the game and marked as `Ship With Game` will be loadable");
                    yield break;
                }

                // Create request
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    // Set handler
                    request.downloadHandler = new DownloadHandlerBuffer();

                    // Wait for completed
                    yield return request.SendWebRequest();

                    // Check for success
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        // Create stream
                        DLCStreamProvider stream = DLCStreamProvider.FromData(request.downloadHandler.data);

                        // Check for file found on server
                        async.Complete(true, stream);
                    }
                    else
                    {
                        // Report error
                        async.Error(request.error);
                    }
                }
            };
            return async;
        }        

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            throw new NotSupportedException();
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            throw new NotSupportedException();
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
        }

        private string GetDLCUrl(string uniqueKey, DLCManifest manifest)
        {
            // Try to find entry
            DLCManifestEntry entry = manifest.DLCContents.FirstOrDefault(d => d.DLCUniqueKey == uniqueKey);

            // Check for null or ship with game
            if (entry != null && entry.ShipWithGame == true)
            {
                // Get the Url or path for the ship with game content
                return entry.GetDLCLoadPath();
            }
            return null;
        }
    }
}
