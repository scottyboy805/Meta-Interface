using DLCToolkit.BuildTools.Format;
using DLCToolkit.BuildTools.Scripting;
using DLCToolkit.Format;
using DLCToolkit.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.BuildTools
{
    internal sealed class DLCBuildContext
    {
        // Internal
        internal bool isFaulted = false;
        internal DateTime platformBuildStartTime;

        // Private
        private string profilePath = null;
        private DLCProfile profile = null;
        private DLCPlatformProfile platformProfile = null;
        private string platformFriendlyName = null;

        private DLCBuildAssetCollection buildAssets = new DLCBuildAssetCollection();
        private DLCBuildScriptCollection scriptCollection = new DLCBuildScriptCollection();

        // Properties
        public DLCProfile Profile
        {
            get { return profile; }
        }

        public DLCPlatformProfile PlatformProfile
        {
            get { return platformProfile; }
        }

        public DLCBuildScriptCollection ScriptCollection
        {
            get { return scriptCollection; }
        }

        public string SharedAssetsBundleName
        {
            get 
            { 
                return (profile.DLCName.Replace(" ", "") 
                    + "-" + platformProfile.PlatformFriendlyName.ToLower() 
                    + "-SharedAssets").ToLower(); 
            }
        }

        public string SceneAssetsBundleName
        {
            get 
            { 
                return (profile.DLCName.Replace(" ", "") 
                    + "-" + platformProfile.PlatformName.ToLower() 
                    + "-SceneAssets").ToLower(); }
        }

        // Constructor
        public DLCBuildContext(DLCProfile profile, DLCPlatformProfile platformProfile)
        {
            this.profilePath = AssetDatabase.GetAssetPath(profile);
            this.profile = profile;
            this.platformProfile = platformProfile;
            this.platformFriendlyName = DLCPlatformProfile.GetFriendlyPlatformName(platformProfile.Platform);
        }

        // Methods
        public bool ValidateBuildProfile()
        {
            Debug.Log("Checking platform profile...");
            bool valid = true;

            // Check name
            if(string.IsNullOrEmpty(profile.DLCName) == true)
            {
                Debug.LogError("DLC name is empty: " + profile.name);
                valid = false;
            }

            // Check guid
            if(string.IsNullOrEmpty(profile.DLCGuid) == true || profile.DLCGuid.Length != new Guid().ToString().Length)
            {
                Debug.LogError("DLC guid is invalid. This should never happen - please contact support: " + profile.DLCName);
                valid = false;
            }

            // Check content path
            if(string.IsNullOrEmpty(profile.DLCContentPath) == true || Directory.Exists(profile.DLCContentPath) == false)
            {
                Debug.LogError("DLC content path is invalid. This should never happen - please contact support: " + profile.DLCName);
                valid = false;
            }

            // Check version
            if(profile.DLCVersion == null)
            {
                Debug.LogError("DLC version is invalid. Please use the numeric format `X.X.X`: " + profile.DLCName);
                valid = false;
            }


            // Check platform profile
            if(string.IsNullOrEmpty(platformProfile.DlcUniqueKey) == true)
            {
                Debug.LogError("DLC platform unique key is empty: " + profile.DLCName + ", " + platformProfile.PlatformName);
                valid = false;
            }

            // All values are valid
            return valid;
        }

        public bool CollectBuildAssets(IList<AssetBundleBuild> bundleBuilds, bool scriptingSupported)
        {
            Debug.Log("Collecting DLC assets...");

            // Get platform exclude folders
            IList<string> platformExcludeDirectories = PlatformProfile.GetPlatformExcludeDirectories();

            // Scan directory - exclude meta files
            string[] assetFiles = Directory.GetFiles(Path.GetFullPath(profile.DLCContentPath), "*.*", SearchOption.AllDirectories)
                .Where(f => Path.GetExtension(f) != ".meta")
                .ToArray();

            int current = 0;

            // Process all files
            foreach(string assetFile in assetFiles)
            {
                // Check if asset is inside excluded folder
                if (IsSpecialFolderExcluded(platformExcludeDirectories, assetFile) == true)
                    continue;

                // Create the build asset
                DLCBuildAsset asset = buildAssets.AddBuildAsset(assetFile);

                // Check if asset is excluded
                if (asset != null)
                {
                    // Update progress
                    //EditorUtility.DisplayProgressBar(string.Format("Discover DLC Content ({0} / {1})", current, assetFiles.Length),
                    //    asset.RelativePath, Mathf.InverseLerp(0, assetFiles.Length, current));

                    // Check for icon asset
                    if (IsIconAssetExcluded(profile, asset.RelativePath) == true)
                    {
                        asset.ExcludeFromDLC();
                        continue;
                    }

                    // Check for non-script asset
                    if (asset.IsScriptAsset == false)
                    {
                        // Report status
                        Debug.Log("Add asset to DLC: " + asset.RelativePath);
                    }
                    // Scripts must be added in a different way
                    else
                    {
                        if (scriptingSupported == true)
                        {
                            // Add script
                            scriptCollection.AddIncludeScriptSource(asset.RelativePath);
                        }
                    }
                }

                current++;
            }

            // Hide progress
            //EditorUtility.ClearProgressBar();

            // Check for shared assets
            if(buildAssets.HasAssetContent(DLCContentFlags.Assets) == true)
            {
                // Create the build
                AssetBundleBuild build = new AssetBundleBuild
                {
                    // Set bundle name
                    assetBundleName = SharedAssetsBundleName,

                    // Get all asset paths
                    assetNames = buildAssets.GetSharedAssets().Select(a => a.RelativePath).ToArray(),
                };

                // Shared asset count
                Debug.Log(string.Format("Registering shared assets ({0})...", build.assetNames.Length));

                // Register build
                bundleBuilds.Add(build);
            }

            // Check for scene assets
            if(buildAssets.HasAssetContent(DLCContentFlags.Scenes) == true)
            {
                // Create the build
                AssetBundleBuild build = new AssetBundleBuild
                {
                    // Set bundle name
                    assetBundleName = SceneAssetsBundleName,

                    // Get all scene paths
                    assetNames = buildAssets.GetSceneAssets().Select(a => a.RelativePath).ToArray(),
                };

                // Shared asset count
                Debug.Log(string.Format("Registering scene assets ({0})...", build.assetNames.Length));

                // Register build
                bundleBuilds.Add(build);
            }

            // Get all builds
            return buildAssets.AssetCount > 0;
        }

        public bool ValidateBundleContent(AssetBundleManifest manifest)
        {
            Debug.Log("Checking asset bundle build status...");

            if(manifest == null)
            {
                Debug.LogError("Failed to build DLC asset bundles");
                return false;
            }

            // Get all bundle names
            string[] bundleNames = manifest.GetAllAssetBundles()
                .Select(p => Path.GetFileNameWithoutExtension(p))
                .ToArray();

            // Check for shared assets
            if(buildAssets.HasAssetContent(DLCContentFlags.Assets) == true)
            {
                // Check for built bundle with name
                if(Array.Exists(bundleNames, n => n == SharedAssetsBundleName) == false)
                {
                    Debug.LogError("Failed to build DLC shared assets bundle. Check console for errors: " + profile.DLCName);
                    return false;
                }
            }

            // Check for scene assets
            if(buildAssets.HasAssetContent(DLCContentFlags.Scenes) == true)
            {
                // Check for built bundle with name
                if (Array.Exists(bundleNames, n => n == SceneAssetsBundleName) == false)
                {
                    Debug.LogError("Failed to build DLC scene assets bundle. Check console for errors: " + profile.DLCName);
                    return false;
                }
            }

            // All bundles were built successfully
            return true;
        }

        public DLCBuildBundle BuildDLCContent(string bundleFolder, bool scriptingSupported, bool scriptingDebug)
        {
            Debug.Log("Preparing to generate DLC content...");

            // Get platform
            RuntimePlatform platform = GetRuntimePlatformTarget();

            // Create content flags
            DLCBundle.ContentFlags flags = 0;
            
            // Content types
            if ((buildAssets.ContentFlags & DLCContentFlags.Assets) != 0) flags |= DLCBundle.ContentFlags.SharedAssets;
            if ((buildAssets.ContentFlags & DLCContentFlags.Scenes) != 0) flags |= DLCBundle.ContentFlags.SceneAssets;
            if ((buildAssets.ContentFlags & DLCContentFlags.Scripts) != 0 && scriptingSupported == true) flags |= DLCBundle.ContentFlags.ScriptAssets;

            // DLC signing
            if (profile.SignDLC == true) flags |= DLCBundle.ContentFlags.Signed;
            if (profile.SignDLC == true && profile.SignDLCVersion == true) flags |= DLCBundle.ContentFlags.SignedWithVersion;

            // Preload options
            if (platformProfile.PreloadSharedAssets == true) flags |= DLCBundle.ContentFlags.PreloadSharedBundle;
            if (platformProfile.PreloadSceneAssets == true) flags |= DLCBundle.ContentFlags.PreloadSceneBundle;

            // Create the bundle
            DLCBuildBundle bundle = new DLCBuildBundle(platform, flags);

            // Create the metadata
            DLCBuildMetadata metadata = new DLCBuildMetadata(
                new DLCNameInfo(
                    profile.DLCName,
                    platformProfile.DlcUniqueKey,
                    profile.DLCVersion),
                profile.DLCGuid,
                profile.Description,
                profile.Developer,
                profile.Publisher,
                DLC.ToolkitVersion,
                Application.unityVersion,
                buildAssets.ContentFlags,
                platformProfile.ShipWithGame,
                profile.CustomMetadata);

            // Register metadata
            Debug.Log("Register metadata...");
            bundle.AddContentEntry(DLCBundle.ContentType.Metadata, metadata);

            // Register icons
            Debug.Log("Register icons...");
            DLCBuildIconSet iconSet = new DLCBuildIconSet(profile);
            bundle.AddContentEntry(DLCBundle.ContentType.IconSet, iconSet);


            // Register scripts
            if (scriptingSupported == true && scriptCollection.HasScriptAssemblies == true)
            {
                Debug.Log("Register scripts...");
                DLCBuildScriptAssembly scriptAssembly = new DLCBuildScriptAssembly(scriptCollection, scriptingDebug);
                bundle.AddContentEntry(DLCBundle.ContentType.ScriptAssembly, scriptAssembly);

                // Cleanup temp files
                scriptCollection.CompilationBatch.CleanupCompilationDirectory();
            }


            // Register shared asset metadata
            Debug.Log("Register shared assets metadata...");
            DLCBuildContentBundleMetadata sharedAssetsMetadata = new DLCBuildContentBundleMetadata(profile.DLCContentPath);

            // Register all shared assets
            foreach(DLCBuildAsset sharedAsset in buildAssets.GetSharedAssets())
            {
                sharedAssetsMetadata.AddAssetMetadata(sharedAsset.RelativePath);
            }

            // Add entry
            bundle.AddContentEntry(DLCBundle.ContentType.SharedAssetMetadata, sharedAssetsMetadata);


            // Register scene asset metadata
            Debug.Log("Register scene assets metadata...");
            DLCBuildContentBundleMetadata sceneAssetsMetadata = new DLCBuildContentBundleMetadata(profile.DLCContentPath);

            // Register all scene assets
            foreach(DLCBuildAsset sceneAsset in buildAssets.GetSceneAssets())
            {
                sceneAssetsMetadata.AddAssetMetadata(sceneAsset.RelativePath);
            }

            // Add entry
            bundle.AddContentEntry(DLCBundle.ContentType.SceneAssetMetadata, sceneAssetsMetadata);


            // Register shared assets bundle
            if (buildAssets.HasAssetContent(DLCContentFlags.Assets) == true)
            {
                // Create bundle
                DLCBuildContentBundle contentBundle = new DLCBuildContentBundle(Path.Combine(bundleFolder, SharedAssetsBundleName), 0);

                // Add entry
                Debug.Log("Register shared assets bundle...");
                bundle.AddContentEntry(DLCBundle.ContentType.SharedAssetBundle, contentBundle);
            }

            // Register scene assets bundle
            if(buildAssets.HasAssetContent(DLCContentFlags.Scenes) == true)
            {
                // Create bundle
                DLCBuildContentBundle contentBundle = new DLCBuildContentBundle(Path.Combine(bundleFolder, SceneAssetsBundleName), 0);

                // Add entry
                Debug.Log("Register scene assets bundle...");
                bundle.AddContentEntry(DLCBundle.ContentType.SceneAssetBundle, contentBundle);
            }


            // Get bundle
            return bundle;
        }

        private RuntimePlatform GetRuntimePlatformTarget()
        {
            switch(platformProfile.Platform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return RuntimePlatform.WindowsPlayer;
                case BuildTarget.StandaloneLinux64: return RuntimePlatform.LinuxPlayer;
                case BuildTarget.StandaloneOSX: return RuntimePlatform.OSXPlayer;
                case BuildTarget.Android: return RuntimePlatform.Android;
                case BuildTarget.iOS: return RuntimePlatform.IPhonePlayer;
                case BuildTarget.WebGL: return RuntimePlatform.WebGLPlayer;
                case BuildTarget.PS4: return RuntimePlatform.PS4;
                case BuildTarget.PS5: return RuntimePlatform.PS5;
                case BuildTarget.XboxOne: return RuntimePlatform.XboxOne;
            }
            throw new NotSupportedException("Build platform is not supported: " +  platformProfile.Platform);
        }

        internal void ReloadProfileFromAssetDatabase()
        {
            profile = AssetDatabase.LoadAssetAtPath<DLCProfile>(profilePath);
        }

        internal static bool IsSpecialFolderExcluded(IList<string> platformExcludeFolders, string assetPath)
        {
            // Get relative path
            string relativePath = Path.IsPathRooted(assetPath) == true
                ? FileUtil.GetProjectRelativePath(assetPath.Replace('\\', '/'))
                : assetPath;

            // Get all folders
            string[] folders = relativePath.Split('/');

            // Check for special folders - note that we skip the last string because it contains the asset file name
            for(int i = 0; i < folders.Length - 1; i++)
            {
                // Check for special name
                if (string.Compare(folders[i], "Editor") == 0
                    || string.Compare(folders[i], "Exclude") == 0)
                {
                    // Folders are excluded
                    return true;
                }

                // Check for platform specific folder
                if (platformExcludeFolders.Contains(folders[i]) == true)
                {
                    // Other platform folder is excluded for this platform
                    return true;
                }
            }

            // Folder is not excluded
            return false;
        }

        internal static bool IsSpecialPlatformFolder(string assetPath, out string platformFolder)
        {
            // Get relative path
            string relativePath = Path.IsPathRooted(assetPath) == true
                ? FileUtil.GetProjectRelativePath(assetPath.Replace('\\', '/'))
                : assetPath;

            // Get all folders
            string[] folders = relativePath.Split('/');

            // Check for special folders - note that we skip the last string because it contains the asset file name
            for (int i = 0; i < folders.Length - 1; i++)
            {
                if (DLCPlatformProfile.friendlyPlatformNames.Contains(folders[i]) == true)
                {
                    platformFolder = folders[i];
                    return true;
                }
            }
            platformFolder = null;
            return false;
        }

        internal static bool IsIconAssetExcluded(DLCProfile profile, string assetPath)
        {
            // Get guid
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            // Check all assigned icons
            if(IsMatchAssetGuid(profile.SmallIcon, assetGuid) == true ||
                IsMatchAssetGuid(profile.MediumIcon, assetGuid) == true ||
                IsMatchAssetGuid(profile.LargeIcon, assetGuid) == true ||
                IsMatchAssetGuid(profile.ExtraLargeIcon, assetGuid) == true)
                return true;

            // Check for custom icons
            foreach(DLCCustomIcon icon in profile.CustomIcons)
            {
                if (icon.CustomIcon != null && IsMatchAssetGuid(icon.CustomIcon, assetGuid) == true)
                    return true;
            }
            return false;
        }

        private static bool IsMatchAssetGuid(UnityEngine.Object asset, string guid)
        {
            // Check for null asset
            if (asset == null)
                return false;

            // Try to get the guid
            string assetGuid; long localID;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out assetGuid, out localID);

            // Check for match
            return assetGuid == guid;
        }

        //private bool IsIconAssetExcluded(DLCBuildAsset asset)
        //{
        //    // Get the loaded asset
        //    UnityEngine.Object mainAsset = asset.MainAsset;

        //    // Check for null
        //    if (mainAsset == null)
        //        return false;

        //    // Check for icons
        //    if (profile.SmallIcon == mainAsset ||
        //        profile.MediumIcon == mainAsset ||
        //        profile.LargeIcon == mainAsset ||
        //        profile.ExtraLargeIcon == mainAsset)
        //        return true;

        //    // Check for custom icons
        //    foreach(DLCCustomIcon icon in profile.CustomIcons)
        //    {
        //        if (icon.CustomIcon == mainAsset)
        //            return true;
        //    }

        //    // Asset is not an excluded icon
        //    return false;
        //}
    }
}
