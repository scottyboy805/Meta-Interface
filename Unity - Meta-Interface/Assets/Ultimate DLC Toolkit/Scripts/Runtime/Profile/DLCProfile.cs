using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
using System.Linq;

[assembly: InternalsVisibleTo("DLCToolkit.BuildTools")]
[assembly: InternalsVisibleTo("DLCToolkit.EditorTools")]

namespace DLCToolkit.Profile
{
    /// <summary>
    /// Represents a build profile for a specified DLC containing most preferences and options relating to the build process.
    /// </summary>
    [CreateAssetMenu]
    public sealed class DLCProfile : ScriptableObject, ISerializationCallbackReceiver
    {
        // Internal
        [SerializeField]
        internal Texture2D assetIcon = null;
        [SerializeField]
        internal bool dlcContentPendingBuild = true;

        [SerializeField]
        internal long lastBuildTime = 0;
        [SerializeField]
        internal BuildTarget[] lastBuildTargets = null;
        [SerializeField]
        internal bool lastBuildSuccess = false;

        [SerializeField]
        internal string dlcGuid = Guid.NewGuid().ToString();
        [SerializeField]
        internal string dlcContentPath = "";
        [SerializeField]
        internal string dlcBuildPath = "DLC";
        [SerializeField]
        internal string dlcName = "New DLC 1";
        [SerializeField]
        internal string dlcVersionString = "1.0.0";
        [SerializeField]
        internal bool enabledForBuild = true;
        

        [Header("Metadata")]
        [SerializeField]
        [TextArea]
        internal string description = "";
        [SerializeField]
        internal string developer = "";
        [SerializeField]
        internal string publisher = "";
        [SerializeField]
        internal string[] tags = { };
        [SerializeField]
        internal DLCCustomMetadata customMetadata = null;

        [Header("Signing")]
        [SerializeField]
        private bool signDlc = true;
        [SerializeField]
        private bool signDlcVersion = false;

        [Header("Icons")]
        [SerializeField]
        internal Texture2D smallIcon = null;
        [SerializeField]
        internal Texture2D mediumIcon = null;
        [SerializeField]
        internal Texture2D largeIcon = null;
        [SerializeField]
        internal Texture2D extraLargeIcon = null;
        [SerializeField]        
        internal List<DLCCustomIcon> customIcons = new List<DLCCustomIcon>();

        [Header("Platform")]
        [SerializeField]
        [SerializeReference]
        internal DLCPlatformProfile[] platforms = null;

        // Properties
        /// <summary>
        /// Return a value indicating whether the DLC needs to be rebuilt due to modified assets or if the DLC was just created.
        /// Useful for knowing whether the DLC has changes to assets and a rebuild is required to sync the shipped DLC bundle.
        /// </summary>
        public bool DLCRebuildRequired
        {
            get { return dlcContentPendingBuild; }
        }

        /// <summary>
        /// Get the time stamp for the last build for this profile.
        /// </summary>
        public DateTime LastBuildTime
        {
            get { return DateTime.FromFileTime(lastBuildTime); }
        }

        /// <summary>
        /// Check if this profile has been built yet for any platform. 
        /// This will be true even if a build was attempted but failed due to errors.
        /// </summary>
        public bool HasLastBuildTime
        {
            get { return lastBuildTime != 0; }
        }

        /// <summary>
        /// Get all the build targets that we built as part of the last profile build request. 
        /// </summary>
        public BuildTarget[] LastBuildTargets
        {
            get { return lastBuildTargets; }
        }

        /// <summary>
        /// Get the result of the last build for this platform.
        /// True if the last build was successful or false if not.
        /// </summary>
        public bool LastBuildSuccess
        {
            get { return lastBuildSuccess; }
        }

        public int LastBuildPlatformCount
        {
            get { return lastBuildTargets != null ? lastBuildTargets.Length : 0; }
        }

