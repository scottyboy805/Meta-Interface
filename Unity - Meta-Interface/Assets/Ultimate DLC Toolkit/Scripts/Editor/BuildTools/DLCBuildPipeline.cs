using System.Collections.Generic;
using UnityEngine;
using DLCToolkit.Format;
using DLCToolkit.Profile;
using UnityEditor;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq;
using DLCToolkit.BuildTools.Events;

[assembly: InternalsVisibleTo("DLCToolkit.EditorTools")]

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Custom build options for building DLC content.
    /// Multiple options can be combined.
    /// </summary>
    [Flags]
    public enum DLCBuildOptions
    {
        /// <summary>
        /// No options selected.
        /// </summary>
        None = 0,
        /// <summary>
        /// Should DLC profiles which are disabled be included in the build.
        /// </summary>
        IncludeDisabledDLC = 1,        
        /// <summary>
        /// Should a total rebuild be forced disregarding any cached or incremental build artifacts.
        /// Will usually cause the build to take much longer but wil ensure that content is refreshed.
        /// </summary>
        ForceRebuild = 2,
        /// <summary>
        /// Prevent DLC content from being built with scripting support.
        /// </summary>
        DisableScripting = 16,
        /// <summary>
        /// Attempt to build supported scripting DLC in debug mode with appropriate debug symbols so that the script debugger can be attached when loading DLC content at runtime.
        /// Only supported on windows platforms.
        /// </summary>
        DebugScripting = 32,


        /// <summary>
        /// Should the force rebuild option be taken from the quick select build menu option (Tools -> DLC Toolkit -> Build Config -> Force Rebuild).
        /// </summary>
        ForceRebuild_UseConfig = 1024,
        /// <summary>
        /// Should the script debugging option be taken from the quick select build menu option (Tools -> DLC Toolkit -> Build Config -> Compilation).
        /// </summary>
        DebugScripting_UseConfig = 2048,

        /// <summary>
        /// Default options.
        /// </summary>
        Default = ForceRebuild_UseConfig | DebugScripting_UseConfig,
    }

    /// <summary>
    /// Main API for building DLC content from script.
    /// </summary>
    [InitializeOnLoad]
    public static class DLCBuildPipeline
    {
        // Private
        private static readonly string profileDBSearch = "t:" + typeof(DLCProfile).FullName;

        // Constructor
        static DLCBuildPipeline()
        {
            // Check for first run
            if(EditorPrefs.GetInt("dlctoolkit.firstrun", 0) == 0)
            {
                EditorPrefs.SetInt("dlctoolkit.firstrun", 1);
                UpdateDLCConfigFromProject();
            }
        }

        // Methods
        /// <summary>
        /// Try to get the <see cref="DLCProfile"/> from the project with the specified name and optional version.
        /// </summary>
        /// <param name="name">The name of the DLC to find</param>
        /// <param name="version">The optional version of the DLC to find if an exact match is required</param>
        /// <returns>A matching profile if found or null</returns>
        public static DLCProfile GetDLCProfile(string name, Version version = null)
        {
            // Find all dlc profiles in project
            string[] guids = AssetDatabase.FindAssets(profileDBSearch);

            // Find and load all
            foreach (string guid in guids)
            {
                // Get asset path
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Load the asset
                DLCProfile profile = AssetDatabase.LoadAssetAtPath<DLCProfile>(path);

                // Check for loaded
                if (profile == null)
                {
                    Debug.LogWarning("Could not load DLCProfile: " + path);
                    continue;
                }

                // Check name and version
                if(profile.DLCName == name)
                {
                    // Check for matching version
                    if(version == null || profile.DLCVersion.CompareTo(version) == 0)
                    {
                        return profile;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the <see cref="DLCProfile"/> from the project with the specified unique key.
        /// This will check all platform profiles in the DLC for a matching unique key.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>A matching profile if found or null</returns>
        public static DLCProfile GetDLCProfile(string uniqueKey)
        {
            // Find all dlc profiles in project
            string[] guids = AssetDatabase.FindAssets(profileDBSearch);

            // Find and load all
            foreach (string guid in guids)
            {
                // Get asset path
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Load the asset
                DLCProfile profile = AssetDatabase.LoadAssetAtPath<DLCProfile>(path);

                // Check for loaded
                if (profile == null)
                {
                    Debug.LogWarning("Could not load DLCProfile: " + path);
                    continue;
                }

                // Check all platforms
                foreach(DLCPlatformProfile platform in profile.Platforms)
                {
                    if(platform.DlcUniqueKey == uniqueKey)
                    {
                        return profile;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get an array of all <see cref="DLCProfile"/> in the project which is enabled for the specified platforms and is enabled unless <paramref name="includeDisabledDlc"/> is disabled.
        /// Platforms can be null if you want to get all DLC for all platforms.
        /// </summary>
        /// <param name="platforms">An optional array of platforms that should be found or null for all platforms</param>
        /// <param name="includeDisabledDlc">Should the result include DLC content that is not enabled for build</param>
        /// <returns>An array of DLC profiles matching the search criteria or an empty array if no results are found</returns>
        public static DLCProfile[] GetAllDLCProfiles(BuildTarget[] platforms = null, bool includeDisabledDlc = false)
        {
            return GetAllDLCProfiles(false, platforms, includeDisabledDlc);
        }

        private static DLCProfile[] GetAllDLCProfiles(bool logExcludedProfiles, BuildTarget[] platforms = null, bool includeDisabledDlc = false)
        {
            // Find all dlc profiles in project
            string[] guids = AssetDatabase.FindAssets(profileDBSearch);

            // Check for any
            if(guids.Length == 0)
                return Array.Empty<DLCProfile>();

            // Create result array
            List<DLCProfile> profiles = new List<DLCProfile>();

            // Find and load all
            foreach(string guid in guids)
            {
                // Get asset path
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Load the asset
                DLCProfile profile = AssetDatabase.LoadAssetAtPath<DLCProfile>(path);

                // Check for loaded
                if(profile == null)
                {
                    Debug.LogWarning("Could not load DLCProfile: " + path);
                    continue;
                }

                // Check if profile has a platform that we are interested in
                if ((platforms == null || profile.IsAnyPlatformEnabled(platforms) == true) &&
                    (includeDisabledDlc == true || profile.EnabledForBuild == true))
                {
                    // Add profile
                    profiles.Add(profile);
                }
                else
                {
                    if (logExcludedProfiles == true)
                        Debug.LogWarning("DLC will not be built because it is disabled: " + profile.DLCProfileName);
                }
            }

            // Get all profiles
            return profiles.ToArray();
        }

        public static string[] GetAllAssetGUIDsForDLCProfile(DLCProfile profile, bool includeIcons = false, bool includeScripts = false)
        {
            // Check for null
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            // Get profile path
            string profilePath = AssetDatabase.GetAssetPath(profile);

            // Get profile guid
            string profileGuid;
            long localID;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out profileGuid, out localID) == false)
                throw new Exception("Could not determine guid for profile asset: " + profile + ". Please ensure the profile asset has been saved first");

            // Search in content folder
            string[] guids = AssetDatabase.FindAssets("", new string[] { profile.DLCContentPath });

            // Create results
            List<string> result = new List<string>(guids.Length);

            // Process all assets
            foreach(string guid in guids)
            {
                // Check for profile asset
                if (guid == profileGuid)
                    continue;

                // Get asset path
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Check for folder
                if (Path.HasExtension(assetPath) == false)
                    continue;

                // Check for excluded due to special folders
                if (DLCBuildContext.IsSpecialFolderExcluded(Array.Empty<string>(), assetPath) == true)
                    continue;

                // Check for icon content
                if (includeIcons == false && DLCBuildContext.IsIconAssetExcluded(profile, assetPath) == true)
                    continue;

                // Check for scripts
                if (includeIcons == false && Path.GetExtension(assetPath) == DLCBuildAsset.scriptExtension)
                    continue;

                // Add the guid
                result.Add(guid);
            }

            return result.ToArray();
        }

        public static string[] GetAllAssetPathsForDLCProfile(DLCProfile profile, bool includeIcons = false, bool includeScripts = false)
        {
            // Check for null
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            // Get profile path
            string profilePath = AssetDatabase.GetAssetPath(profile);

            // Get profile guid
            string profileGuid;
            long localID;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out profileGuid, out localID) == false)
                throw new Exception("Could not determine guid for profile asset: " + profile + ". Please ensure the profile asset has been saved first");

            // Search in content folder
            string[] guids = AssetDatabase.FindAssets("", new string[] { profile.DLCContentPath });

            // Create results
            List<string> result = new List<string>(guids.Length);

            // Process all assets
            foreach (string guid in guids)
            {
                // Check for profile asset
                if (guid == profileGuid)
                    continue;

                // Get asset path
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Check for folder
                if (Path.HasExtension(assetPath) == false)
                    continue;

                // Check for excluded due to special folders
                if (DLCBuildContext.IsSpecialFolderExcluded(Array.Empty<string>(), assetPath) == true)
                    continue;

                // Check for icon content
                if (includeIcons == false && DLCBuildContext.IsIconAssetExcluded(profile, assetPath) == true)
                    continue;

                // Check for scripts
                if (includeIcons == false && Path.GetExtension(assetPath) == DLCBuildAsset.scriptExtension)
                    continue;

                // Add the guid
                result.Add(assetPath);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Build all <see cref="DLCProfile"/> in the project with the specified options.
        /// </summary>
        /// <param name="outputFolder">The optional output folder where the DLC content will be built. Use null for the default output folder of 'DLC Content/'</param>
        /// <param name="platforms">An optional array of platforms to build for or null if all platforms should be built</param>
        /// <param name="buildOptions">Build option flags to define various build settings</param>
        public static DLCBuildResult BuildAllDLCContent(string outputFolder = null, BuildTarget[] platforms = null, DLCBuildOptions buildOptions = DLCBuildOptions.Default)
        {
            // Get all profiles
            DLCProfile[] profiles = GetAllDLCProfiles(true, platforms, (buildOptions & DLCBuildOptions.IncludeDisabledDLC) != 0);

            // Build profiles
            return BuildDLCContentProfiles(profiles, outputFolder, platforms, buildOptions);
        }

        /// <summary>
        /// Build all DLC content in the project for only the specified array of <see cref="DLCProfile"/>.
        /// Only the profiles provided will be built and other profiles in the project will be ignored.
        /// </summary>
        /// <param name="profiles">An array of DLC profiles to build in this batch</param>
        /// <param name="outputFolder">The optional output folder where the DLC content will be built. Use null for the default output folder of 'DLC Content/'</param>
        /// <param name="platforms">An optional array of platforms to build for or null if all platforms should be built</param>
        /// <param name="buildOptions">Build option flags to define various build settings</param>
        /// <exception cref="ArgumentNullException">profiles is null</exception>
        public static DLCBuildResult BuildDLCContent(DLCProfile[] profiles, string outputFolder = null, BuildTarget[] platforms = null, DLCBuildOptions buildOptions = DLCBuildOptions.Default)
        {
            if(profiles == null)
                throw new ArgumentNullException(nameof(profiles));

            // Build profiles
            return BuildDLCContentProfiles(profiles, outputFolder, platforms, buildOptions);
        }

        /// <summary>
        /// Build the specified DLC content in the project for the provided <see cref="DLCProfile"/>.
        /// Only the profile provided will be build and all other profiles in the project will be ignored.
        /// </summary>
        /// <param name="profile">The DLC profile to build</param>
        /// <param name="outputFolder">The optional output folder where the DLC content will be built. Use null for the default output folder of 'DLC Content/'</param>
        /// <param name="platforms">An optional array of platforms to build for or null if all platforms should be built</param>
        /// <param name="buildOptions">Build option flags to define various build settings</param>
        /// <exception cref="ArgumentNullException">profile is null</exception>
        public static DLCBuildResult BuildDLCContent(DLCProfile profile, string outputFolder = null, BuildTarget[] platforms = null, DLCBuildOptions buildOptions = DLCBuildOptions.Default)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            // Build profiles
            return BuildDLCContentProfiles(new DLCProfile[] { profile }, outputFolder, platforms, buildOptions);
        }

        /// <summary>
        /// Build all <see cref="DLCProfile"/> in the project for the target platform that are marked to be shipped with the game.
        /// </summary>
        /// <param name="platformTarget">The platform that the DLC should be built for</param>
        /// <param name="install">Should the built DLC content be installed in the shipped game output folder</param>
        /// <param name="buildOutputFolder">The output folder where the shipped game will be built</param>
        /// <param name="outputFolder">The optional output folder where the DLC content will be built. Use null for the default output folder of 'DLC Content/'</param>
        /// <param name="buildOptions">Build option flags to define various build settings</param>
        public static DLCBuildResult BuildAllDLCShipWithGameContent(BuildTarget platformTarget, bool install = true, string buildOutputFolder = null, string outputFolder = null, DLCBuildOptions buildOptions = DLCBuildOptions.Default)
        {
            // Get all DLC content for the build platform
            DLCProfile[] profiles = GetAllDLCProfiles(new BuildTarget[] { platformTarget });

            // Build the content
            return BuildDLCShipWithGameContent(profiles, platformTarget, install, buildOutputFolder, outputFolder, buildOptions);
        }

        /// <summary>
        /// Build all <see cref="DLCProfile"/> provided for the target platform that are marked to be shipped with the game.
        /// </summary>
        /// <param name="profiles">A collection of DLC profiles that should be built</param>
        /// <param name="platformTarget">The platform that the DLC should be built for</param>
        /// <param name="install">Should the built DLC content be installed in the shipped game output folder</param>
        /// <param name="buildOutputFolder">The output folder where the shipped game will be built</param>
        /// <param name="outputFolder">The optional output folder where the DLC content will be built. Use null for the default output folder of 'DLC Content/'</param>
        /// <param name="buildOptions">Build option flags to define various build settings</param>
        public static DLCBuildResult BuildDLCShipWithGameContent(DLCProfile[] profiles, BuildTarget platformTarget, bool install = true, string buildOutputFolder = null, string outputFolder = null, DLCBuildOptions buildOptions = DLCBuildOptions.Default)
        {
            // Check for invalid
            if (install == true && string.IsNullOrEmpty(buildOutputFolder) == true)
                throw new ArgumentException("Build output folder cannot be empty if `install` parameter is set");

            // Check for any
            if (profiles.Length == 0)
                return DLCBuildResult.Empty();

            // List all profiles that should be shipped with the build
            List<DLCProfile> shipProfilesStreaming = new List<DLCProfile>();
            List<DLCProfile> shipProfilesBuild = new List<DLCProfile>();

            // Add all ship profiles
            foreach (DLCProfile profile in profiles)
            {
                if (profile.IsPlatformEnabled(platformTarget) == true)
                {
                    // Get the platform
                    DLCPlatformProfile platform = profile.GetPlatform(platformTarget);

                    // Check for supported and ship with build enabled
                    if (platform != null && platform.ShipWithGame == true)
                    {
                        // Check for streaming
                        if (platform.ShipWithGameDirectory == ShipWithGameDirectory.StreamingAssets ||
                            (platformTarget == BuildTarget.Android || platformTarget == BuildTarget.WebGL))
                        {
                            // Report warning
                            if (platform.ShipWithGameDirectory != ShipWithGameDirectory.StreamingAssets)
                                Debug.LogWarning("DLC content is marked as `ShipWithGame` using `BuildDirectory`, but it is not supported on this platform! `StreamingAssets` will be used instead: " + profile.DLCName);

                            shipProfilesStreaming.Add(profile);
                        }
                        else
                        {
                            shipProfilesBuild.Add(profile);
                        }
                    }
                }
            }

            // Check for any profiles
            if (shipProfilesStreaming.Count == 0 && shipProfilesBuild.Count == 0)
                return DLCBuildResult.Empty();

            // Build all profiles
            List<DLCProfile> allProfiles = new List<DLCProfile>();
            allProfiles.AddRange(shipProfilesStreaming);
            allProfiles.AddRange(shipProfilesBuild);

            // Build all DLC marked as ship with game
            DLCBuildResult result = BuildDLCContent(allProfiles.ToArray(), outputFolder, new BuildTarget[] { platformTarget }, buildOptions);

            // Check for install
            if(install == true)
            {
                bool hasStreamingBuilds = false;

                // Copy all successful
                foreach (DLCBuildTask task in result.GetSuccessfulBuildTasks())
                {
                    // Check for streaming
                    if (shipProfilesStreaming.Contains(task.Profile) == true)
                    {
                        // Copy to streaming
                        string copyPath = Path.Combine("Assets/StreamingAssets", task.PlatformProfile.ShipWithGamePath);

                        // Create directory
                        if (Directory.Exists(copyPath) == false)
                            Directory.CreateDirectory(copyPath);

                        // Copy to output
                        File.Copy(task.OutputPath, Path.Combine(copyPath, Path.GetFileName(task.OutputPath)), true);
                        hasStreamingBuilds = true;
                    }
                    // Check for build
                    else
                    {
                        // Copy to build
                        string copyPath = Path.Combine(buildOutputFolder, task.PlatformProfile.ShipWithGamePath);

                        // Create directory
                        if (Directory.Exists(copyPath) == false)
                            Directory.CreateDirectory(copyPath);

                        // Copy to output
                        File.Copy(task.OutputPath, Path.Combine(copyPath, Path.GetFileName(task.OutputPath)), true);
                    }
                }

                // Refresh for streaming assets
                if (hasStreamingBuilds == true)
                    AssetDatabase.Refresh();
            }
            
            // Get the build result
            return result;
        }

        /// <summary>
        /// Build the provided <see cref="DLCProfile"/> for the target platform that are marked to be shipped with the game.
        /// </summary>
        /// <param name="profile">The DLC profile to build</param>
        /// <param name="platformTarget">The platform that the DLC should be built for</param>
        /// <param name="install">Should the built DLC content be installed in the shipped game output folder</param>
        /// <param name="buildOutputFolder">The output folder where the shipped game will be built</param>
        /// <param name="outputFolder">The optional output folder where the DLC content will be built. Use null for the default output folder of 'DLC Content/'</param>
        /// <param name="buildOptions">Build option flags to define various build settings</param>
        public static DLCBuildResult BuildDLCShipWithGameContent(DLCProfile profile, BuildTarget platformTarget, bool install = true, string buildOutputFolder = null, string outputFolder = null, DLCBuildOptions buildOptions = DLCBuildOptions.Default)
        {
            return BuildDLCShipWithGameContent(new DLCProfile[] { profile }, platformTarget, install, buildOutputFolder, outputFolder, buildOptions);
        }

        internal static DLCConfig UpdateDLCConfigFromProject()
        {
            // Get or create asset
            DLCConfig config = DLC.Config;

            // Set build log level
            config.SetBuildLogLevel();

            // Write product hash
            using (HashAlgorithm hash = SHA256.Create())
            {
                // Get product string
                string productString = PlayerSettings.productGUID.ToString() + "-" + Application.productName;

                // Store hash
                config.UpdateProductHash(hash.ComputeHash(Encoding.UTF8.GetBytes(productString)));
            }

            // Write version hash
            using(HashAlgorithm hash = SHA256.Create())
            {
                // Get version string
                string versionString = Application.version;

                // Store hash
                config.UpdateVersionHash(hash.ComputeHash(Encoding.UTF8.GetBytes(versionString)));
            }


            // Update available content
            // Get all profiles
            DLCProfile[] profiles = GetAllDLCProfiles();

            // Build all platform keys
            Dictionary<RuntimePlatform, string> platformUniqueKeys = new Dictionary<RuntimePlatform, string>();

            // Check all profiles
            foreach(DLCProfile profile in profiles)
            {
                // Check for enabled
                if (profile.EnabledForBuild == false)
                    continue;

                // Check all platforms
                foreach(DLCPlatformProfile platform in profile.Platforms)
                {
                    // Check for enabled
                    if (platform.Enabled == false)
                        continue;

                    // Register content
                    try
                    {
                        platformUniqueKeys[platform.RuntimePlatform] = platform.DlcUniqueKey;
                    }
                    // Platform is not supported
                    catch (NotSupportedException) { }
                }
            }

            // Store platforms
            config.UpdatePlatformContent(platformUniqueKeys);
            return config;
        }

        private static DLCBuildResult BuildDLCContentProfiles(DLCProfile[] profiles, string outputFolder, BuildTarget[] platforms, DLCBuildOptions buildOptions)
        {
            // Create DLC build
            DLCBuildRequest request = new DLCBuildRequest(profiles);

            // Update config asset
            DLCConfig config = UpdateDLCConfigFromProject();

            // Check for clear console
            if(config.clearConsoleOnBuild == true)
            {
                try
                {
                    typeof(EditorApplication).Assembly
                        .GetType("UnityEditor.LogEntries")
                        .GetMethod("Clear")
                        .Invoke(null, null);
                }
                catch
                {
                    UnityEngine.Debug.LogWarning("Unable to clear console - API may have changed!");
                }
            }


            // Raise start build events
            DLCBuildEventHooks.SafeInvokeBuildEventHookImplementations<DLCPreBuildAttribute, DLCBuildEvent>((DLCBuildEvent e)
                => e.OnBuildEvent());

            // Update scripting debug
            if ((buildOptions & DLCBuildOptions.DebugScripting_UseConfig) != 0 && config.enableScripting == true && config.ScriptingDebug == true)
                buildOptions |= DLCBuildOptions.DebugScripting;

            // Update force rebuild
            if ((buildOptions & DLCBuildOptions.ForceRebuild_UseConfig) != 0 && config.ForceRebuild == true)
                buildOptions |= DLCBuildOptions.ForceRebuild;

            // Check for scripting allowed
            if (config.enableScripting == false)
                buildOptions |= DLCBuildOptions.DisableScripting;

            // Log start build
            string profileNames = string.Join(", ", profiles.Select(p => p.DLCName));
            string platformNames = platforms != null ? string.Join(", ", platforms.Select(p => DLCPlatformProfile.GetFriendlyPlatformName(p))) : "All";

            // Write info
            Debug.Log(string.Format("Building DLC profiles ({0}) for platforms ({1})...", profileNames, platformNames));

            // Build the content
            DLCBuildResult result = request.BuildDLCForPlatforms(outputFolder, platforms, buildOptions);


            // Raise end build events
            DLCBuildEventHooks.SafeInvokeBuildEventHookImplementations<DLCPostBuildAttribute, DLCBuildEvent>((DLCBuildEvent e)
                => e.OnBuildEvent());

            return result;
        }
    }
}
