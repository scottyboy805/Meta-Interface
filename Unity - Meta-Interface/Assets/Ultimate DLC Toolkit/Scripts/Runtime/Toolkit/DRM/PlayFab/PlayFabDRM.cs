#if DLCTOOLKIT_DRM_PLAYFAB || DLCTOOLKIT_DRM_TEST_PLAYFAB
using PlayFab;
using PlayFab.ClientModels;
using System;
using UnityEngine;

namespace DLCToolkit.DRM
{
    public sealed class PlayFabDRM : IDRMProvider
    {
        // Private
        private const string dlcContentType = "binary/octet-stream";

        private LocalManifestDRM manifestDRM = new LocalManifestDRM();

        // Constructor
        public PlayFabDRM()
        {
#if DLCTOOLKIT_DRM_TEST_PLAYFAB
            // Check for login
            if (PlayFabClientAPI.IsClientLoggedIn() == false)
            {
                // Login for testing
                PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
                    {
                        CustomId = "DevTestUser",
                        CreateAccount = true,
                    },
                    (LoginResult result) =>
                    {
                        // Do nothing
                    },
                    (PlayFabError error) =>
                    {
                        Debug.LogError("Error logging in as PlayFab Test user: " + error.ToString());
                    });
            }
#endif
        }

        // Methods
        public DLCAsync<string[]> GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {
            return manifestDRM.GetDLCUniqueKeysAsync(asyncProvider);
        }

        public DLCAsync<DLCStreamProvider> GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Check for logged in
            if (PlayFabClientAPI.IsClientLoggedIn() == false)
                return DLCAsync<DLCStreamProvider>.Error("User is not logged into PlayFab. Content delivery network is not available unless logged in!");

            // Create async result
            DLCAsync<DLCStreamProvider> async = new DLCAsync<DLCStreamProvider>();            

            // Build the url
            string urlKey = Application.platform.ToString() + "/" + uniqueKey;

            // Update status
            async.UpdateStatus("Fetching DLC download url for key: " + urlKey);

            // Request content url
            PlayFabClientAPI.GetContentDownloadUrl(new GetContentDownloadUrlRequest
            { 
                Key = urlKey,
                ThruCDN = true,
            }, 
            (GetContentDownloadUrlResult result) =>
            {
                // Get the download url
                string downloadUrl = result.URL;


            },
            (PlayFabError error) =>
            {
                async.Error("DLC download url could not be resolved: " + error.ToString());
            });

            // Get a
            return async;
        }

        public DLCAsync<bool> IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            throw new NotImplementedException();
        }

        public DLCAsync RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            throw new NotImplementedException();
        }

        public void RequestUninstallDLC(string uniqueKey)
        {
            throw new NotImplementedException();
        }

        public void TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            throw new NotSupportedException();
        }
    }
}
#endif