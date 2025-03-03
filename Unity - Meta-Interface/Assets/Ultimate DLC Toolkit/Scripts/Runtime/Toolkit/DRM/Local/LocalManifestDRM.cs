using System;
using System.Collections;
using System.Linq;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A local DRM provider that uses the DLC manifest created at each build to determine which DLC content is available to the game.
    /// For this reason, only DLC content known to the game at build time will be available as part of this DRM.
    /// Does not support ownership verification and will simply report that any DLC shipped with the game is owned by the user and can be loaded.
    /// Supported on any platform with IO support (Not WebGL for example) and may be useful for free DLC or expansion packs that can be simply dropped into a game folder and be auto-detected.
    /// </summary>
    public class LocalManifestDRM : IDRMProvider
    {
        // Methods
        public DLCAsync<string[]> GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {
            // Create async
            DLCAsync<string[]> async = new DLCAsync<string[]>();

            // Run async
            asyncProvider.RunAsync(GetDLCUniqueKeysRoutine());
            IEnumerator GetDLCUniqueKeysRoutine()
            {
                // Wait for manifest to be fetched
                yield return DLC.ManifestAsync;

                // Check for manifest
                if (DLC.ManifestAsync.IsSuccessful == false)
                {
                    async.Error("Could not fetch DLC manifest: " + DLC.ManifestAsync.Status);
                    yield break;
                }

                // Get manifest keys
                string[] dlcUniqueKeys = DLC.ManifestAsync.Result.DLCContents
                    .Select(d => d.DLCUniqueKey)
                    .ToArray();

                // Complete operation
                async.Complete(true, dlcUniqueKeys);
            };
            return async;
        }

        public DLCAsync IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync async = new DLCAsync();

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

                // Try to find manifest entry
                DLCManifestEntry entry = DLC.ManifestAsync.Result.DLCContents.FirstOrDefault(d => d.DLCUniqueKey == uniqueKey);

                // Check for shipped with game
                async.Complete(entry != null && entry.ShipWithGame);
            };
            return async;            
        }

        public DLCAsync<DLCStreamProvider> GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync<DLCStreamProvider> async = new DLCAsync<DLCStreamProvider>();

            // Run async
            asyncProvider.RunAsync(GetDLCStreamRoutine());
            IEnumerator GetDLCStreamRoutine()
            {
                // Wait for manifest to be fetched
                yield return DLC.ManifestAsync;

                // Check for manifest
                if (DLC.ManifestAsync.IsSuccessful == false)
                {
                    async.Error("Could not fetch DLC manifest: " + DLC.ManifestAsync.Status);
                    yield break;
                }

                // Try to find entry
                DLCManifestEntry entry = DLC.ManifestAsync.Result.DLCContents.FirstOrDefault(d => d.DLCUniqueKey == uniqueKey);

                // Check for null or ship with game
                if (entry != null && entry.shipWithGame == true)
                {
                    // Get the load path
                    string dlcPath = entry.GetDLCLoadPath();

#if UNITY_EDITOR || (!UNITY_WEBGL && !UNITY_ANDROID)
                    async.Complete(true, DLCStreamProvider.FromFile(dlcPath));
#else               
                    async.Error("Not supported on this platform!");
#endif
                }

                async.Error("DLC not found: " + uniqueKey);
            };
            return async;            
        }

        public DLCAsync RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            throw new NotSupportedException();
        }

        public void RequestUninstallDLC(string uniqueKey)
        {
            throw new NotSupportedException();
        }

        public void TrackDLCUsage(string uniqueKey, bool isInUse)
        {
        }
    }
}
