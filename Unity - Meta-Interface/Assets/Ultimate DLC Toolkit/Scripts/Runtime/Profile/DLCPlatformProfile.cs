using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.Profile
{
    public enum PlatformGroup
    {
        None = 0,
        Desktop,
        Console,
        Mobile,
    }

    public enum ShipWithGameDirectory
    {
        StreamingAssets,
        BuildDirectory,
    }

    /// <summary>
    /// Represents a DLC build profile for a specific build target platform.
    /// </summary>
    [Serializable]
    public class DLCPlatformProfile
    {
        // Internal
        protected internal event Action OnProfileModified;

        // Public
        /// <summary>
        /// An array of supported platform names in an easily readable format.
        /// </summary>
        [NonSerialized]
        public static readonly string[] friendlyPlatformNames = ((BuildTarget[])Enum.GetValues(typeof(BuildTarget)))
            .Select(t => GetFriendlyPlatformName(t))
            .Where(n => n != "UnsupportedPlatform")
            .ToArray();

        public const string friendlyDesktopPlatformName = "Desktop";
        public const string friendlyMobilePlatformName = "Mobile";
        public const string friendlyConsolePlatformName = "Console";

        // Internal
        [SerializeField]
        internal string[] platformScriptingDefines = new string[0];

        [SerializeField]
        internal long lastPlatformBuildTime = 0;
        [SerializeField]
        internal bool lastBuildSuccess = false;

        // Private
        [SerializeField]
        private string platformName = "";        
        [SerializeField]
        private BuildTarget platform = 0;
        [SerializeField]
        private bool enabled = true;

        [SerializeField]
        private string platformSpecificFolder = "";
        [SerializeField]
        private string dlcUniqueKey = "";
        [SerializeField]
        private string dlcExtension = ".dlc";
        [SerializeField]
        private bool useCompression = true;
        [SerializeField]
        private bool strictBuild = false;

        [Header("DRM")]
        private string dlcIAPName = "";

        [Header("Options")]
        [SerializeField]
        private bool preloadSharedAssets = true;
        [SerializeField]
        private bool preloadSceneAssets = true;

        [Header("Distribution")]
        [SerializeField]
        private bool shipWithGame = false;
        [SerializeField]
        private ShipWithGameDirectory shipWithGameDirectory = ShipWithGameDirectory.BuildDirectory;
        [SerializeField]
        private string shipWithGamePath = "DLC";

        // Properties
        /// <summary>
        /// Get the time stamp for the last build for this platform profile.
        /// </summary>
        public DateTime LastBuildTime
        {
            get { return DateTime.FromFileTime(lastPlatformBuildTime); }
        }

        /// <summary>
        /// Check if this platform has been built yet. 
        /// This will be true even if a build was attempted but failed due to errors.
        /// </summary>
        public bool HasLastBuildTime
        {
            get { return lastPlatformBuildTime != 0; }
        }

        /// <summary>
        /// Get the result of the last build for this platform.
        /// True if the last build was successful or false if not.
        /// </summary>
        public bool LastBuildSuccess
        {
            get { return lastBuildSuccess; }
        }

        /// <summary>
        /// Get the easily readable platform name for this platform profile.
        /// </summary>
        public string PlatformFriendlyName
        {
            get { return GetFriendlyPlatformName(platform); }
        }

        /// <summary>
        /// Get the name of this platform.
        /// </summary>
        public string PlatformName
        {
            get { return platformName; }
        }

        /// <summary>
        /// Get the build target for this platform.
        /// </summary>
        public BuildTarget Platform
        {
            get { return platform; }
        }

        /// <summary>
        /// Get the runtime target for this platform.
        /// </summary>
        public RuntimePlatform RuntimePlatform
        {
            get { return GetRuntimePlatform(platform); }
        }

        /// <summary>
        /// Return a value indicating whether this platform is targeting a desktop build target.
        /// Note that this value will be false if targeting an unsupported desktop platform.
        /// </summary>
        public bool IsDesktopPlatform
        {
            get { return GetPlatformGroup(platform) == PlatformGroup.Desktop; }
        }

        /// <summary>
        /// Return a value indicating whether this platform is targeting a console build target.
        /// Note that this value will be false if targeting an unsupported console platform.
        /// </summary>
        public bool IsConsolePlatform
        {
            get { return GetPlatformGroup(platform) == PlatformGroup.Console; }
        }

        /// <summary>
        /// Return a value indicating whether this platform is targeting a mobile build target.
        /// </summary>
        public bool IsMobilePlatform
        {
            get { return GetPlatformGroup(platform) == PlatformGroup.Mobile; }
        }

        /// <summary>
        /// Is this platform enabled for build.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set 
            { 
                enabled = value;
                OnProfileModified?.Invoke();
            }
        }

        public string PlatformSpecificFolder
        {
            get { return platformSpecificFolder; }
            set
            {
                platformSpecificFolder = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// The unique key for this platform DLC.
        /// </summary>
        public string DlcUniqueKey
        {
            get { return dlcUniqueKey; }
            set 
            { 
                dlcUniqueKey = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// The file extension for this platform DLC.
        /// </summary>
        public string DLCExtension
        {
            get { return dlcExtension; }
            set 
            { 
                dlcExtension = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// Should the DLC content be built with compression for this platform.
        /// </summary>
        public bool UseCompression
        {
            get { return useCompression; }
            set 
            { 
                useCompression = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// Should the DLC content be built in strict mode for this platform.
        /// </summary>
        public bool StrictBuild
        {
            get { return strictBuild; }
            set 
            { 
                strictBuild = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// The name of an InApp Purchase that is linked to this DLC, and verifies ownership of the DLC content.
        /// Requires Unity.Purchasing or other IAP integration.
        /// </summary>
        public string DLCIAPName
        {
            get { return dlcIAPName; }
            set
            {
                dlcIAPName = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// Return a value indicating whether this DLC is linked to a specific InApp Purchase.
        /// </summary>
        public bool IsLinkedToIAP
        {
            get { return string.IsNullOrEmpty(dlcIAPName); }
        }

        /// <summary>
        /// Should the DLC content preload shared assets at load time for this platform.
        /// </summary>
        public bool PreloadSharedAssets
        {
            get { return preloadSharedAssets; }
            set 
            { 
                preloadSharedAssets = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// Should the DLC content preload scene assets at load time for this platform.
        /// </summary>
        public bool PreloadSceneAssets
        {
            get { return preloadSceneAssets; }
            set 
            { 
                preloadSceneAssets = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// Should the DLC content be included in the built game.
        /// Note that it is possible to include DLC that is not usable until the user has purchased via DRM.
        /// </summary>
        public bool ShipWithGame
        {
            get { return shipWithGame; }
            set 
            {
                shipWithGame = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// The directory relative where the DLC content should be placed.
        /// </summary>
        public ShipWithGameDirectory ShipWithGameDirectory
        {
            get { return shipWithGameDirectory; }
            set 
            { 
                shipWithGameDirectory = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// The path where the DLC content should be placed.
        /// </summary>
        public string ShipWithGamePath
        {
            get { return shipWithGamePath; }
            set 
            { 
                shipWithGamePath = value;
                OnProfileModified?.Invoke();
            }
        }

        /// <summary>
        /// Get an array of define symbols specific to this platform when compiling scripts.
        /// </summary>
        public string[] PlatformDefines
        {
            get { return platformScriptingDefines; }
        }

        // Constructor
        internal DLCPlatformProfile(BuildTarget platform, params string[] platformDefines)
        {
            this.platformName = platform.ToString();
            this.platformSpecificFolder = GetFriendlyPlatformName(platform);
            this.platform = platform;
            this.enabled = IsDLCBuildTargetAvailable(platform);
            this.platformScriptingDefines = platformDefines;

            // Update ship mode
            this.shipWithGameDirectory = (platform == BuildTarget.StandaloneWindows
                || platform == BuildTarget.StandaloneWindows64
                || platform == BuildTarget.StandaloneLinux64
                || platform == BuildTarget.StandaloneOSX)
                ? ShipWithGameDirectory.BuildDirectory
                : ShipWithGameDirectory.StreamingAssets;

            this.shipWithGamePath = "DLC";
        }

        // Methods
        public IList<string> GetPlatformExcludeDirectories()
        {
            // Get platform exclude folders
            List<string> platformExcludeDirectories = new List<string>(friendlyPlatformNames);

            // Get the platform group
            PlatformGroup platformGroup = GetPlatformGroup(platform);

            // Check for group special folders
            switch (platformGroup)
            {
                case PlatformGroup.Desktop:
                    {
                        // Exclude console and mobile
                        platformExcludeDirectories.Add(friendlyConsolePlatformName);
                        platformExcludeDirectories.Add(friendlyMobilePlatformName);
                        break;
                    }
                case PlatformGroup.Console:
                    {
                        // Exclude desktop and mobile
                        platformExcludeDirectories.Add(friendlyDesktopPlatformName);
                        platformExcludeDirectories.Add(friendlyMobilePlatformName);
                        break;
                    }
                case PlatformGroup.Mobile:
                    {
                        // Exclude desktop and console
                        platformExcludeDirectories.Add(friendlyDesktopPlatformName);
                        platformExcludeDirectories.Add(friendlyConsolePlatformName);
                        break;
                    }
            }

            // Get the friendly name
            string platformFriendlyName = PlatformFriendlyName;

            // Remove restricted platform name - this platform folder is allowed during this phase of the build
            while (platformExcludeDirectories.Contains(platformFriendlyName) == true)
                platformExcludeDirectories.Remove(platformFriendlyName);

            return platformExcludeDirectories;
        }

        /// <summary>
        /// Get the platform friendly name from the specified build target.
        /// </summary>
        /// <param name="platform">The build target platform</param>
        /// <returns>The friendly string name for the build target</returns>
        public static string GetFriendlyPlatformName(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return "Windows";
                case BuildTarget.StandaloneLinux64: return "Linux";
                case BuildTarget.StandaloneOSX: return "OSX";
                case BuildTarget.Android: return "Android";
                case BuildTarget.iOS: return "IOS";
                case BuildTarget.WebGL: return "WebGL";
                case BuildTarget.PS4: return "PS4";
                case BuildTarget.PS5: return "PS5";
                case BuildTarget.XboxOne: return "XBoxOne";
            }
            return "UnsupportedPlatform";
        }

        /// <summary>
        /// Get the runtime platform from the specified build target.
        /// </summary>
        /// <param name="platform">The build target platform</param>
        /// <returns>The runtime platform for the build target</returns>
        /// <exception cref="NotSupportedException">The build target is not supported</exception>
        public static RuntimePlatform GetRuntimePlatform(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return RuntimePlatform.WindowsPlayer;
                case BuildTarget.StandaloneOSX: return RuntimePlatform.OSXPlayer;
                case BuildTarget.EmbeddedLinux:
                case BuildTarget.StandaloneLinux64: return RuntimePlatform.LinuxPlayer;
                case BuildTarget.WebGL: return RuntimePlatform.WebGLPlayer;
                case BuildTarget.iOS: return RuntimePlatform.IPhonePlayer;
                case BuildTarget.Android: return RuntimePlatform.Android;
                case BuildTarget.PS4: return RuntimePlatform.PS4;
                case BuildTarget.PS5: return RuntimePlatform.PS5;
                case BuildTarget.XboxOne: return RuntimePlatform.XboxOne;
            }
            throw new NotSupportedException("Platform is not supported");
        }        

        /// <summary>
        /// Check if build support for the target platform is installed.
        /// </summary>
        /// <param name="buildTarget">The build target to check</param>
        /// <returns>True if build support is available or false if not</returns>
        public static bool IsDLCBuildTargetAvailable(BuildTarget buildTarget)
        {
            try
            {
                // Get string from build target
                string buildTargetString = (string)Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll")
                    .GetMethod("GetTargetStringFromBuildTarget", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { buildTarget });

                // Check build availability
                return (bool)Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll")
                    .GetMethod("IsPlatformSupportLoaded", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { buildTargetString });
            }
            catch
            {
                Debug.LogWarning("Unable to determine if build support is available - API may have changed!");
            }
            // Could not determine availability - We must assume that the platform is available and the build will fail when generating asset bundles if it is not
            return true;
        }

        public static PlatformGroup GetPlatformGroup(BuildTarget buildTarget)
        {
            switch(buildTarget)
            {
                // Check for desktop
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    return PlatformGroup.Desktop;

                // Check for console
                case BuildTarget.PS4:
                case BuildTarget.PS5:
                case BuildTarget.XboxOne:
                    return PlatformGroup.Console;

                // Check for mobile
                case BuildTarget.iOS:
                case BuildTarget.Android:
                    return PlatformGroup.Mobile;
            }
            return PlatformGroup.None;
        }
    }
}
