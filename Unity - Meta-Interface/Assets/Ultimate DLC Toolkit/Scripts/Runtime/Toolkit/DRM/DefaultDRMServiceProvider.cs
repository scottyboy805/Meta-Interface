#if DLCTOOLKIT_DRM_TEST_STEAMWORKSNET || DLCTOOLKIT_DRM_TEST_FACEPUNCHSTEAMWORKS || DLCTOOLKIT_DRM_TEST_GOOGLEPLAY
#define DLCTOOLKIT_DRM_TEST
#endif

using System;
using System.IO;
using UnityEngine;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A DRM service provider that supports editor local DRM, steamworks, and google play DRM services.
    /// </summary>
    public sealed class DefaultDRMServiceProvider : IDRMServiceProvider
    {
        // Private        
        private string contentPath = null;

        // Properties
        /// <summary>
        /// The path where DLC content will be loaded from when targeting desktop standalone.
        /// Defaults to `install_path/DLC`
        /// </summary>
        public string ContentPath
        {
            get
            {
                // Get parent folder
                if(contentPath == null)
                    contentPath = Directory.GetParent(Application.dataPath).FullName + "/DLC";

                return contentPath;
            }
            set
            {
                contentPath = value;
            }
        }

        // Methods
        /// <summary>
        /// Get the DRM provider for the current platform.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public IDRMProvider GetDRMProvider()
        {
            
            // Check for runtime platforms
#if UNITY_WEBGL
            Debug.Log("Using remote web request DRM");
            return new RemoteWebRequestDRM();
//#elif DLCTOOLKIT_DRM_PLAYFAB || DLCTOOLKIT_DRM_TEST_PLAYFAB
//            Debug.Log("Using PlayFab DRM");
//            return new PlayFabDRM();
#elif DLCTOOLKIT_DRM_LOOTLOCKER || DLCTOOLKIT_DRM_TEST_LOOTLOCKER
            Debug.Log("Using LootLocker DRM");
            return new LootLockerDRM();
#elif (UNITY_STANDALONE && DLCTOOLKIT_DRM_STEAMWORKSNET) || DLCTOOLKIT_DRM_TEST_STEAMWORKSNET
            Debug.Log("Using Steamworks.Net DRM");
            return new SteamworksNetDRM();            
#elif (UNITY_STANDALONE && DLCTOOLKIT_DRM_FACEPUNCHSTEAMWORKS) || DLCTOOLKITR_DRM_TEST_FACEPUNCHSTEAMWORKS
            Debug.Log("Using Facepunch.Steamworks DRM");
            return new FacepunchSteamworksDRM();
#elif (UNITY_ANDROID && DLCTOOLKIT_DRM_GOOGLEPLAY) || DLCTOOLKIT_DRM_TEST_GOOGLEPLAY
            Debug.Log("Using GooglePlayAssetDelivery DRM");
            return new GooglePlayAssetDeliveryDRM();
#elif UNITY_STANDALONE
            Debug.Log("Using local directory DRM");
            return new LocalDirectoryDRM(ContentPath);
#endif

            // No DRM
            throw new NotSupportedException("DRM is not available on this platform");
        }
    }
}
