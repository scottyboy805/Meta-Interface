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
        // Public
        /// <summary>
        /// The path where DLC content will be stored in editor mode.
        /// The editor will scan this folder for DLC content automatically.
        /// </summary>
        public string editorContentPath = "DLC";

        // Methods
        /// <summary>
        /// Get the DRM provider for the current platform.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public IDRMProvider GetDRMProvider()
        {
#if UNITY_EDITOR && !DLCTOOLKIT_DRM_TEST
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    {
                        // Get the path
                        string path = Path.Combine(editorContentPath, "Windows");

                        Debug.Log("Using editor DRM cache: " + path);
                        return new LocalDirectoryDRM(path);
                    }
                case RuntimePlatform.OSXEditor:
                    {
                        // Get the path
                        string path = Path.Combine(editorContentPath, "OSX");

                        Debug.Log("Using editor DRM cache: " + path);
                        return new LocalDirectoryDRM(path);
                    }

                case RuntimePlatform.LinuxEditor:
                    {
                        // Get the path
                        string path = Path.Combine(editorContentPath, "Linux");

                        Debug.Log("Using editor DRM cache: " + path);
                        return new LocalDirectoryDRM(path);
                    }
            }

            // Check for runtime platforms
#elif (UNITY_STANDALONE && DLCTOOLKIT_DRM_STEAMWORKSNET) || DLCTOOLKIT_DRM_TEST_STEAMWORKSNET
            Debug.Log("Using Steamworks.Net DRM");
            return new SteamworksNetDRM();            
#elif (UNITY_STANDALONE && DLCTOOLKIT_DRM_FACEPUNCHSTEAMWORKS) || DLCTOOLKITR_DRM_TEST_FACEPUNCHSTEAMWORKS
            Debug.Log("Using Facepunch.Steamworks DRM");
            return new FacepunchSteamworksDRM();
#elif (UNITY_ANDROID && DLCTOOLKIT_DRM_GOOGLEPLAY) || DLCTOOLKIT_DRM_TEST_GOOGLEPLAY
            Debug.Log("Using GooglePlayAssetDelivery DRM");
            return new GooglePlayAssetDeliveryDRM();
#endif

            // No DRM
            throw new NotSupportedException("DRM is not available on this platform");
        }
    }
}
