using System.IO;
using UnityEngine;

namespace DLCToolkit.Profile
{
    public sealed class DLCPlatformProfileAndroid : DLCPlatformProfile
    {
        // Private
        [SerializeField]
        private string dlcAssetPackDirectory = "Assets/Android DLC";
        [SerializeField]
        private string deliveryType = "fast-follow";

        // Properties
        /// <summary>
        /// The DLC custom asset pack directory.
        /// </summary>
        public string DLCAssetPackDirectory
        {
            get { return dlcAssetPackDirectory; }
            set { dlcAssetPackDirectory = value; }
        }

        /// <summary>
        /// The DLC delivery type.
        /// </summary>
        public string DeliveryType
        {
            get { return deliveryType; } 
            set { deliveryType = value; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="platformDefines">An array of platform defines</param>
        public DLCPlatformProfileAndroid(params string[] platformDefines)
            : base(UnityEditor.BuildTarget.Android, platformDefines)
        {
        }

        // Methods
        /// <summary>
        /// Get the Android custom asset pack path for the DLC platform.
        /// </summary>
        /// <param name="dlcName">The dlc name</param>
        /// <returns>The asset pack path</returns>
        public string GetDLCCustomAssetPackPath(string dlcName)
        {
            return Path.Combine(dlcAssetPackDirectory, dlcName + ".androidpack");
        }

        /// <summary>
        /// Get the Android gradle build file path for the DLC platform.
        /// </summary>
        /// <param name="dlcName">The dlc name</param>
        /// <returns>The gradle file path</returns>
        public string GetDLCCustomAssetPackGradlePath(string dlcName)
        {
            return Path.Combine(GetDLCCustomAssetPackPath(dlcName), "build.gradle");
        }

        /// <summary>
        /// Get the Android gradle build file contents for the DLC platform.
        /// </summary>
        /// <param name="dlcName">The dlc name</param>
        /// <returns>The gradle file contents</returns>
        public string GetDLCustomAssetGradleContents(string dlcName)
        {
            string contents =
            @"apply plugin: 'com.android.asset-pack'
            assetPack {
                packName = ""dlc_packname_key""
                dynamicDelivery {
                    deliveryType = ""dlc_deliverytype_key""
                }
            }";

            contents = contents.Replace("dlc_packname_key", dlcName);
            contents = contents.Replace("dlc_deliverytype_key", deliveryType);

            return contents;
        }
    }
}