        public int LastSuccessfulBuildPlatformCount
        {
            get
            {
                int count = 0;

                if(lastBuildTargets != null)
                {
                    foreach(BuildTarget target in lastBuildTargets)
                    {
                        if (GetPlatform(target).LastBuildSuccess == true)
                            count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Get the name of this DLC profile.
        /// </summary>
        public string DLCProfileName
        {
            get
            {
                if (string.IsNullOrEmpty(DLCName) == false)
                    return DLCName;

                return name;
            }
        }

        /// <summary>
        /// Get the guid for this DLC profile.
        /// </summary>
        public string DLCGuid
        {
            get { return dlcGuid; }
        }

        /// <summary>
        /// Get the content path for this DLC profile.
        /// The content path is the project asset location where DLC assets should be created.
        /// </summary>
        public string DLCContentPath
        {
            get { return dlcContentPath; }
        }

        /// <summary>
        /// Get the project asset path for this DLC profile asset.
        /// Available only in the editor and will return the path relative to the project folder including name and extension.
        /// </summary>
        public string DLCProfileAssetPath
        {
            get
            {
#if UNITY_EDITOR
                return AssetDatabase.GetAssetPath(this);
#else
                return null;
#endif
            }
        }

        /// <summary>
        /// The build path for this DLC profile.
        /// The build path is the optional output directory where built DLC wil be stored. 
        /// Default is 'DLC/', and you can pass null or empty string to use the default location.
        /// </summary>
        public string DLCBuildPath
        {
            get { return dlcBuildPath; }
            internal set
            {
                dlcBuildPath = value;

                // Check for empty
                if (string.IsNullOrEmpty(DLCContentPath) == true)
                    dlcBuildPath = "DLC";

                MarkAsModified();
            }
        }

        /// <summary>
        /// The name for this DLC content.
        /// </summary>
        public string DLCName
        {
            get { return dlcName; }
            internal set 
            { 
                dlcName = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The version string for this DLC content in the format X.X.X
        /// </summary>
        public string DLCVersionString
        {
            get { return dlcVersionString; }
            internal set
            {
                dlcVersionString = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The version for this DLC content.
        /// </summary>
        public Version DLCVersion
        {
            get
            {
                // Try to parse
                Version version;
                Version.TryParse(dlcVersionString, out version);

                // Get version
                return version;
            }
        }

        /// <summary>
        /// Is this DLC enabled for build.
        /// </summary>
        public bool EnabledForBuild
        {
            get { return enabledForBuild; }
            internal set
            {
                enabledForBuild = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// A short description for this DLC content.
        /// </summary>
        public string Description
        {
            get { return description; }
            internal set
            {
                description = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The developer or studio name that created this DLC content.
        /// </summary>
        public string Developer
        {
            get { return developer; }
            internal set
            {
                developer = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The publisher name that distributed this DLC content.
        /// </summary>
        public string Publisher
        {
            get { return publisher; }
            internal set
            {
                publisher = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The custom metadata for this DLC content, or null if no custom metadata has been assigned.
        /// </summary>
        public DLCCustomMetadata CustomMetadata
        {
            get { return customMetadata; }
            internal set
            {
#if UNITY_EDITOR
                // Check for changed
                if(customMetadata != null && value != customMetadata)
                {
                    // Remove from asset
                    AssetDatabase.RemoveObjectFromAsset(customMetadata);
                    DestroyImmediate(customMetadata);
                }

                // Check for new value
                if(value != customMetadata && value != null)
                {
                    // Add to asset
                    AssetDatabase.AddObjectToAsset(value, this);
                }
#endif

                customMetadata = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// Should the DLC content be signed to this game project.
        /// Signing means that the DLC will be encoded with data making it loadable only by this game project.
        /// </summary>
        public bool SignDLC
        {
            get { return signDlc; }
            internal set
            {
                signDlc = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// Should the DLC content be signed with version information to this game project.
        /// Version signing means that the DLC will signed to and loadable only by a specific version of this game project.
        /// </summary>
        public bool SignDLCVersion
        {
            get { return signDlcVersion; }
            internal set
            {
                signDlcVersion = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The small icon for the DLC content.
        /// </summary>
        public Texture2D SmallIcon
        {
            get { return smallIcon; }
            set
            {
                smallIcon = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The medium icon for the DLC content.
        /// </summary>
        public Texture2D MediumIcon
        {
            get { return mediumIcon; }
            set
            {
                mediumIcon = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The large icon for the DLC content.
        /// </summary>
        public Texture2D LargeIcon
        {
            get { return largeIcon; }
            set
            {
                largeIcon = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The extra large icon for the DLC content.
        /// </summary>
        public Texture2D ExtraLargeIcon
        {
            get { return extraLargeIcon; }
            set
            {
                extraLargeIcon = value;
                MarkAsModified();
            }
        }

        /// <summary>
        /// The custom icons list for the DLC content.
        /// </summary>
        public IList<DLCCustomIcon> CustomIcons
        {
            get { return customIcons; }
        }

        /// <summary>
        /// The available platform profiles for this DLC content.
        /// </summary>
        public DLCPlatformProfile[] Platforms
        {
            get { return platforms; }
        }

        // Methods
        private void Reset()
        {
            UpdateDLCPlatforms();
            UpdateDLCContentPath();
        }

        private void OnValidate()
        {
            UpdateDLCContentPath();
        }

        private void OnEnable()
        {
            UpdateDLCPlatforms();
            UpdateDLCContentPath();
        }

        /// <summary>
        /// Mark the profile as modified indicating that a build is required to sync the modified DLC assets with the built version.
        /// Call this when any asset associated with this DLC profile has been created, deleted, moved or modified in any way and a notification will be shown to indicate that the profile is out of sync and needs to be rebuilt.
        /// </summary>
        public void MarkAsModified()
        {
            dlcContentPendingBuild = true;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Check if any of the setup DLC platforms are enabled for build.
        /// </summary>
        /// <returns>True if any of the profile platforms are enabled or false if not</returns>
        public bool IsAnyPlatformEnabled()
        {
            foreach(DLCPlatformProfile platform in platforms)
            {
                if (platform.Enabled == true)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if this DLC profile has any of the provided build targets enabled for build.
        /// </summary>
        /// <param name="buildTargets">An array of build targets to check</param>
        /// <returns>True if any of the provided build targets are enabled for build or false if not</returns>
        public bool IsAnyPlatformEnabled(BuildTarget[] buildTargets)
        {
            foreach(BuildTarget buildTarget in buildTargets)
            {
                if(IsPlatformEnabled(buildTarget) == true)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if this DLC profile has the specified build target enabled for build.
        /// </summary>
        /// <param name="buildTarget">The build target to check</param>
        /// <returns>True if the build target is enabled for build or false if not</returns>
        public bool IsPlatformEnabled(BuildTarget buildTarget)
        {
            foreach(DLCPlatformProfile platform in platforms)
            {
                if (platform.Platform == buildTarget && platform.Enabled == true)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if the specified asset is associated with this DLC profile. Ie. will this asset be built into this DLC content bundle.
        /// </summary>
        /// <param name="asset">A reference to a loaded asset. Asset must have been created in the asset database</param>
        /// <returns>True is the asset is part of this DLC or false if not</returns>
        public bool IsAssetAssociated(UnityEngine.Object asset)
        {
            // Check for null
            if (asset == null)
                return false;

#if UNITY_EDITOR
            // Get asset path
            string assetPath = AssetDatabase.GetAssetPath(asset);

            // Check for invalid
            if (string.IsNullOrEmpty(assetPath) == true)
                return false;

            // Check for associated
            return IsAssetAssociated(assetPath);
#else
            throw new NotSupportedException("Cannot be called at runtime");
#endif
        }

        /// <summary>
        /// Check if the specified asset path is associated with this DLC profile. Ie. will this asset be built into this DLC content bundle.
        /// </summary>
        /// <param name="assetPath">The path relative to the project folder of the asset. Asset path must have been created in the asset database</param>
        /// <returns>True is the asset is part of this DLC or false if not</returns>
        public bool IsAssetAssociated(string assetPath)
        {
            // Check for empty
            if (string.IsNullOrEmpty(assetPath) == true)
                return false;

#if UNITY_EDITOR
            // Check for relative path
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid) == true)
                return false;

            // Check for sub asset
            return assetPath.StartsWith(DLCContentPath) == true;
#else
            throw new NotSupportedException("Cannot be called at runtime");
#endif
        }

        public string GetShipWithGameOutputPath(BuildTarget buildTarget)
        {
            // Try to find platform profile
            DLCPlatformProfile platformProfile = GetPlatform(buildTarget);

            // Check for any
            if (platformProfile == null || platformProfile.ShipWithGame == false)
                return null;

            // Build path
            return Path.Combine(platformProfile.ShipWithGamePath, DLCName + platformProfile.DLCExtension).Replace('\\', '/');
        }

        public string GetPlatformOutputPath(BuildTarget buildTarget, string overrideOutputFolder = null)
        {
            // Try to find platform profile
            DLCPlatformProfile platformProfile = GetPlatform(buildTarget);

            // Check for any
            if (platformProfile == null)
                return null;

            // Select output folder
            string outputPath = string.IsNullOrEmpty(overrideOutputFolder) == true
                ? DLCBuildPath
                : overrideOutputFolder;

            // Build path
            string outputFolder = string.IsNullOrEmpty(platformProfile.PlatformSpecificFolder) == false
                ? Path.Combine(outputPath, platformProfile.PlatformSpecificFolder)
                : outputPath;

            // Create the output file name
            return Path.Combine(outputFolder, DLCName + platformProfile.DLCExtension).Replace('\\', '/');
        }

        public string GetPlatformOutputFolder(BuildTarget buildTarget, string overrideOutputFolder = null)
        {
            // Try to find platform profile
            DLCPlatformProfile platformProfile = GetPlatform(buildTarget);

            // Check for any
            if (platformProfile == null)
                return null;

            // Select output folder
            string outputPath = string.IsNullOrEmpty(overrideOutputFolder) == true
                ? DLCBuildPath
                : overrideOutputFolder;

            // Get folder with platform specific
            if(string.IsNullOrEmpty(platformProfile.PlatformSpecificFolder) == false)
                return Path.Combine(outputPath, platformProfile.PlatformSpecificFolder).Replace('\\', '/');

            return outputPath.Replace('\\', '/');
            // Build path
            //return Path.Combine(outputPath, platformProfile.PlatformFriendlyName);
        }

        public DLCPlatformProfile GetPlatform(BuildTarget buildTarget)
        {
            foreach (DLCPlatformProfile platform in platforms)
            {
                if (platform.Platform == buildTarget)
                    return platform;
            }
            return null;
        }

        public string GetAssetRelativePath(string projectRelativeAssetPath)
        {
            // Check for invalid
            if (string.IsNullOrEmpty(projectRelativeAssetPath) == true)
                throw new ArgumentNullException("Asset path cannot be null or empty");

            // Get the content folder
            string contentFolder = DLCContentPath;

            // Check for asset part of this profile
            if (projectRelativeAssetPath.StartsWith(contentFolder) == false)
                return projectRelativeAssetPath;

            // Make relative
            try
            {
                return projectRelativeAssetPath.Remove(0, contentFolder.Length + 1);
            }
            catch { }
            return projectRelativeAssetPath;
        }

        internal void UpdateLastBuildTargets(BuildTarget[] buildBatchGroup)
        {
            // Check for null
            if (buildBatchGroup == null)
                buildBatchGroup = (BuildTarget[])Enum.GetValues(typeof(BuildTarget));

            List<BuildTarget> profileLastBuildTargets = new List<BuildTarget>(buildBatchGroup.Length);

            // Check all targets
            foreach(BuildTarget buildTarget in buildBatchGroup)
            {
                if(IsPlatformEnabled(buildTarget) == true)
                    profileLastBuildTargets.Add(buildTarget);
            }

            lastBuildTargets = profileLastBuildTargets.ToArray();
        }

        internal void UpdateDLCPlatforms()
        {
            if(platforms == null || platforms.Length == 0)
            {
                platforms = new DLCPlatformProfile[]
                {
                    CreatePlatformProfile(BuildTarget.StandaloneWindows64),
                    CreatePlatformProfile(BuildTarget.StandaloneLinux64),
                    CreatePlatformProfile(BuildTarget.StandaloneOSX),
                    CreatePlatformProfile(BuildTarget.iOS),
                    CreatePlatformProfile(BuildTarget.Android),
                    CreatePlatformProfile(BuildTarget.WebGL),
                    CreatePlatformProfile(BuildTarget.PS4),
                    CreatePlatformProfile(BuildTarget.PS5),
                    CreatePlatformProfile(BuildTarget.XboxOne),
                };

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        internal List<BuildTarget> GetMissingDLCPlatforms()
        {
            List<BuildTarget> result = new List<BuildTarget>();

            // Process all supported platforms
            if(GetPlatform(BuildTarget.StandaloneWindows64) == null) result.Add(BuildTarget.StandaloneWindows64);
            if (GetPlatform(BuildTarget.StandaloneLinux64) == null) result.Add(BuildTarget.StandaloneLinux64);
            if (GetPlatform(BuildTarget.StandaloneOSX) == null) result.Add(BuildTarget.StandaloneOSX);
            if (GetPlatform(BuildTarget.iOS) == null) result.Add(BuildTarget.iOS);
            if (GetPlatform(BuildTarget.Android) == null) result.Add(BuildTarget.Android);
            if (GetPlatform(BuildTarget.WebGL) == null) result.Add(BuildTarget.WebGL);
            if (GetPlatform(BuildTarget.PS4) == null) result.Add(BuildTarget.PS4);
            if (GetPlatform(BuildTarget.PS5) == null) result.Add(BuildTarget.PS5);
            if (GetPlatform(BuildTarget.XboxOne) == null) result.Add(BuildTarget.XboxOne);

            return result;
        }

        internal void DeleteDLCPlatform(BuildTarget target)
        {
            // Get the platform
            DLCPlatformProfile platform = GetPlatform(target);

            // Remove from platforms
            platforms = platforms.Where(p => p != platform).ToArray();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        internal void AddDLCPlatform(BuildTarget target)
        {
            // Create if supported - an exception will be thrown if not
            DLCPlatformProfile platform = CreatePlatformProfile(target);

            // Setup default / inherit options
            platform.Enabled = true;
            platform.DlcUniqueKey = platforms[platforms.Length - 1].DlcUniqueKey;

            // Add to platforms
            Array.Resize(ref platforms, platforms.Length + 1);

            // Insert as last entry
            platforms[platforms.Length - 1] = platform;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        internal DLCPlatformProfile CreatePlatformProfile(BuildTarget target)
        {
            switch(target)
            {
                case BuildTarget.StandaloneWindows64: return new DLCPlatformProfile(BuildTarget.StandaloneWindows64, "UNITY_STANDALONE_WIN");
                case BuildTarget.StandaloneLinux64: return new DLCPlatformProfile(BuildTarget.StandaloneLinux64, "UNITY_STANDALONE_LINUX");
                case BuildTarget.StandaloneOSX: return new DLCPlatformProfile(BuildTarget.StandaloneOSX, "UNITY_STANDALONE_OSX");
                case BuildTarget.iOS: return new DLCPlatformProfile(BuildTarget.iOS, "UNITY_IOS");
                case BuildTarget.Android: return new DLCPlatformProfileAndroid("UNITY_ANDROID");
                case BuildTarget.WebGL: return new DLCPlatformProfile(BuildTarget.WebGL, "UNITY_WEBGL");
                case BuildTarget.PS4: return new DLCPlatformProfile(BuildTarget.PS4, "UNITY_PS4");
                case BuildTarget.PS5: return new DLCPlatformProfile(BuildTarget.PS5, "UNITY_PS5");
                case BuildTarget.XboxOne: return new DLCPlatformProfile(BuildTarget.XboxOne, "UNITY_XBOXONE");
            }
            throw new NotSupportedException("Platform is not supported: " + target);
        }

        internal void UpdateDLCContentPath()
        {
#if UNITY_EDITOR
            // Update icon
            if (Application.isPlaying == false)
                EditorGUIUtility.SetIconForObject(this, assetIcon);


            // Get this asset path
            string path = AssetDatabase.GetAssetPath(this);

            if (string.IsNullOrEmpty(path) == false)
            {
                // Get parent
                string parentPath = Directory.GetParent(path).FullName;

                // Convert back to relative
                dlcContentPath = FileUtil.GetProjectRelativePath(parentPath.Replace('\\', '/'));
            }
#endif
        }

        internal void RemoveDisabledPlatforms()
        {
            List<DLCPlatformProfile> enabledPlatforms = new List<DLCPlatformProfile>(platforms.Length);

            // Find and add enabled platforms
            foreach(DLCPlatformProfile platformProfile in platforms)
            {
                if(platformProfile.Enabled == true)
                    enabledPlatforms.Add(platformProfile);
            }

            // Update platforms
            platforms = enabledPlatforms.ToArray();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Do nothing
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            foreach (DLCPlatformProfile platform in platforms)
            {
                // Add listener
                if (platform != null)
                    platform.OnProfileModified += MarkAsModified;
            }
        }
    }
}
