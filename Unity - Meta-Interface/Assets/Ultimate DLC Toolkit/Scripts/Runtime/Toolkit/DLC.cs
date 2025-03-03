using DLCToolkit.Assets;
using DLCToolkit.DRM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[assembly: InternalsVisibleTo("DLCToolkit.BuildTools")]
[assembly: InternalsVisibleTo("DLCToolkit.EditorTools")]

namespace DLCToolkit
{
    internal enum DLCLogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    /// <summary>
    /// Main API for interacting with and loading DLC content from an external source.
    /// Also provides API's for retrieving DLC from various DRM (Digital Rights Management) services such as Steamworks, Google Play and more.
    /// DRM is used to ensure that the current user has access to or owns the requested DLC content if it is paid for or licensed in any way by the providing service.
    /// </summary>
    public static class DLC
    {
        // Type
        private sealed class DLCAsyncCommon : MonoBehaviour, IDLCAsyncProvider
        {
            // Methods
            public Coroutine RunAsync(IEnumerator routine)
            {
                return StartCoroutine(routine);
            }
        }

        // Events
        public static readonly UnityEvent<DLCContent> OnContentWillUnload = new UnityEvent<DLCContent>();
        public static readonly UnityEvent<DLCContent> OnContentUnloaded = new UnityEvent<DLCContent>();

        // Internal
        internal static readonly List<DLCSharedAsset> allSharedAssets = new List<DLCSharedAsset>();
        internal static readonly List<DLCSceneAsset> allSceneAssets = new List<DLCSceneAsset>();

        // Private
        private static DLCContent tempContent = null;
        private static DLCAsyncCommon asyncCommon = null;
        private static DLCConfig config = null;

        private static IDRMServiceProvider drmServiceProvider = new DefaultDRMServiceProvider();
        private static IDRMProvider drmProvider = null;
        private static DLCAsync<DLCManifest> cachedManifestRequest = null;
        private static bool isTestMode = false;

        private static readonly DLCAssetCollection<DLCSharedAsset> allSharedAssetsCollection = new DLCAssetCollection<DLCSharedAsset>(allSharedAssets);
        private static readonly DLCAssetCollection<DLCSceneAsset> allSceneAssetsCollection = new DLCAssetCollection<DLCSceneAsset>(allSceneAssets);

        // Public
        /// <summary>
        /// Get the current version of DLC Toolkit.
        /// </summary>
        public static readonly Version ToolkitVersion = new Version(1, 2, 0);

        // Properties
        private static DLCContent TempContent
        {
            get
            {
                if(tempContent == null)
                {
                    // Create new content host
                    GameObject temp = new GameObject("DLC Temp");
                    temp.hideFlags = HideFlags.HideAndDontSave;
                    tempContent = temp.AddComponent<DLCContent>();

                    // Keep alive
                    if(Application.isPlaying == true)
                        GameObject.DontDestroyOnLoad(temp);
                }
                return tempContent;
            }
        }

        internal static IDLCAsyncProvider AsyncCommon
        {
            get
            {
                if(asyncCommon == null)
                {
                    // Create new container
                    GameObject async = new GameObject("DLC Async Common");
                    async.hideFlags = HideFlags.HideAndDontSave;
                    asyncCommon = async.AddComponent<DLCAsyncCommon>();

                    // Keep alive
                    GameObject.DontDestroyOnLoad(async);
                }
                return asyncCommon;
            }
        }

        internal static DLCConfig Config
        {
            get
            {
                if(config == null)
                {
                    // Try to load
                    config = Resources.Load<DLCConfig>(DLCConfig.assetName);

                    // Check for failed to load
                    if(config == null)
                    {
                        Debug.LogWarning("Failed to load DLC config, creating defaults!");
                        config = ScriptableObject.CreateInstance<DLCConfig>();

#if UNITY_EDITOR
                        if(Application.isPlaying == false)
                        {
                            // Check if there are any profile assets which means they must be in the project but not in the resources folder - log a warning to help the user
                            // Try to find
                            string[] guids = AssetDatabase.FindAssets("t:" + typeof(DLCConfig).FullName);

                            // Check for multiple
                            foreach(string guid in guids)
                            {
                                // Get asset path
                                string assetPathOutsideOfResources = AssetDatabase.GUIDToAssetPath(guid);

                                // Report a warning
                                Debug.LogWarning("DLC Config asset was not found inside a `Resources` folder or did not have the required name `" + DLCConfig.assetName + ".asset`. The config will not be loadable at runtime: " + assetPathOutsideOfResources);
                            }


                            string createLocation = "Assets/Resources";

                            // Create the directory if it does not exist
                            if (Directory.Exists(createLocation) == false)
                            {
                                Directory.CreateDirectory(createLocation);
                                AssetDatabase.ImportAsset(createLocation);
                            }

                            // Create instance
                            DLCConfig config = ScriptableObject.CreateInstance<DLCConfig>();

                            // Create asset
                            AssetDatabase.CreateAsset(config, createLocation + "/" + DLCConfig.assetName + ".asset");
                        }
#endif
                    }
                }
                return config;
            }
        }
        
        /// <summary>
        /// Check if scripting is supported on this platform.
        /// Scripting is supported only on desktop platforms using the Mono backend.
        /// </summary>
        public static bool IsScriptingSupported
        {
            get
            {
#if (UNITY_EDITOR || UNITY_STANDALONE) && !ENABLE_IL2CPP
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Get the service that can provide a DRM container for the current build configuration.
        /// </summary>
        public static IDRMServiceProvider DRMServiceProvider
        {
            get { return drmServiceProvider; }
        }

        /// <summary>
        /// Get the DRM provider that will handle all DLC ownership and access requests for the current build configuration.
        /// For example: Desktop builds may use Steamworks DLC, whereas Android may use GooglePlay and so on.
        /// Note that you can override the default DRM provider that is used by registering a custom DRM service provider using <see cref="RegisterDRMServiceProvider(IDRMServiceProvider)"/>.
        /// At that point all further DRM related requests will be routed through the DRM manager provided by <see cref="IDRMServiceProvider.GetDRMProvider"/>.
        /// </summary>
        private static IDRMProvider DRMProvider
        {
            get
            {
                // Get and cache the provider
                if (drmProvider == null)
                    drmProvider = drmServiceProvider.GetDRMProvider();

                return drmProvider;
            }
        }

        /// <summary>
        /// An async operation to fetch the default <see cref="DLCManifest"/> that is included in all standalone builds via streaming assets.
        /// The manifest contains information about all enabled DLC known to the game at build time.
        /// </summary>
        public static DLCAsync<DLCManifest> ManifestAsync
        {
            get
            {
                // Check for not initialized
                if (cachedManifestRequest == null)
                    cachedManifestRequest = DLCManifest.FromProjectAsync(AsyncCommon);

                // Get the current operation
                return cachedManifestRequest;
            }
        }

        /// <summary>
        /// Get a value indicating whether Ultimate DLC Toolkit is currently setup in editor test mode.
        /// Editor test mode allows DLC DRM management to be simulated in the editor environment.
        /// This means that you can load any DLC you have created in the editor using the DRM API, as long as it has been built for the equivalent editor built target.
        /// For example: The DLC must be built for StandaloneWindows if running in the Windows Editor, otherwise it will not be available for loading.
        /// This mode is fully automatic and is intended for quick and easy testing of DLC content. 
        /// THIS MODE IS ONLY AVAILABLE IN EDITOR AND SHOULD BE DISABLED VIA THE TOOLS MENU IF YOU WANT TO TEST HOW A STANDALONE BUILD WOULD ACTUALLY FUNCTION.
        /// </summary>
        public static bool IsTestMode
        {
            get { return Application.isEditor == true && isTestMode == true; }
        }

        /// <summary>
        /// Get all DLC contents that are currently available.
        /// This could include contents that are currently loading or loaded.
        /// </summary>
        public static IEnumerable<DLCContent> AllDLCContents
        {
            get 
            { 
                // Check all contents
                foreach(DLCContent content in DLCContent.AllContents)
                {
                    // We are only interested DLS's that have been requested with full load mode which may be loading or are currently loaded.
                    if (content.LoadMode == DLCLoadMode.Full)
                        yield return content;
                }
            }
        }

        /// <summary>
        /// Get all DLC contents that are currently available with a loaded status.
        /// The DLC has been successfully loaded into memory.
        /// </summary>
        public static IEnumerable<DLCContent> LoadedDLCContents
        {
            get
            {
                foreach(DLCContent content in DLCContent.AllContents)
                {
                    if (content.IsLoaded == true)
                        yield return content;
                }
            }
        }

        /// <summary>
        /// Get all DLC contents that are currently available with a loading status.
        /// The DLC is currently in the process of being loaded into memory.
        /// </summary>
        public static IEnumerable<DLCContent> LoadingDLCContents
        {
            get
            {
                foreach (DLCContent content in DLCContent.AllContents)
                {
                    if (content.IsLoading == true)
                        yield return content;
                }
            }
        }

        // Constructor
        static DLC()
        {
            // Only start request in play mode otherwise there will be issues
            if (Application.isPlaying == true)
            {
                // Start requesting the manifest at earliest opportunity
                cachedManifestRequest = DLCManifest.FromProjectAsync(AsyncCommon);
            }
        }

        // Methods        
        #region IsLoaded
        /// <summary>
        /// Check if the DLC with the specified name and optional version is currently loaded.
        /// Note that this method will check the DLC name/version and not the DLC unique key which can be checked by <see cref="IsDLCLoaded(string)"/> instead.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">An optional version if you want to find a specific version of a loaded DLC</param>
        /// <returns>True if the DLC is loaded or false if not</returns>
        public static bool IsDLCLoaded(string name, Version version = null)
        {
            // Check all loaded contents
            foreach(DLCContent content in LoadedDLCContents)
            {
                // Check for matching name
                if(content.NameInfo.Name == name)
                {
                    // Check for version
                    if(version == null || content.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find loaded DLC with version: " + version + " mismatch, nearest version found: " + content.NameInfo.Version);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the DLC with the specified unique key is currently loaded.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>True if the DLC is loaded or false if not</returns>
        public static bool IsDLCLoaded(string uniqueKey)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadedDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.UniqueKey == uniqueKey)
                {
                    // Match
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the DLC with the specified name and optional version is currently being loaded.
        /// Note that this method will check the DLC name/version and not the DLC unique key which can be checked by <see cref="IsDLCLoading(string)"/> instead.
        /// Note that this method is only of use when DLC async loading is used.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">An optional version if you want to find a specific version of a loading DLC</param>
        /// <returns>True if the DLC is loading or false if not</returns>
        public static bool IsDLCLoading(string name, Version version = null)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadingDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.Name == name)
                {
                    // Check for version
                    if (version == null || content.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find loading DLC with version: " + version + " mismatch, nearest version found: " + content.NameInfo.Version);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the DLC with the specified unique key is currently being loaded.
        /// Note that this method is only of use when DLC async loading is used.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>True if the DLC is loading or false if not</returns>
        public static bool IsDLCLoading(string uniqueKey)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadingDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.UniqueKey == uniqueKey)
                {
                    // Match
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region GetLoaded
        /// <summary>
        /// Try to get the DLC loaded from the specified path if it has already started loading or has been loaded.
        /// </summary>
        /// <param name="path">The path of the DLC</param>
        /// <returns>The DLC that is already loading or loaded from the provided path, or null if the DLC was not found</returns>
        public static DLCContent GetDLCFrom(string path)
        {
            // Check for invalid path
            if (string.IsNullOrEmpty(path) == true)
                return null;

            // Check all loaded contents
            foreach (DLCContent content in AllDLCContents)
            {
                // Check for equal path
                if(content.IsLoadPath(path) == true)
                {
                    return content;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the DLC loaded with the specified name and optional version if it has already started loading or has been loaded.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">The optional version of the DLC if an explicit match is required</param>
        /// <returns>The DLC that is already loading or loaded from the provided path, or null if the DLC was not found</returns>
        public static DLCContent GetDLC(string name, Version version = null)
        {
            // Check all loaded contents
            foreach (DLCContent content in AllDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.Name == name)
                {
                    // Check for version
                    if (version == null || content.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return content;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find loaded DLC with version: " + version + " mismatch, nearest version found: " + content.NameInfo.Version);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the DLC with the specified key if it has already started loading or has been loaded.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>The DLC that is already loading or loaded with the provided unique key, or null if the DLC was not found</returns>
        public static DLCContent GetDLC(string uniqueKey)
        {
            // Check all loaded contents
            foreach (DLCContent content in AllDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.UniqueKey == uniqueKey)
                {
                    return content;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the DLC loaded with the specified name and optional version if it has already been loaded.
        /// Note this will only detect successfully loaded DLC's and not DLC's that are currently loading.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">The optional version of the DLC if an explicit match is required</param>
        /// <returns>The DLC that is already loaded from the provided name and optional version, or null if the DLC was not found</returns>
        public static DLCContent GetLoadedDLC(string name, Version version = null)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadedDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.Name == name)
                {
                    // Check for version
                    if (version == null || content.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return content;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find loaded DLC with version: " + version + " mismatch, nearest version found: " + content.NameInfo.Version);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the DLC with the specified key if it has already been loaded.
        /// Note this will only detect successfully loaded DLC's and not DLC's that are currently loading.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>The DLC that is already loaded with the provided unique key, or null if the DLC was not found</returns>
        public static DLCContent GetLoadedDLC(string uniqueKey)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadedDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.UniqueKey == uniqueKey)
                {
                    return content;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the DLC currently loading with the specified name and optional version.
        /// Note this will only detect DLC's that are currently in the process of being loaded and not DLC's that are currently loaded.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">The optional version of the DLC if an explicit match is required</param>
        /// <returns>The DLC that is loading with the provided name and optional version, or null if the DLC was not found</returns>
        public static DLCContent GetLoadingDLC(string name, Version version = null)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadingDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.Name == name)
                {
                    // Check for version
                    if (version == null || content.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return content;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find loaded DLC with version: " + version + " mismatch, nearest version found: " + content.NameInfo.Version);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the DLC currently loading with the specified key.
        /// Note this will only detect successfully loaded DLC's and not DLC's that are currently loaded.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>The DLC that is loading with the provided unique key, or null if the DLC was not found</returns>
        public static DLCContent GetLoadingDLC(string uniqueKey)
        {
            // Check all loaded contents
            foreach (DLCContent content in LoadingDLCContents)
            {
                // Check for matching name
                if (content.NameInfo.UniqueKey == uniqueKey)
                {
                    return content;
                }
            }
            return null;
        }
        #endregion

        #region DRM
        /// <summary>
        /// Try to get all DLC unique keys that are available locally.
        /// Although this operation is async, it will complete instantly on all platforms except Web.
        /// Local keys are simply DLC unique keys which were known about at the time of building the game (DLC profiles created before building the game).
        /// Note that only unique keys for enabled DLC profiles at the time of building the game will be available.
        /// For that reason the array will only list DLC contents that were created during development of the game, whether the DLC content was released or not.
        /// As a result it is highly recommended that you check with the current DRM provider for a true reflection of available DLC content using <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> (If the current platform has DRM support and the DRM provider can support listing unique contents).
        /// Alternatively you might use these local keys in combination with <see cref="IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider, string)"/> to determine whether the DLC is usable (Exists via DRM) and is available (Installed locally).
        /// </summary>
        public static DLCAsync<string[]> FetchLocalDLCUniqueKeys()
        {
            // Get the manifest request - should be instant on all platforms except web
            DLCAsync<DLCManifest> manifest = ManifestAsync;

            // Create request
            DLCAsync<string[]> async = new DLCAsync<string[]>();

            // Wait for operation to complete
            AsyncCommon.RunAsync(WaitForCompleted(manifest, () =>
            {
                // Complete the operation
                if(manifest.IsSuccessful == true)
                {
                    // Update status
                    async.UpdateStatus("Successfully listed local DLC");
                    async.Complete(true, manifest.Result.DLCContents.Select(c => c.DLCUniqueKey).ToArray());
                }
                else
                {
                    // Fail operation
                    async.Error(manifest.Status);
                }
            }));

            return async;
        }

        /// <summary>
        /// Try to get all DLC unique keys that are available remotely. 
        /// Remote keys are simply DLC unique keys which have been published to a DRM provider such as Steamworks.
        /// Some DRM providers may not support listing DLC contents that are published remotely.
        /// Note that only published unique keys will be returned here if the DRM provider supports listing available DLC contents.
        /// Note also that all DLC unique keys will be listed here even if the user does not own or subscribed to the downloadable content. 
        /// For that reason you should use <see cref="IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider, string)"/> to determine whether the DLC is usable (Exists via DRM) and is available (Installed locally).
        /// </summary>
        /// <exception cref="NotSupportedException">No DRM provider is setup for this platform</exception>
        public static DLCAsync<string[]> FetchRemoteDLCUniqueKeys()
        {
            // Check for invalid
            if (DRMProvider == null)
                throw new NotSupportedException("No suitable DRM provider is available on this platform. You will need to use a LoadFrom method to load DLC content");

            try
            {
                // Try to get remote DLC's
                return DRMProvider.GetDLCUniqueKeysAsync(AsyncCommon);
            }
            catch (NotSupportedException)
            {
                // Check for not supported
                return DLCAsync<string[]>.Error("DRM provider does not support listing DLC content");
            }
            catch (Exception e)
            {
                // Check for error during call
                return DLCAsync<string[]>.Error("An unhandled exception was thrown when calling the DRM provider: " + e);
            }
        }

        /// <summary>
        /// Check if the specified DLC is purchased and installed.
        /// Some providers may need to make a web request to check for purchased DLC, so this operations must be async.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the dlc</param>
        /// <returns>True if the dlc is installed or false if not</returns>
        /// <exception cref="NotSupportedException">No DRM provider for ths current platform</exception>
        public static DLCAsync IsAvailable(string uniqueKey)
        {
            // Create async operations
            DLCAsync async = new DLCAsync();
            DLCAsync<KeyValuePair<bool, DLCStreamProvider>> asyncAvailable = CheckDLCAvailableAsync(AsyncCommon, uniqueKey, false);

            // Wait for completed
            AsyncCommon.RunAsync(WaitForCompleted(asyncAvailable, (() =>
            {
                // Update the status
                async.UpdateStatus(asyncAvailable.Status);

                // Complete the operation
                async.Complete(asyncAvailable.IsSuccessful == true && asyncAvailable.Result.Key == true);
            })));

            return async;
        }

        /// <summary>
        /// Request that the dlc with the provided unique key is installed onto the system if it is available to the user.
        /// </summary>
        /// <param name="uniqueKey">The async provider to allow async tasks to be started</param>
        /// <returns>The unique key of the dlc</returns>
        /// <exception cref="NotSupportedException">No DRM provider for ths current platform</exception>
        public static DLCAsync RequestInstall(string uniqueKey)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform");
#else

            // Check for invalid
            if (DRMProvider == null)
                throw new NotSupportedException("No suitable DRM provider is available on this platform. You will need to use a LoadFrom method to load DLC content");

            try
            { 
                // Request installation
                return DRMProvider.RequestInstallDLCAsync(AsyncCommon, uniqueKey);
            }
            catch (NotSupportedException)
            {
                // Check for not supported
                return DLCAsync<string[]>.Error("DRM provider does not support installing DLC content");
            }
            catch (Exception e)
            {
                // Check for error during call
                return DLCAsync<string[]>.Error("An unhandled exception was thrown when calling the DRM provider: " + e);
            }
#endif
        }

        /// <summary>
        /// Request that the dlc with the provided unique key is uninstalled from the system if it is currently installed.
        /// </summary>
        /// <param name="uniqueKey">The unique key of the dlc</param>
        /// <exception cref="NotSupportedException">No DRM provider for ths current platform</exception>
        public static void RequestUninstall(string uniqueKey)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform");
#else

            // Check for invalid
            if (DRMProvider == null)
                throw new NotSupportedException("No suitable DRM provider is available on this platform. You will need to use a LoadFrom method to load DLC content");

            try
            { 
                // Request installation
                DRMProvider.RequestUninstallDLC(uniqueKey);
            }
            catch (NotSupportedException)
            {
                // Check for not supported
                Debug.LogError("DRM provider does not support listing DLC content");
            }
            catch (Exception e)
            {
                // Check for error during call
                Debug.LogError("An unhandled exception was thrown when calling the DRM provider: " + e);
            }
#endif
        }
#endregion


        // ### Load batch
        #region LoadBatchUniqueKeyAsync
        /// <summary>
        /// Attempt to load multiple DLC content simultaneously with the specified unique keys asynchronously.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCFromAsync(string)"/>.
        /// Note that this method will suppress any exceptions and report them in the resulting async operation if caught.
        /// </summary>
        /// <param name="uniqueKeys">An array of unique keys for all DLC content to load</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the user but not available locally. Only available in async mode</param>
        /// <returns>A batch async operation which can report progress for the combined load operation as a whole, or can provide access to each inner load request</returns>
        /// <exception cref="ArgumentNullException">The unique keys are empty</exception>
        public static DLCBatchAsync<DLCContent> LoadDLCBatchAsync(string[] uniqueKeys, bool installOnDemand)
        {
            // Check for null
            if (uniqueKeys == null)
                throw new ArgumentNullException(nameof(uniqueKeys));

            // Create requests collection
            List<DLCAsync<DLCContent>> requests = new List<DLCAsync<DLCContent>>(uniqueKeys.Length);

            // Request load all
            foreach(string uniqueKey in uniqueKeys)
            {
                DLCAsync<DLCContent> async = null;

                try
                {
                    // Handle exceptions
                    async = LoadDLCAsync(uniqueKey, installOnDemand);
                }
                catch(Exception e)
                {
                    async = DLCAsync<DLCContent>.Error(e.Message);
                }

                requests.Add(async);
            }

            // Create batch operation
            DLCBatchAsync<DLCContent> asyncBatch = new DLCBatchAsync<DLCContent>(requests);

            // Run async to ensure that batch progress and status are updated
            AsyncCommon.RunAsync(asyncBatch.UpdateTasksRoutine());
            return asyncBatch;
        }
        #endregion

        #region LoadBatchFromAsync
        /// <summary>
        /// Attempt to load multiple DLC content simultaneously from the paths asynchronously.
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCFromAsync(string)"/>.
        /// Note that this method will suppress any exceptions and report them in the resulting async operation if caught.
        /// </summary>
        /// <param name="paths">An array of paths for all DLC content to load</param>
        /// <returns>A batch async operation which can report progress for the combined load operation as a whole, or can provide access to each inner load request</returns>
        /// <exception cref="ArgumentNullException">The paths are empty</exception>
        public static DLCBatchAsync<DLCContent> LoadDLCBatchFromAsync(string[] paths)
        {
            // Check for null
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            // Create requests collection
            List<DLCAsync<DLCContent>> requests = new List<DLCAsync<DLCContent>>(paths.Length);

            // Request load all
            foreach (string path in paths)
            {
                DLCAsync<DLCContent> async = null;

                try
                {
                    // Handle exceptions
                    async = LoadDLCFromAsync(path);
                }
                catch (Exception e)
                {
                    async = DLCAsync<DLCContent>.Error(e.Message);
                }

                requests.Add(async);
            }

            // Create batch operation
            DLCBatchAsync<DLCContent> asyncBatch = new DLCBatchAsync<DLCContent>(requests);

            // Run async to ensure that batch progress and status are updated
            AsyncCommon.RunAsync(asyncBatch.UpdateTasksRoutine());
            return asyncBatch;
        }
        #endregion


        // ### Load
        #region LoadStreamProvider
        /// <summary>
        /// Attempt to load DLC content with the specified unique key.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will need to be available (Owned and installed) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="streamProvider">The stream provider for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentNullException">The stream provider is null</exception>
        /// <exception cref="NotSupportedException">There is not suitable DRM provider for this platform</exception>
        /// <exception cref="TimeoutException">A DRM request timed out and the operation is aborted to avoid infinite waiting or freezing the application</exception>
        /// <exception cref="DLCNotAvailableException">The requested DLC is owned but is not currently installed or available locally. You may need to request that the DLC is installed using <see cref="IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider, string)"/></exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCContent LoadDLC(DLCStreamProvider streamProvider)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCAsync` instead!");
#else

            // Check provider
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));

            // Create the content
            DLCContent content = DLCContent.GetDLCContentHost(DRMProvider);

            // Attempt to load from provided path
            content.LoadDLCContent(streamProvider, DLCLoadMode.Full);

            return content;
#endif
        }

        /// <summary>
        /// Attempt to load DLC content with the specified unique key.
        /// This is a reduced load mode and it will only be possible to access the <see cref="IDLCMetadata"/> for the DLC and nothing more.
        /// Designed to be a quick operation for accessing metadata, but may take some time to complete depending upon DRM provider and availability.
        /// Use <see cref="LoadDLCAsync(string, bool)"/> if you need to load assets from the DLC.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will need to be available (Owned and installed) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="streamProvider">The stream provider for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentNullException">The stream provider is null</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="TimeoutException">A DRM request timed out and the operation is aborted to avoid infinite waiting or freezing the application</exception>
        /// <exception cref="DLCNotAvailableException">The requested DLC is owned but is not currently installed or available locally. You may need to request that the DLC is installed using <see cref="IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider, string)"/></exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static IDLCMetadata LoadDLCMetadata(DLCStreamProvider streamProvider)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCMetadataAsync` instead!");
#else

            // Check provider
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));

            // Create the content
            DLCContent content = DLCContent.GetDLCContentHost();

            // Attempt to load from provided path
            content.LoadDLCContent(streamProvider, DLCLoadMode.Metadata);

            // Get metadata
            IDLCMetadata metadata = content.Metadata;

            // Destroy content
            content.Dispose();

            return metadata;
#endif
        }

        /// <summary>
        /// Attempt to load DLC content with the specified unique key.
        /// This is a reduced load mode and it will be possible to access extra metadata for assets and scenes, but it will not be possible to load any asset or scene content into the game.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will need to be available (Owned and installed) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="streamProvider">The stream provider for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentNullException">The stream provider is null</exception>
        /// <exception cref="NotSupportedException">There is not suitable DRM provider for this platform</exception>
        /// <exception cref="TimeoutException">A DRM request timed out and the operation is aborted to avoid infinite waiting or freezing the application</exception>
        /// <exception cref="DLCNotAvailableException">The requested DLC is owned but is not currently installed or available locally. You may need to request that the DLC is installed using <see cref="IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider, string)"/></exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCContent LoadDLCMetadataWithAssets(DLCStreamProvider streamProvider)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCMetadataWithAssetsAsync` instead!");
#else
            
            // Check provider
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));

            // Create the content
            DLCContent content = DLCContent.GetDLCContentHost();

            // Attempt to load from provided path
            content.LoadDLCContent(streamProvider, DLCLoadMode.MetadataWithAssets);

            return content;
#endif
        }
        #endregion

        #region LoadUniqueKey
        /// <summary>
        /// Attempt to load DLC content with the specified unique key.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will need to be available (Owned and installed) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The unique key is null or empty</exception>
        /// <exception cref="NotSupportedException">There is not suitable DRM provider for this platform</exception>
        /// <exception cref="TimeoutException">A DRM request timed out and the operation is aborted to avoid infinite waiting or freezing the application</exception>
        /// <exception cref="DLCNotAvailableException">The requested DLC is owned but is not currently installed or available locally. You may need to request that the DLC is installed using <see cref="IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider, string)"/></exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        [Obsolete("Non-Async variants are no longer supported due to the need to make async DRM requests: Use LoadDLCAsync")]
        public static DLCContent LoadDLC(string uniqueKey)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCAsync` instead!");
#else

            // Check key
            if (string.IsNullOrEmpty(uniqueKey) == true)
                throw new ArgumentException(nameof(uniqueKey) + " is null or empty");

            // Check for cached loaded or loading content
            DLCContent dlc;
            if ((dlc = GetDLC(uniqueKey)) != null)
                return dlc;

            // Check if DLC is available
            DLCStreamProvider streamProvider;
            bool available = CheckDLCAvailable(uniqueKey, out streamProvider);

            // DLC is not available
            if (available == false)
                return null;

            // Try to load the DLC
            return LoadDLC(streamProvider);    
#endif
        }

        /// <summary>
        /// Attempt to load DLC content with the specified unique key.
        /// This is a reduced load mode and it will only be possible to access the <see cref="IDLCMetadata"/> for the DLC and nothing more.
        /// Designed to be a quick operation for accessing metadata, but may take some time to complete depending upon DRM provider and availability.
        /// Use <see cref="LoadDLCAsync(string, bool)"/> if you need to load assets from the DLC.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will need to be available (Owned and installed) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The unique key is null or empty</exception>
        /// <exception cref="NotSupportedException">There is not suitable DRM provider for this platform</exception>
        /// <exception cref="TimeoutException">A DRM request timed out and the operation is aborted to avoid infinite waiting or freezing the application</exception>
        /// <exception cref="DLCNotAvailableException">The requested DLC is owned but is not currently installed or available locally. You may need to request that the DLC is installed using <see cref="IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider, string)"/></exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        [Obsolete("Non-Async variants are no longer supported due to the need to make async DRM requests: Use LoadDLCMetadataAsync")]
        public static IDLCMetadata LoadDLCMetadata(string uniqueKey)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCMetadataAsync` instead!");
#else

            // Check key
            if (string.IsNullOrEmpty(uniqueKey) == true)
                throw new ArgumentException(nameof(uniqueKey) + " is null or empty");

            // Check if DLC is available
            DLCStreamProvider streamProvider;
            bool available = CheckDLCAvailable(uniqueKey, out streamProvider);

            // DLC is not available
            if (available == false)
                return null;

            // Try to load the DLC
            return LoadDLCMetadata(streamProvider);
#endif
        }

        /// <summary>
        /// Attempt to load DLC content with the specified unique key.
        /// This is a reduced load mode and it will be possible to access extra metadata for assets and scenes, but it will not be possible to load any asset or scene content into the game.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will need to be available (Owned and installed) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The unique key is null or empty</exception>
        /// <exception cref="NotSupportedException">There is not suitable DRM provider for this platform</exception>
        /// <exception cref="TimeoutException">A DRM request timed out and the operation is aborted to avoid infinite waiting or freezing the application</exception>
        /// <exception cref="DLCNotAvailableException">The requested DLC is owned but is not currently installed or available locally. You may need to request that the DLC is installed using <see cref="IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider, string)"/></exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>#
        [Obsolete("Non-Async variants are no longer supported due to the need to make async DRM requests: Use LoadDLCMetadataWithAssetsAsync")]
        public static DLCContent LoadDLCMetadataWithAssets(string uniqueKey)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCMetadataWithAssetsAsync` instead!");
#else

            // Check key
            if (string.IsNullOrEmpty(uniqueKey) == true)
                throw new ArgumentException(nameof(uniqueKey) + " is null or empty");

            // Check if DLC is available
            DLCStreamProvider streamProvider;
            bool available = CheckDLCAvailable(uniqueKey, out streamProvider);

            // DLC is not available
            if (available == false)
                return null;

            // Try to load the DLC
            return LoadDLCMetadataWithAssets(streamProvider);
#endif
        }

        private static bool CheckDLCAvailable(string uniqueKey, out DLCStreamProvider streamProvider)
        {
            // Check for DRM provider
            IDRMProvider provider = DRMProvider;

            // Check for invalid
            if (provider == null)
                throw new NotSupportedException("No suitable DRM provider is available on this platform. You will need to use a LoadFrom method to load DLC content");

            // Some DRM providers may not support listing DLC unique keys
            try
            {
                // Send request for remote keys
                DLCAsync<string[]> uniqueKeysAsync = FetchLocalDLCUniqueKeys();

                // Wait for operation to complete - Block the main thread
                // This will timeout if it takes too long with an exception
                uniqueKeysAsync.Await();

                // Check for potential DLC valid key
                if (uniqueKeysAsync.Result == null || Array.Exists(uniqueKeysAsync.Result, k => k == uniqueKey) == false)
                {
                    Debug.LogWarning("DLC unique key potentially does not exist `" + uniqueKey + "`. Checking DRM provider for DLC that the game is not aware of...");
                }
            }
            catch (NotSupportedException) 
            {
                Debug.LogWarning("A list remote DLC request was made but the DRM provider does not support listing DLC contents, falling back to local DLC checks...");
            }

            // Send request from DLC provider
            DLCAsync async = provider.IsDLCAvailableAsync(AsyncCommon, uniqueKey);

            // Wait for operation to complete - Block the main thread
            // This will timeout if it takes to long with an exception
            async.Await();

            // Check result
            if(async.IsSuccessful == false)
            {
                Debug.LogWarning("DRM provider failed to check if DLC is available `" + uniqueKey + "`. The DLC may be valid but not usable at this time: " + async.Status);
                streamProvider = null;
                return false;
            }

            // Check available
            bool available =  async.IsSuccessful;

            // Update install path
            streamProvider = null;            

            // Get the stream
            if(available == true)
            {
                // Get the stream
                DLCAsync<DLCStreamProvider> streamAsync = provider.GetDLCStreamAsync(AsyncCommon, uniqueKey);

                // Wait for operation to complete - Block the main thread
                // This will timeout if it takes to long with an exception
                streamAsync.Await();

                // Check for success
                if (streamAsync.IsSuccessful == true)
                    streamProvider = streamAsync.Result;
            }

            // Check for available but DLC file not found
            if (available == true && streamProvider == null)
                throw new DLCNotAvailableException("DLC is available but not installed locally. You may need to request that the DLC is installed on demand");

            return available;
        }
#endregion

        #region LoadFrom
        /// <summary>
        /// Attempt to load DLC content from the specified file path.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="path">The file path containing the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC</returns>
        /// <exception cref="ArgumentException">The path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCContent LoadDLCFrom(string path)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCAsync` instead!");
#else

            // Check arg
            if (string.IsNullOrEmpty(path) == true)
                throw new ArgumentException(nameof(path) + " is null or empty");

            // Check for cached loaded or loading content
            DLCContent dlc;
            if ((dlc = GetDLCFrom(path)) != null)
                return dlc;

            // Load from path
            return LoadDLC(DLCStreamProvider.FromFile(path));
#endif
        }

        /// <summary>
        /// Attempt to load DLC metadata only from the specified file path.
        /// Intended to be very quick access and will only load the absolute minimum amount of data required to access the meta information for the DLC.
        /// Only the DLC metadata will be loaded into memory.
        /// </summary>
        /// <param name="path">The file path containing the DLC content</param>
        /// <returns>A <see cref="IDLCMetadata"/> containing the loaded DLC metadata</returns>
        /// <exception cref="ArgumentException">The path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static IDLCMetadata LoadDLCMetadataFrom(string path)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCMetadataAsync` instead!");
#else

            // Check arg
            if (string.IsNullOrEmpty(path) == true)
                throw new ArgumentException(nameof(path) + " is null or empty");

            // Load from path
            return LoadDLCMetadata(DLCStreamProvider.FromFile(path));
#endif
        }

        /// <summary>
        /// Attempt to load DLC metadata with assets only from the specified file path.
        /// Intended to be very quick access and will only load the absolute minimum amount of data required to access the meta information for the DLC.
        /// Only the DLC metadata and asset metadata will be loaded into memory (Assets, scenes and scripts will not be loadable).
        /// <see cref="DLCContent.SharedAssets"/> and <see cref="DLCContent.SceneAssets"/> will be accessible after successful load to discover meta information about included assets.
        /// </summary>
        /// <param name="path">The file path containing the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC</returns>
        /// <exception cref="ArgumentException">The path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCContent LoadDLCMetadataWithAssetsFrom(string path)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform. Use `LoadDLCMetadataWithAssetsAsync` instead!");
#else

            // Check arg
            if (string.IsNullOrEmpty(path) == true)
                throw new ArgumentException(nameof(path) + " is null or empty");

            // Load from path
            return LoadDLCMetadataWithAssets(DLCStreamProvider.FromFile(path));
#endif
        }
        #endregion



        // ### Load Async
        #region LoadStreamProviderAsync
        /// <summary>
        /// Attempt to load DLC content from the specified stream provider asynchronously.
        /// If the target DLC is already being loaded or has been loaded, this method will simply return the current load operation or a completed operation with the already loaded <see cref="DLCContent"/>.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCFromAsync(string)"/>.
        /// </summary>
        /// <param name="streamProvider">The stream provider for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentNullException">The stream provider is null</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCAsync(DLCStreamProvider streamProvider)
        {
            // Check for null
            if(streamProvider == null) 
                throw new ArgumentNullException(nameof(streamProvider));

            // Create the content
            DLCContent content = DLCContent.GetDLCContentHost(DRMProvider);

            // Create async
            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            try
            {
                // Run async
                content.LoadDLCContentAsync(async, streamProvider, DLCLoadMode.Full);
            }
            catch(Exception e)
            {
                async.Error(e.Message);
            }

            return async;
        }

        /// <summary>
        /// Attempt to load DLC metadata only from the specified stream provider asynchronously in metadata.
        /// This is a reduced load mode and it will only be possible to access the <see cref="IDLCMetadata"/> for the DLC and nothing more.
        /// Designed to be a quick operation for accessing metadata, but may take some time to complete depending upon DRM provider and availability.
        /// Use <see cref="LoadDLCAsync(string, bool)"/> if you need to load assets from the DLC.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC metadata will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCMetadataFrom(string)"/>.
        /// </summary>
        /// <param name="streamProvider">The stream provider for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentNullException">The stream provider is null</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<IDLCMetadata> LoadDLCMetadataAsync(DLCStreamProvider streamProvider)
        {
            // Check for null
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));

            // Create the content
            DLCContent content = DLCContent.GetDLCContentHost();

            // Create async
            DLCAsync<IDLCMetadata> async = new DLCAsync<IDLCMetadata>();
            DLCAsync<DLCContent> loadAsync = new DLCAsync<DLCContent>();

            try
            {
                // Run async
                content.LoadDLCContentAsync(loadAsync, streamProvider, DLCLoadMode.Metadata);
            }
            catch(Exception e)
            {
                async.Error(e.Message);
            }

            // Wait for loaded
            AsyncCommon.RunAsync(WaitForCompleted(loadAsync, () =>
            {
                // Complete operation
                async.Complete(loadAsync.IsSuccessful, loadAsync.IsSuccessful ? loadAsync.Result.Metadata : null);

                // Release bundle
                if (loadAsync.IsSuccessful == true)
                    loadAsync.Result.Dispose();
            }));

            return async;
        }

        /// <summary>
        /// Attempt to load DLC content from the specified stream provider asynchronously in metadata with assets mode.
        /// This is a reduced load mode and it will be possible to access extra metadata for assets and scenes, but it will not be possible to load any asset or scene content into the game.
        /// Use <see cref="LoadDLCAsync(string, bool)"/> if you need to load assets from the DLC.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCMetadataWithAssetsFrom(string)"/>.
        /// </summary>
        /// <param name="streamProvider">The stream provider for the DLC content</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentNullException">The stream provider is null</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCMetadataWithAssetsAsync(DLCStreamProvider streamProvider)
        {
            // Check for null
            if (streamProvider == null)
                throw new ArgumentNullException(nameof(streamProvider));

            // Create the content
            DLCContent content = DLCContent.GetDLCContentHost();

            // Create async
            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            try
            {
                // Run async
                content.LoadDLCContentAsync(async, streamProvider, DLCLoadMode.MetadataWithAssets);
            }
            catch(Exception e)
            {
                async.Error(e.Message);
            }

            return async;
        }
        #endregion

        #region LoadNameAsync
        /// <summary>
        /// Attempt to load DLC content with the specified name and optional version asynchronously.
        /// If the target DLC is already being loaded or has been loaded, this method will simply return the current load operation or a completed operation with the already loaded <see cref="DLCContent"/>.
        /// This method is slow (and available as async only) since all available DLC need to be evaluated at the metadata level in order to find a matching name and version before loading can begin. Use <see cref="LoadDLCAsync(string, bool)"/> for quicker load times
        /// The available DLC that will be scanned is determined from the active DRM provider via <see cref="FetchRemoteDLCUniqueKeys"/>.
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCFromAsync(string)"/>.
        /// </summary>
        /// <param name="dlcName">The name of the DLC to load. This is the friendly name assigned at build time and not the unique key</param>
        /// <param name="version">An optional version if you need to load the specific version of a DLC</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the used but not available locally. Only available in async mode</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The DLC name is null or empty</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCNameAsync(string dlcName, Version version = null, bool installOnDemand = false)
        {
            // Check key
            if (string.IsNullOrEmpty(dlcName) == true)
                throw new ArgumentException(nameof(dlcName) + " is null or empty");

            // Create async
            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();
            AsyncCommon.RunAsync(LoadDLCNameAsyncRoutine(async, AsyncCommon, dlcName, version, installOnDemand, DLCLoadMode.Full));

            return async;
        }

        /// <summary>
        /// Attempt to load DLC metadata only with the specified name and optional version asynchronously.
        /// This is a reduced load mode and it will only be possible to access the <see cref="IDLCMetadata"/> for the DLC and nothing more.
        /// Designed to be a quick operation for accessing metadata, but may take some time to complete depending upon DRM provider and availability.
        /// Use <see cref="LoadDLCNameAsync(string, Version, bool)"/> if you need to load assets from the DLC.
        /// This method is slow (and available as async only) since all available DLC need to be evaluated at the metadata level in order to find a matching name and version before loading can begin. Use <see cref="LoadDLCMetadataAsync(string, bool)"/> for quicker load times
        /// The available DLC that will be scanned is determined from the active DRM provider via <see cref="FetchRemoteDLCUniqueKeys"/>.
        /// The DLC metadata will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCMetadataFrom(string)"/>.
        /// </summary>
        /// <param name="dlcName">The name of the DLC to load. This is the friendly name assigned at build time and not the unique key</param>
        /// <param name="version">An optional version if you need to load the specific version of a DLC</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the used but not available locally. Only available in async mode</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The DLC name is null or empty</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<IDLCMetadata> LoadDLCNameMetadataAsync(string dlcName, Version version = null, bool installOnDemand = false)
        {
            // Check key
            if (string.IsNullOrEmpty(dlcName) == true)
                throw new ArgumentException(nameof(dlcName) + " is null or empty");

            // Create async
            DLCAsync<IDLCMetadata> async = new DLCAsync<IDLCMetadata>();
            DLCAsync<DLCContent> loadAsync = new DLCAsync<DLCContent>();

            // Run async
            AsyncCommon.RunAsync(LoadDLCNameAsyncRoutine(loadAsync, AsyncCommon, dlcName, version, installOnDemand, DLCLoadMode.Metadata));

            // Wait for loaded
            AsyncCommon.RunAsync(WaitForCompleted(loadAsync, () =>
            {
                // Complete operation
                async.Complete(loadAsync.IsSuccessful, loadAsync.IsSuccessful ? loadAsync.Result.Metadata : null);

                // Release bundle
                if (loadAsync.IsSuccessful == true)
                    loadAsync.Result.Dispose();
            }));

            // Wait for completed
            return async;
        }

        /// <summary>
        /// Attempt to load DLC content with the specified name and optional version asynchronously in metadata with assets mode.
        /// This is a reduced load mode and it will be possible to access extra metadata for assets and scenes, but it will not be possible to load any asset or scene content into the game.
        /// Use <see cref="LoadDLCNameAsync(string, Version, bool)"/> if you need to load assets from the DLC.
        /// This method is slow (and available as async only) since all available DLC need to be evaluated at the metadata level in order to find a matching name and version before loading can begin. Use <see cref="LoadDLCMetadataAsync(string, bool)"/> for quicker load times
        /// The available DLC that will be scanned is determined from the active DRM provider via <see cref="FetchRemoteDLCUniqueKeys"/>.
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCMetadataWithAssetsFrom(string)"/>.
        /// </summary>
        /// <param name="dlcName">The name of the DLC to load. This is the friendly name assigned at build time and not the unique key</param>
        /// <param name="version">An optional version if you need to load the specific version of a DLC</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the used but not available locally. Only available in async mode</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The DLC name is null or empty</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCNameMetadataWithAssetsAsync(string dlcName, Version version = null, bool installOnDemand = false)
        {
            // Check key
            if (string.IsNullOrEmpty(dlcName) == true)
                throw new ArgumentException(nameof(dlcName) + " is null or empty");

            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            // Run async
            AsyncCommon.RunAsync(LoadDLCNameAsyncRoutine(async, AsyncCommon, dlcName, version, installOnDemand, DLCLoadMode.MetadataWithAssets));
            return async;
        }

        private static IEnumerator LoadDLCNameAsyncRoutine(DLCAsync<DLCContent> async, IDLCAsyncProvider asyncProvider, string dlcName, Version version, bool installOnDemand, DLCLoadMode loadMode)
        {
            // try to get unique key
            DLCAsync<string> uniqueKeyAsync = GetDLCUniqueKeyAsync(dlcName, version, installOnDemand);

            // Wait for completed
            yield return uniqueKeyAsync;

            // Check for completed
            if(uniqueKeyAsync.IsSuccessful == false)
            {
                async.Error(uniqueKeyAsync.Status);
                yield break;
            }

            // Check for available
            DLCAsync<KeyValuePair<bool, DLCStreamProvider>> availableAsync = CheckDLCAvailableAsync(asyncProvider, uniqueKeyAsync.Result, installOnDemand);

            // Wait for completed
            while (availableAsync.IsDone == false)
            {
                // Update progress and status
                async.UpdateStatus(availableAsync.Status);
                async.UpdateProgress(availableAsync.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for completed
            if (availableAsync.IsSuccessful == false)
            {
                async.Error(availableAsync.Status);
                yield break;
            }

            // Load the DLC
            // Create the content - Pass DRM provider if we are loading in full mode so that DLC usage progress can be tracked
            DLCContent content = DLCContent.GetDLCContentHost(loadMode == DLCLoadMode.Full ? DRMProvider : null);

            // Create async
            DLCAsync<DLCContent> loadAsync = new DLCAsync<DLCContent>();

            try
            {
                // Attempt to load from provided path
                content.LoadDLCContentAsync(async, availableAsync.Result.Value, loadMode);
            }
            catch (Exception e)
            {
                async.Error(e.Message);
            }

            // Wait for completed
            while (loadAsync.IsDone == false)
            {
                // Update progress and status
                async.UpdateStatus(loadAsync.Status);
                async.UpdateProgress(0.5f + (loadAsync.Progress * 0.5f));

                // Wait a frame
                yield return null;
            }

            // Complete operation
            async.Complete(loadAsync.IsSuccessful, loadAsync.Result);
        }
        #endregion

        #region LoadUniqueKeyAsync
        /// <summary>
        /// Attempt to load DLC content with the specified unique key asynchronously.
        /// If the target DLC is already being loaded or has been loaded, this method will simply return the current load operation or a completed operation with the already loaded <see cref="DLCContent"/>.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCFromAsync(string)"/>.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC content</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the used but not available locally. Only available in async mode</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The unique key is null or empty</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCAsync(string uniqueKey, bool installOnDemand = false)
        {
            // Check key
            if (string.IsNullOrEmpty(uniqueKey) == true)
                throw new ArgumentException(nameof(uniqueKey) + " is null or empty");

            // Check for cached loaded or loading content
            DLCContent dlc;
            if ((dlc = GetDLC(uniqueKey)) != null)
                return dlc.AwaitLoadedAsync(AsyncCommon);

            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            // Run async
            AsyncCommon.RunAsync(LoadDLCAsyncRoutine(async, AsyncCommon, uniqueKey, installOnDemand, DLCLoadMode.Full));
            return async;
        }

        /// <summary>
        /// Attempt to load DLC metadata only with the specified unique key asynchronously in metadata with assets mode.
        /// This is a reduced load mode and it will only be possible to access the <see cref="IDLCMetadata"/> for the DLC and nothing more.
        /// Designed to be a quick operation for accessing metadata, but may take some time to complete depending upon DRM provider and availability.
        /// Use <see cref="LoadDLCAsync(string, bool)"/> if you need to load assets from the DLC.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC metadata will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCMetadataFrom(string)"/>.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC content</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the used but not available locally. Only available in async mode</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The unique key is null or empty</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<IDLCMetadata> LoadDLCMetadataAsync(string uniqueKey, bool installOnDemand = false)
        {
            // Check key
            if (string.IsNullOrEmpty(uniqueKey) == true)
                throw new ArgumentException(nameof(uniqueKey) + " is null or empty");

            // Create async
            DLCAsync<IDLCMetadata> async = new DLCAsync<IDLCMetadata>();
            DLCAsync<DLCContent> loadAsync = new DLCAsync<DLCContent>();

            // Run async
            AsyncCommon.RunAsync(LoadDLCAsyncRoutine(loadAsync, AsyncCommon, uniqueKey, installOnDemand, DLCLoadMode.Metadata));

            // Wait for loaded
            AsyncCommon.RunAsync(WaitForCompleted(loadAsync, () =>
            {
                // Complete operation
                async.Complete(loadAsync.IsSuccessful, loadAsync.IsSuccessful ? loadAsync.Result.Metadata : null);

                // Release bundle
                if (loadAsync.IsSuccessful == true)
                    loadAsync.Result.Dispose();
            }));

            // Wait for completed
            return async;
        }

        /// <summary>
        /// Attempt to load DLC content with the specified unique key asynchronously in metadata with assets mode.
        /// This is a reduced load mode and it will be possible to access extra metadata for assets and scenes, but it will not be possible to load any asset or scene content into the game.
        /// Use <see cref="LoadDLCAsync(string, bool)"/> if you need to load assets from the DLC.
        /// Note that the unique key can be different between platforms and you should check the DLC profile to ensure that the correct key is used for the current platform. 
        /// Alternatively you may be able to query <see cref="IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider)"/> to enumerate all available DLC's, but note that some DRM providers may not implement that property or only partially implement it (May not return all possible DLC's).
        /// The DLC will be loaded on the background thread so it is possible to continue with gameplay or show an animated loading screen.
        /// The DLC will need to be available (Owned and installed, or owned with <paramref name="installOnDemand"/> enabled) from the current DRM provider in order to succeed.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// Note that a DRM provider is required for the current platform, otherwise you should use <see cref="LoadDLCMetadataWithAssetsFrom(string)"/>.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC content</param>
        /// <param name="installOnDemand">Should the DLC be installed if it is owned by the used but not available locally. Only available in async mode</param>
        /// <returns>A <see cref="DLCContent"/> containing the loaded DLC or null if the DLC could not be loaded</returns>
        /// <exception cref="ArgumentException">The unique key is null or empty</exception>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCMetadataWithAssetsAsync(string uniqueKey, bool installOnDemand = false)
        {
            // Check key
            if (string.IsNullOrEmpty(uniqueKey) == true)
                throw new ArgumentException(nameof(uniqueKey) + " is null or empty");

            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            // Run async
            AsyncCommon.RunAsync(LoadDLCAsyncRoutine(async, AsyncCommon, uniqueKey, installOnDemand, DLCLoadMode.MetadataWithAssets));
            return async;
        }

        private static IEnumerator LoadDLCAsyncRoutine(DLCAsync<DLCContent> async, IDLCAsyncProvider asyncProvider, string uniqueKey, bool installOnDemand, DLCLoadMode loadMode)
        {
            // Check for available
            DLCAsync<KeyValuePair<bool, DLCStreamProvider>> availableAsync = CheckDLCAvailableAsync(asyncProvider, uniqueKey, installOnDemand);

            // Wait for completed
            while(availableAsync.IsDone == false)
            {
                // Update progress and status
                async.UpdateStatus(availableAsync.Status);
                async.UpdateProgress(availableAsync.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for completed
            if(availableAsync.IsSuccessful == false)
            {
                async.Error(availableAsync.Status);
                yield break;
            }

            // Load the DLC
            // Create the content - Pass DRM provider if we are loading in full mode so that DLC usage progress can be tracked
            DLCContent content = DLCContent.GetDLCContentHost(loadMode == DLCLoadMode.Full ? DRMProvider : null);

            // Create async
            DLCAsync<DLCContent> loadAsync = new DLCAsync<DLCContent>();

            try
            {
                // Attempt to load from provided path
                content.LoadDLCContentAsync(async, availableAsync.Result.Value, loadMode);
            }
            catch (Exception e)
            {
                async.Error(e.Message);
            }

            // Wait for completed
            while(loadAsync.IsDone == false)
            {
                // Update progress and status
                async.UpdateStatus(loadAsync.Status);
                async.UpdateProgress(0.5f + (loadAsync.Progress * 0.5f));

                // Wait a frame
                yield return null;
            }

            // Complete operation
            async.Complete(loadAsync.IsSuccessful, loadAsync.Result);
        }

        private static DLCAsync<KeyValuePair<bool, DLCStreamProvider>> CheckDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey, bool installOnDemand)
        {
            // Check for invalid
            if (DRMProvider == null)
                throw new NotSupportedException("No suitable DRM provider is available on this platform. You will need to use a LoadFrom method to load DLC content");

            // Create async
            DLCAsync<KeyValuePair<bool, DLCStreamProvider>> async = new DLCAsync<KeyValuePair<bool, DLCStreamProvider>>();

            // Run async
            asyncProvider.RunAsync(WaitForCheckDLCAvailableAsync(async, asyncProvider, uniqueKey, installOnDemand));

            return async;
        }

        private static IEnumerator WaitForCheckDLCAvailableAsync(DLCAsync<KeyValuePair<bool, DLCStreamProvider>> async, IDLCAsyncProvider asyncProvider, string uniqueKey, bool installOnDemand)
        {
            // Check for DRM provider
            IDRMProvider provider = DRMProvider;

            // Request for DLC keys
            DLCAsync<string[]> uniqueKeysAsync = FetchLocalDLCUniqueKeys();

            // Wait for request
            yield return uniqueKeysAsync;

            // Check for potential DLC valid key
            if (uniqueKeysAsync == null || uniqueKeysAsync.Result == null || Array.Exists(uniqueKeysAsync.Result, k => k == uniqueKey) == false)
            {
                Debug.LogWarning("DLC unique key potentially does not exist. Checking DRM provider for DLC that the game is not aware of...");
            }


            // Send request from DLC provider
            DLCAsync availableAsync = provider.IsDLCAvailableAsync(AsyncCommon, uniqueKey);

            // Update status
            async.UpdateStatus("Checking if DLC is available");

            // Wait for completed
            while(availableAsync.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(availableAsync.Progress);

                // Wait a frame
                yield return null;
            }

            // Check result
            if (availableAsync.IsSuccessful == false)
            {
                async.Error("DRM provider informed that DLC is not available. The DLC may not be owned by the user or there may be connection, hosting or service issues: " + availableAsync);
                yield break;
            }

            // Check available
            bool available = availableAsync.IsSuccessful;

            // Update install path
            DLCStreamProvider streamProvider = null;

            // Check for available
            if (available == true)
            {
                // Send request for DLC stream
                DLCAsync<DLCStreamProvider> streamAsync = null;

                try
                {
                    streamAsync = provider.GetDLCStreamAsync(AsyncCommon, uniqueKey);
                }
                catch (NotSupportedException) 
                {
                    Debug.LogWarning("A DLC installation request was made but the DRM provider does not support install on demand feature");
                }

                // Check for supported by DRM
                if (streamAsync != null)
                {
                    // Update status
                    async.UpdateStatus("Fetching DLC stream");

                    // Wait for completed
                    while (streamAsync.IsDone == false)
                    {
                        // Update progress
                        async.UpdateProgress(streamAsync.Progress);

                        // Wait a frame
                        yield return null;
                    }

                    // Check result
                    if (streamAsync.IsSuccessful == false)
                    {
                        async.Error("DRM provider failed to get DLC stream. The DLC may be valid but not usable at this time perhaps due to connection, hosting or service issues: " + streamAsync.Status);
                        yield break;
                    }

                    // Get the stream
                    streamProvider = streamAsync.Result;
                }
            }

            // Check for available but DLC file not found
            if (available == true && streamProvider == null)
            {
                // Install may be required
                if(installOnDemand == true)
                {
                    // Request install
                    DLCAsync installAsync = provider.RequestInstallDLCAsync(asyncProvider, uniqueKey);

                    // Update status
                    async.UpdateStatus("Installing DLC on demand");

                    // Wait for completed
                    while(installAsync.IsDone == false)
                    {
                        // Update progress
                        async.UpdateProgress(installAsync.Progress);

                        // Wait a frame
                        yield return null;
                    }

                    // Check for success
                    available = installAsync.IsSuccessful;

                    // Get the stream provider once more now that the content is installed
                    // Check for available
                    if (available == true)
                    {
                        // Send request for DLC stream
                        DLCAsync<DLCStreamProvider> streamAsync = provider.GetDLCStreamAsync(AsyncCommon, uniqueKey);

                        // Update status
                        async.UpdateStatus("Fetching DLC stream");

                        // Wait for completed
                        while (streamAsync.IsDone == false)
                        {
                            // Update progress
                            async.UpdateProgress(streamAsync.Progress);

                            // Wait a frame
                            yield return null;
                        }

                        // Check result
                        if (streamAsync.IsSuccessful == false)
                        {
                            async.Error("DRM provider failed to get DLC stream. The DLC may be valid but not usable at this time perhaps due to connection, hosting or service issues: " + streamAsync.Status);
                            yield break;
                        }

                        // Get the stream
                        streamProvider = streamAsync.Result;
                    }
                }
                else
                {
                    async.Error("DLC is available but not installed locally. You may need to request that the DLC is installed on demand");
                    yield break;
                }
            }

            // Complete operation
            async.Complete(true, new KeyValuePair<bool, DLCStreamProvider>(available, streamProvider));
        }
        #endregion

        #region LoadFromAsync
        /// <summary>
        /// Attempt to load DLC content from the specified file path asynchronously.
        /// The DLC will be loaded into memory and data may be preloaded according to the preload options set at build time.
        /// </summary>
        /// <param name="path">The file path containing the DLC content</param>
        /// <returns>A <see cref="DLCAsync"/> operation that can be awaited and provides access to the loaded DLC</returns>
        /// <exception cref="ArgumentException">The path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCFromAsync(string path)
        {
            // Check arg
            if (string.IsNullOrEmpty(path) == true)
                throw new ArgumentException(nameof(path) + " is null or empty");

            // Check for cached loaded or loading content
            DLCContent dlc;
            if ((dlc = GetDLCFrom(path)) != null)
                return dlc.AwaitLoadedAsync(AsyncCommon);

            // Load from file
            return LoadDLCAsync(DLCStreamProvider.FromFile(path));
        }

        /// <summary>
        /// Attempt to load DLC metadata only from the specified file path asynchronously.
        /// Intended to be very quick access and will only load the absolute minimum amount of data required to access the meta information for the DLC.
        /// Only the DLC metadata will be loaded into memory.
        /// </summary>
        /// <param name="path">The file path containing the DLC content</param>
        /// <returns>A <see cref="DLCAsync"/> operation that can be awaited and provides access to the DLC metadata</returns>
        /// <exception cref="ArgumentException">The path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<IDLCMetadata> LoadDLCMetadataFromAsync(string path)
        {
            // Check arg
            if (string.IsNullOrEmpty(path) == true)
                throw new ArgumentException(nameof(path) + " is null or empty");

            // Load from file
            return LoadDLCMetadataAsync(DLCStreamProvider.FromFile(path));
        }

        /// <summary>
        /// Attempt to load DLC metadata with assets only from the specified file path asynchronously.
        /// Intended to be very quick access and will only load the absolute minimum amount of data required to access the meta information for the DLC.
        /// Only the DLC metadata and asset metadata will be loaded into memory (Assets, scenes and scripts will not be loadable).
        /// <see cref="DLCContent.SharedAssets"/> and <see cref="DLCContent.SceneAssets"/> will be accessible after successful load to discover meta information only about included assets.
        /// </summary>
        /// <param name="path">The file path containing the DLC content</param>
        /// <returns>A <see cref="DLCAsync"/> operation that can be awaited and provides access to the loaded DLC</returns>
        /// <exception cref="ArgumentException">The path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<DLCContent> LoadDLCMetadataWithAssetsFromAsync(string path)
        {
            // Check arg
            if (string.IsNullOrEmpty(path) == true)
                throw new ArgumentException(nameof(path) + " is null or empty");

            // Load from file
            return LoadDLCMetadataWithAssetsAsync(DLCStreamProvider.FromFile(path));
        }
        #endregion



        // ### Load Asset
        #region LoadAsset
        ///<summary>
        /// Attempts to load an asset with the specified name or path from any loaded DLC.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="LoadAssetAsync(string, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>The loaded asset</returns>
        public static Object LoadAsset(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true)
        {
            return LoadAsset<Object>(nameOrPath, dlcUniqueKey, throwOnError);
        }

        ///<summary>
        /// Attempts to load an asset with the specified name or path from any loaded DLC.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="LoadAssetAsync{T}(string, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <typeparam name="T">The type of asset to return</typeparam>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>The loaded asset</returns>
        public static T LoadAsset<T>(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true) where T : Object
        {
            // Find the asset
            DLCSharedAsset asset = GetAssetWithNameOrPath(nameOrPath, dlcUniqueKey, throwOnError);

            // Load the asset
            T result = asset.Load<T>();

            // Check for load error
            if(result == null)
            {
                // Check for throw
                if (throwOnError == true)
                    throw new Exception("Could not load asset with name or path: " + nameOrPath);

                return null;
            }

            // Get the result
            return result;
        }

        ///<summary>
        /// Attempts to load an asset with the specified name or path from any loaded DLC with all associated sub assets.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="LoadAssetWithSubAssetsAsync(string, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>An array of assets containing the main and sub assets</returns>
        public static Object[] LoadAssetWithSubAssets(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true)
        {
            return LoadAssetWithSubAssets<Object>(nameOrPath, dlcUniqueKey, throwOnError);
        }

        ///<summary>
        /// Attempts to load an asset with the specified name or path from any loaded DLC with all associated sub assets of the specified generic type.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="LoadAssetWithSubAssetsAsync{T}(string, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <typeparam name="T">The type of asset to return</typeparam>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>An array of loaded assets of the specified generic type</returns>
        public static T[] LoadAssetWithSubAssets<T>(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true) where T : Object
        {
            // Find the asset
            DLCSharedAsset asset = GetAssetWithNameOrPath(nameOrPath, dlcUniqueKey, throwOnError);

            // Load the asset
            T[] result = asset.LoadWithSubAssets<T>();

            // Check for load error
            if (result == null)
            {
                // Check for throw
                if (throwOnError == true)
                    throw new Exception("Could not load asset or sub assets with name or path: " + nameOrPath);

                return null;
            }

            // Get the result
            return result;
        }
        #endregion

        // ### Load Asset Async
        #region LoadAssetAsync
        ///<summary>
        /// Attempts to load an asset with the specified name or path from any loaded DLC asynchronously.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <returns>An awaitable object</returns>
        public static DLCAsync<Object> LoadAssetAsync(string nameOrPath, string dlcUniqueKey = null)
        {
            // Create async
            DLCAsync<Object> async = new DLCAsync<Object>();

            // Issue load request
            AsyncCommon.RunAsync(WaitForLoadAssetAsync(async, nameOrPath, dlcUniqueKey));
            return async;
        }

        ///<summary>
        /// Attempts to load an asset with the specified name or path from any loaded DLC asynchronously as the specified generic type.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns>An awaitable object</returns>
        public static DLCAsync<T> LoadAssetAsync<T>(string nameOrPath, string dlcUniqueKey = null) where T : Object
        {
            // Create async
            DLCAsync<T> async = new DLCAsync<T>();

            // Issue load request
            AsyncCommon.RunAsync(WaitForLoadAssetAsync(async, nameOrPath, dlcUniqueKey));
            return async;
        }

        ///<summary>
        /// Attempts to load an asset and sub-assets with the specified name or path from any loaded DLC asynchronously.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <returns>An awaitable object</returns>
        public static DLCAsync<Object[]> LoadAssetWithSubAssetsAsync(string nameOrPath, string dlcUniqueKey = null)
        {
            // Create async
            DLCAsync<Object[]> async = new DLCAsync<Object[]>();

            // Issue load request
            AsyncCommon.RunAsync(WaitForLoadAssetWithSubAssetsAsync(async, nameOrPath, dlcUniqueKey));
            return async;
        }

        ///<summary>
        /// Attempts to load an asset and sub-assets of the specifried generic type with the specified name or path from any loaded DLC asynchronously.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns>An awaitable object</returns>
        public static DLCAsync<T[]> LoadAssetWithSubAssetsAsync<T>(string nameOrPath, string dlcUniqueKey = null) where T : Object
        {
            // Create async
            DLCAsync<T[]> async = new DLCAsync<T[]>();

            // Issue load request
            AsyncCommon.RunAsync(WaitForLoadAssetWithSubAssetsAsync(async, nameOrPath, dlcUniqueKey));
            return async;
        }

        private static IEnumerator WaitForLoadAssetAsync<T>(DLCAsync<T> async, string nameOrPath, string dlcUniqueKey) where T : Object
        {
            // Wait for get DLC
            DLCAsync<DLCContent> dlcRequest = GetOrLoadDLCContentWithAssetAsync(nameOrPath, dlcUniqueKey);

            // Wait for load
            while(dlcRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(dlcRequest.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for loaded
            if(dlcRequest.IsSuccessful == false)
            {
                async.Error("Could not load asset because no DLC content containing such an asset could be located: " + nameOrPath);
                yield break;
            }

            // Find the asset
            DLCSharedAsset asset = dlcRequest.Result.SharedAssets.Find(nameOrPath);

            // Check for not found
            if(asset == null)
            {
                async.Error("Could not locate asset with the specified name or path in the associated DLC: " + nameOrPath + ", " + dlcRequest.Result.Metadata.NameInfo.UniqueKey + ": " + dlcRequest.Status);
                yield break;
            }

            // Load the asset
            DLCAsync<T> assetRequest = asset.LoadAsync<T>();

            // Wait for load
            while(assetRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(0.5f + (assetRequest.Progress * 0.5f));

                // Wait a frame
                yield return null;
            }

            // Check for success
            if(assetRequest.IsSuccessful == false)
            {
                async.Error("Could not load asset from target DLC: " + nameOrPath + ", " + dlcRequest.Result.NameInfo.UniqueKey + ": " + assetRequest.Status);
                yield break;
            }

            // Complete the operation
            async.Complete(true, assetRequest.Result);
        }

        private static IEnumerator WaitForLoadAssetWithSubAssetsAsync<T>(DLCAsync<T[]> async, string nameOrPath, string dlcUniqueKey) where T : Object
        {
            // Wait for get DLC
            DLCAsync<DLCContent> dlcRequest = GetOrLoadDLCContentWithAssetAsync(nameOrPath, dlcUniqueKey);

            // Wait for load
            while (dlcRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(dlcRequest.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for loaded
            if (dlcRequest.IsSuccessful == false)
            {
                async.Error("Could not load asset or sub assets because no DLC content containing such an asset could be located: " + nameOrPath);
                yield break;
            }

            // Find the asset
            DLCSharedAsset asset = dlcRequest.Result.SharedAssets.Find(nameOrPath);

            // Check for not found
            if (asset == null)
            {
                async.Error("Could not locate asset with the specified name or path in the associated DLC: " + nameOrPath + ", " + dlcRequest.Result.Metadata.NameInfo.UniqueKey + ": " + dlcRequest.Status);
                yield break;
            }

            // Load the asset
            DLCAsync<T[]> assetRequest = asset.LoadWithSubAssetsAsync<T>();

            // Wait for load
            while (assetRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(0.5f + (assetRequest.Progress * 0.5f));

                // Wait a frame
                yield return null;
            }

            // Check for success
            if (assetRequest.IsSuccessful == false)
            {
                async.Error("Could not load asset from target DLC: " + nameOrPath + ", " + dlcRequest.Result.NameInfo.UniqueKey + ": " + assetRequest.Status);
                yield break;
            }

            // Complete the operation
            async.Complete(true, assetRequest.Result);
        }

        private static DLCAsync<DLCContent> GetOrLoadDLCContentWithAssetAsync(string nameOrPath, string dlcUniqueKey)
        {
            // Check for dlc provided
            if(string.IsNullOrEmpty(dlcUniqueKey) == false)
            {
                // Try to get loaded
                DLCContent targetContent = GetLoadedDLC(dlcUniqueKey);

                // Check for loaded
                if (targetContent != null)
                {
                    // Complete operation
                    return DLCAsync<DLCContent>.Completed(true, targetContent);
                }

                // Request load the dlc
                return LoadDLCAsync(dlcUniqueKey);
            }

            // Create async
            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            // Issue request
            AsyncCommon.RunAsync(WaitForLoadDLCContentWithAssetAsync(async, nameOrPath));
            return async;
        }

        private static IEnumerator WaitForLoadDLCContentWithAssetAsync(DLCAsync<DLCContent> async, string nameOrPath)
        {
            // Get the manifest
            yield return ManifestAsync;

            // Try to get the dlc content from the manifest
            DLCManifest manifest = ManifestAsync.Result;

            // Find the associated manifest
            DLCManifestEntry targetManifestEntry = null;
            foreach(DLCManifestEntry manifestEntry in manifest.DLCContents)
            {
                // Check for asset found
                if(manifestEntry.HasAsset(nameOrPath) == true)
                {
                    targetManifestEntry = manifestEntry;
                    break;
                }
            }

            // Check for found
            if(targetManifestEntry == null)
            {
                async.Error("Could not determine the associated DLC containing the specified asset: " + nameOrPath);
                yield break;
            }

            // Check for loaded
            DLCContent targetContent = GetLoadedDLC(targetManifestEntry.DLCUniqueKey);

            // Complete if loaded
            if(targetContent != null)
            {
                async.Complete(true, targetContent);
                yield break;
            }

            // Load the DLC
            DLCAsync<DLCContent> dlcRequest = LoadDLCAsync(targetManifestEntry.DLCUniqueKey);

            // Wait for loaded
            while(dlcRequest.IsDone == false)
            {
                // Update progress and status
                async.UpdateProgress(dlcRequest.Progress);
                async.UpdateStatus(dlcRequest.Status);

                // Wait a frame
                yield return null;
            }

            // Complete operation
            async.Complete(dlcRequest.IsSuccessful, dlcRequest.Result);
        }
        #endregion

        // ### Load Scene
        #region LoadScene
        ///<summary>
        /// Attempts to load a scene with the specified name or path from any loaded DLC.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="LoadSceneAsync(string, LoadSceneMode, bool, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="loadSceneMode">The mode used to load the scene</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>The loaded asset</returns>
        public static bool LoadScene(string nameOrPath, LoadSceneMode loadSceneMode, string dlcUniqueKey = null, bool throwOnError = true)
        {
            // Find the asset
            DLCSceneAsset scene = GetSceneWithNameOrPath(nameOrPath, dlcUniqueKey, throwOnError);

            // Check for found
            if (scene != null)
            {
                // Start loading scene
                scene.Load(loadSceneMode);
                return true;
            }

            // Could not load
            return false;
        }

        ///<summary>
        /// Attempts to load a scene with the specified name or path from any loaded DLC.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="LoadSceneAsync(string, LoadSceneParameters, bool, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="loadSceneParameters">The parameters used to load the scene</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>Was the scene loaded</returns>
        public static bool LoadScene(string nameOrPath, LoadSceneParameters loadSceneParameters, string dlcUniqueKey = null, bool throwOnError = true)
        {
            // Find the asset
            DLCSceneAsset scene = GetSceneWithNameOrPath(nameOrPath, dlcUniqueKey, throwOnError);

            // Check for found
            if (scene != null)
            {
                // Start loading scene
                scene.Load(loadSceneParameters);
                return true;
            }

            // Could not load
            return false;
        }
        #endregion

        // ### Load Scene Async
        #region LoadSceneAsync
        ///<summary>
        /// Attempts to load a scene with the specified name or path from any loaded DLC asynchronously.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified scene name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="loadSceneMode">The mode used to load the scene</param>
        /// <param name="allowSceneActivation">Should the scene be activated as soon as it is loaded</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <returns>An awaitable object</returns>
        public static DLCAsync LoadSceneAsync(string nameOrPath, LoadSceneMode loadSceneMode, bool allowSceneActivation = true, string dlcUniqueKey = null)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Issue load request
            AsyncCommon.RunAsync(WaitForLoadSceneAsync(async, nameOrPath, dlcUniqueKey, new LoadSceneParameters(loadSceneMode), allowSceneActivation));
            return async;
        }


        ///<summary>
        /// Attempts to load a scene with the specified name or path from any loaded DLC asynchronously.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified scene name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="loadSceneParameters">The parameters used to load the scene</param>
        /// <param name="allowSceneActivation">Should the scene be activated as soon as it is loaded</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <returns>An awaitable object</returns>
        public static DLCAsync LoadSceneAsync(string nameOrPath, LoadSceneParameters loadSceneParameters, bool allowSceneActivation = true, string dlcUniqueKey = null)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Issue load request
            AsyncCommon.RunAsync(WaitForLoadSceneAsync(async, nameOrPath, dlcUniqueKey, loadSceneParameters, allowSceneActivation));
            return async;
        }

        private static IEnumerator WaitForLoadSceneAsync(DLCAsync async, string nameOrPath, string dlcUniqueKey, LoadSceneParameters loadSceneParameters, bool allowSceneActivation)
        {
            // Wait for get DLC
            DLCAsync<DLCContent> dlcRequest = GetOrLoadDLCContentWithSceneAsync(nameOrPath, dlcUniqueKey);

            // Wait for load
            while (dlcRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(dlcRequest.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for loaded
            if (dlcRequest.IsSuccessful == false)
            {
                async.Error("Could not load scene because no DLC content containing such an asset could be located: " + nameOrPath);
                yield break;
            }

            // Find the asset
            DLCSceneAsset scene = dlcRequest.Result.SceneAssets.Find(nameOrPath);

            // Check for not found
            if (scene == null)
            {
                async.Error("Could not locate scene with the specified name or path in the associated DLC: " + nameOrPath + ", " + dlcRequest.Result.Metadata.NameInfo.UniqueKey + ": " + dlcRequest.Status);
                yield break;
            }

            // Load the asset
            DLCAsync sceneRequest = scene.LoadAsync(loadSceneParameters, allowSceneActivation);

            // Wait for load
            while (sceneRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(0.5f + (sceneRequest.Progress * 0.5f));

                // Wait a frame
                yield return null;
            }

            // Check for success
            if (sceneRequest.IsSuccessful == false)
            {
                async.Error("Could not load scene from target DLC: " + nameOrPath + ", " + dlcRequest.Result.NameInfo.UniqueKey + ": " + sceneRequest.Status);
                yield break;
            }

            // Complete the operation
            async.Complete(true, sceneRequest.Result);
        }

        private static DLCAsync<DLCContent> GetOrLoadDLCContentWithSceneAsync(string nameOrPath, string dlcUniqueKey)
        {
            // Check for dlc provided
            if (string.IsNullOrEmpty(dlcUniqueKey) == false)
            {
                // Try to get loaded
                DLCContent targetContent = GetLoadedDLC(dlcUniqueKey);

                // Check for loaded
                if (targetContent != null)
                {
                    // Complete operation
                    return DLCAsync<DLCContent>.Completed(true, targetContent);
                }

                // Request load the dlc
                return LoadDLCAsync(dlcUniqueKey);
            }

            // Create async
            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            // Issue request
            AsyncCommon.RunAsync(WaitForLoadDLCContentWithSceneAsync(async, nameOrPath));
            return async;
        }

        private static IEnumerator WaitForLoadDLCContentWithSceneAsync(DLCAsync<DLCContent> async, string nameOrPath)
        {
            // Get the manifest
            yield return ManifestAsync;

            // Try to get the dlc content from the manifest
            DLCManifest manifest = ManifestAsync.Result;

            // Find the associated manifest
            DLCManifestEntry targetManifestEntry = null;
            foreach (DLCManifestEntry manifestEntry in manifest.DLCContents)
            {
                // Check for asset found
                if (manifestEntry.HasScene(nameOrPath) == true)
                {
                    targetManifestEntry = manifestEntry;
                    break;
                }
            }

            // Check for found
            if (targetManifestEntry == null)
            {
                async.Error("Could not determine the associated DLC containing the specified scene: " + nameOrPath);
                yield break;
            }

            // Check for loaded
            DLCContent targetContent = GetLoadedDLC(targetManifestEntry.DLCUniqueKey);

            // Complete if loaded
            if (targetContent != null)
            {
                async.Complete(true, targetContent);
                yield break;
            }

            // Load the DLC
            DLCAsync<DLCContent> dlcRequest = LoadDLCAsync(targetManifestEntry.DLCUniqueKey);

            // Wait for loaded
            while (dlcRequest.IsDone == false)
            {
                // Update progress and status
                async.UpdateProgress(dlcRequest.Progress);
                async.UpdateStatus(dlcRequest.Status);

                // Wait a frame
                yield return null;
            }

            // Complete operation
            async.Complete(dlcRequest.IsSuccessful, dlcRequest.Result);
        }
        #endregion



        // ### Find asset
        #region FindAsset
        ///<summary>
        /// Attempts to find an asset with the specified name or path from any loaded DLC.
        /// You must ensure that the containing DLC is loaded before hand, or alternatively you can use the async version <see cref="FindAssetAsync(string, string)"/> which can load the DLC on demand in a single call.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>The loaded asset</returns>
        public static DLCSharedAsset FindAsset(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true)
        {
            // Find the asset
            DLCSharedAsset asset = GetAssetWithNameOrPath(nameOrPath, dlcUniqueKey, throwOnError);

            // Check for not found
            if(asset == null)
            {
                // Check for throw
                if(throwOnError == true)
                    throw new Exception("Could not locate asset with name or path: " + nameOrPath);
            }
            return asset;
        }

        private static DLCSharedAsset GetAssetWithNameOrPath(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true)
        {
            // Find the asset
            DLCSharedAsset asset = null;

            if (string.IsNullOrEmpty(dlcUniqueKey) == false)
            {
                // Try to find the target dlc
                DLCContent targetDLC = LoadedDLCContents.FirstOrDefault(d => d.Metadata.NameInfo.UniqueKey == dlcUniqueKey);

                // Check for not found
                if (targetDLC == null)
                {
                    // Check for throw
                    if (throwOnError == true)
                        throw new DLCNotLoadedException("Could not find loaded DLC with the unique key, and the DLC will not be loaded on demand in non-async calls: " + dlcUniqueKey);

                    return null;
                }

                // Try to find the asset
                asset = targetDLC.SharedAssets.Find(nameOrPath);
            }
            else
            {
                // Try to find from all loaded
                allSharedAssetsCollection.Find(nameOrPath);
            }

            // Check for found
            if (asset == null)
            {
                // Check for throw
                if (throwOnError == true)
                    throw new Exception("Could not find asset from loaded DLC contents with the specified name or path: " + nameOrPath);

                return null;
            }
            return asset;
        }
        #endregion

        // ### Find Asset Async
        #region FindAssetAsync
        ///<summary>
        /// Attempts to find an asset with the specified name or path from any loaded DLC or DLC known at build time.
        /// If there is no loaded DLC containing the specified asset, then this call will attempt to load the DLC on demand if the source DLC can be determined from the name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain assets with the same name or relative paths</param>
        /// <returns>An awaitable object containing the found asset</returns>
        public static DLCAsync<DLCSharedAsset> FindAssetAsync(string nameOrPath, string dlcUniqueKey = null)
        {
            // Create async
            DLCAsync<DLCSharedAsset> async = new DLCAsync<DLCSharedAsset>();
            
            // Run async
            AsyncCommon.RunAsync(WaitForFindAssetAsync(async, nameOrPath, dlcUniqueKey));
            return async;
        }

        private static IEnumerator WaitForFindAssetAsync(DLCAsync<DLCSharedAsset> async, string nameOrPath, string dlcUniqueKey)
        {
            // Wait for get DLC
            DLCAsync<DLCContent> dlcRequest = GetOrLoadDLCContentWithAssetAsync(nameOrPath, dlcUniqueKey);

            // Wait for load
            while (dlcRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(dlcRequest.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for loaded
            if (dlcRequest.IsSuccessful == false)
            {
                async.Error("Could not load asset because no DLC content containing such an asset could be located: " + nameOrPath);
                yield break;
            }

            // Find the asset
            DLCSharedAsset asset = dlcRequest.Result.SharedAssets.Find(nameOrPath);

            // Check for not found
            if (asset == null)
            {
                async.Error("Could not locate asset with the specified name or path in the associated DLC: " + nameOrPath + ", " + dlcRequest.Result.Metadata.NameInfo.UniqueKey + ": " + dlcRequest.Status);
                yield break;
            }

            // Complete the operation
            async.Complete(true, asset);
        }
        #endregion

        // ### Find Scene
        #region FindScene
        ///<summary>
        /// Attempts to find a scene with the specified name or path from any loaded DLC or DLC known at build time.
        /// If there is no loaded DLC containing the specified scene, then this call will attempt to load the DLC on demand if the source DLC can be determined from the name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain scenes with the same name or relative paths</param>
        /// <param name="throwOnError">Will throw an exception on error if enabled, or simply return null if disabled</param>
        /// <returns>An awaitable object containing the found asset</returns>
        public static DLCSceneAsset FindScene(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true)
        {
            // Find the scene
            DLCSceneAsset scene = GetSceneWithNameOrPath(nameOrPath, dlcUniqueKey, throwOnError);

            // Check for not found
            if (scene == null)
            {
                // Check for throw
                if (throwOnError == true)
                    throw new Exception("Could not locate scene with name or path: " + nameOrPath);
            }
            return scene;
        }

        private static DLCSceneAsset GetSceneWithNameOrPath(string nameOrPath, string dlcUniqueKey = null, bool throwOnError = true)
        {
            // Find the asset
            DLCSceneAsset scene = null;

            if (string.IsNullOrEmpty(dlcUniqueKey) == false)
            {
                // Try to find the target dlc
                DLCContent targetDLC = LoadedDLCContents.FirstOrDefault(d => d.Metadata.NameInfo.UniqueKey == dlcUniqueKey);

                // Check for not found
                if (targetDLC == null)
                {
                    // Check for throw
                    if (throwOnError == true)
                        throw new DLCNotLoadedException("Could not find loaded DLC with the unique key, and the DLC will not be loaded on demand in non-async calls: " + dlcUniqueKey);

                    return null;
                }

                // Try to find the asset
                scene = targetDLC.SceneAssets.Find(nameOrPath);
            }
            else
            {
                // Try to find from all loaded
                allSharedAssetsCollection.Find(nameOrPath);
            }

            // Check for found
            if (scene == null)
            {
                // Check for throw
                if (throwOnError == true)
                    throw new Exception("Could not find scene from loaded DLC contents with the specified name or path: " + nameOrPath);

                return null;
            }
            return scene;
        }
        #endregion

        // ### Find Scene Async
        #region FindSceneAsync
        ///<summary>
        /// Attempts to find a scene with the specified name or path from any loaded DLC or DLC known at build time.
        /// This call will attempt to load the associated DLC as part of the same call if it is not already loaded into memory, and the target DLC can be determined from the specified scene name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to load</param>
        /// <param name="dlcUniqueKey">The optional unique key of the DLC to search, useful if multiple DLC's may contain scenes with the same name or relative paths</param>
        /// <returns>An awaitable object containing the found asset</returns>
        public static DLCAsync<DLCSceneAsset> FindSceneAsync(string nameOrPath, string dlcUniqueKey = null)
        {
            // Create async
            DLCAsync<DLCSceneAsset> async = new DLCAsync<DLCSceneAsset>();

            // Run async
            AsyncCommon.RunAsync(WaitForFindSceneAsync(async, nameOrPath, dlcUniqueKey));
            return async;
        }

        private static IEnumerator WaitForFindSceneAsync(DLCAsync<DLCSceneAsset> async, string nameOrPath, string dlcUniqueKey)
        {
            // Wait for get DLC
            DLCAsync<DLCContent> dlcRequest = GetOrLoadDLCContentWithSceneAsync(nameOrPath, dlcUniqueKey);

            // Wait for load
            while (dlcRequest.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(dlcRequest.Progress * 0.5f);

                // Wait a frame
                yield return null;
            }

            // Check for loaded
            if (dlcRequest.IsSuccessful == false)
            {
                async.Error("Could not load scene because no DLC content containing such an asset could be located: " + nameOrPath);
                yield break;
            }

            // Find the asset
            DLCSceneAsset scene = dlcRequest.Result.SceneAssets.Find(nameOrPath);

            // Check for not found
            if (scene == null)
            {
                async.Error("Could not locate scene with the specified name or path in the associated DLC: " + nameOrPath + ", " + dlcRequest.Result.Metadata.NameInfo.UniqueKey + ": " + dlcRequest.Status);
                yield break;
            }

            // Complete the operation
            async.Complete(true, scene);
        }
        #endregion



        // ### Unload
        #region UnloadAll
        /// <summary>
        /// Attempt to unload all the current loaded DLC contents.
        /// </summary>
        /// <param name="withAssets">Should asset instances also be unloaded</param>
        public static void UnloadAllDLC(bool withAssets = true)
        {
            // Pass in loaded dlc contents - ensure that a copy is passed using ToList otherwise a collection modified error will occur during unload
            UnloadDLCBatch(LoadedDLCContents.ToList(), withAssets);
        }

        /// <summary>
        /// Attempt to unload all the current loaded DLC contents asynchronously.
        /// </summary>
        /// <param name="withAssets">Should asset instances also be unloaded</param>
        /// <returns>A <see cref="DLCBatchAsync"/> object that can be awaited and provided additional information about the state of the batch operation</returns>
        public static DLCBatchAsync UnloadAllDLCAsync(bool withAssets = true)
        {
            // Pass in loaded dlc contents - ensure that a copy is passed using ToList otherwise a collection modified error will occur during unload
            return UnloadDLCBatchAsync(LoadedDLCContents.ToList(), withAssets);
        }
        #endregion

        #region UnloadBatch
        /// <summary>
        /// Attempt to unload all the specified DLC contents.
        /// Each DLC will only be unloaded if it is currently fully loaded into memory.
        /// Loading or idle contents will simply be ignored.
        /// </summary>
        /// <param name="dlcContents">An enumerable of <see cref="DLCContent"/> that should be unloaded</param>
        /// <param name="withAssets">Should asset instances also be unloaded</param>
        /// <exception cref="ArgumentNullException"><paramref name="dlcContents"/> is null or empty</exception>
        public static void UnloadDLCBatch(IEnumerable<DLCContent> dlcContents, bool withAssets = true)
        {
            // Check for null
            if (dlcContents == null)
                throw new ArgumentNullException(nameof(DLCContent));

            foreach (DLCContent content in dlcContents)
            {
                if (content != null)
                    content.Unload(withAssets);
            }
        }

        /// <summary>
        /// Attempt to unload all the specified DLC contents asynchronously.
        /// Each DLC will only be unloaded if it is currently fully loaded into memory.
        /// Loading or idle contents will simply be ignored.
        /// </summary>
        /// <param name="dlcContents">An enumerable of <see cref="DLCContent"/> that should be unloaded</param>
        /// <param name="withAssets">Should asset instances also be unloaded</param>
        /// <returns>A <see cref="DLCBatchAsync"/> object that can be awaited and provided additional information about the state of the batch operation</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dlcContents"/> is null or empty</exception>
        public static DLCBatchAsync UnloadDLCBatchAsync(IEnumerable<DLCContent> dlcContents, bool withAssets = true)
        {
            // Check for null
            if(dlcContents == null)
                throw new ArgumentNullException(nameof(DLCContent));

            // Create requests collection
            List<DLCAsync> requests = new List<DLCAsync>();

            // Request unload all
            foreach (DLCContent content in dlcContents)
            {
                // Check for null
                if (content == null)
                    continue;

                DLCAsync async = null;

                try
                {
                    // Handle exceptions
                    async = content.UnloadAsync(withAssets);
                }
                catch (Exception e)
                {
                    async = DLCAsync.Error(e.Message);
                }

                requests.Add(async);
            }

            // Create batch operation
            DLCBatchAsync asyncBatch = new DLCBatchAsync(requests);

            // Run async to ensure that batch progress and status are updated
            AsyncCommon.RunAsync(asyncBatch.UpdateTasksRoutine());
            return asyncBatch;
        }
        #endregion

        /// <summary>
        /// Attempt to fetch the unique key for the DLC with the specified name and optional version.
        /// This method is slow (and available as async only) since all available DLC need to be evaluated at the metadata level in order to find a matching name and version.
        /// The available DLC that will be scanned is determined from the active DRM provider via <see cref="FetchRemoteDLCUniqueKeys"/>.
        /// </summary>
        /// <param name="dlcName">The name of the dlc to find the unique key for</param>
        /// <param name="version">An optional version if a specific version of the DLC is required</param>
        /// <param name="installOnDemand">Should the DLC be installed on demand if it is not available locally</param>
        /// <returns>A <see cref="DLCAsync"/> operation that can be awaited and provides access to the unique key of the installed DLC with the specified name and optional version info if a match is found, or a null string if te DLC could not be located</returns>
        /// <exception cref="NotSupportedException">There is no suitable DRM provider for this platform</exception>
        /// <exception cref="DirectoryNotFoundException">Part of the file path could not be found</exception>
        /// <exception cref="FileNotFoundException">The specified file path does not exist</exception>
        /// <exception cref="FormatException">The specified file is not a valid DLC format</exception>
        /// <exception cref="InvalidDataException">The specified file has been signed by another game or version - The DLC does not belong to this game</exception>
        /// <exception cref="InvalidOperationException">The DLC file is missing required data or is possibly corrupt</exception>
        public static DLCAsync<string> GetDLCUniqueKeyAsync(string dlcName, Version version = null, bool installOnDemand = false)
        {
            // Create async
            DLCAsync<string> async = new DLCAsync<string>();
            AsyncCommon.RunAsync(WaitForGetDLCUniqueKey(async, dlcName, version, installOnDemand));

            return async;
         }

        private static IEnumerator WaitForGetDLCUniqueKey(DLCAsync<string> async, string dlcName, Version version, bool installOnDemand)
        {
            // Get remote keys
            DLCAsync<string[]> remoteKeysAsync = FetchRemoteDLCUniqueKeys();

            // Wait for completed
            yield return remoteKeysAsync;

            // Check for error
            if(remoteKeysAsync.IsSuccessful == false)
            {
                async.Error(remoteKeysAsync.Status);
                yield break;
            }

            // Check all available keys
            foreach(string remoteKey in remoteKeysAsync.Result)
            {
                // We need to request load the metadata to determine if metadata is equal
                DLCAsync<IDLCMetadata> metadataAsync = LoadDLCMetadataAsync(remoteKey, installOnDemand);

                // Wait for completed;
                yield return metadataAsync;

                // Check for matching name
                if(metadataAsync.IsSuccessful == true
                && metadataAsync.Result.NameInfo.Name == dlcName 
                    && (version == null || metadataAsync.Result.NameInfo.Version.CompareTo(version) == 0))
                {
                    // We have found the unique key for the dlc
                    async.Complete(true, remoteKey);
                    yield break;
                }
            }

            // Failure
            async.Error("Could not find installed DLC with the specified name and version info specified. The DLC may not be installed or may be of a different version if specified");
        }

        /// <summary>
        /// Check if the specified file path is a valid DLC file format.
        /// This is intended to be a very quick check and will only load a few bytes from the source file in order to determine validity.
        /// </summary>
        /// <param name="path">The path of the file to check</param>
        /// <returns>True if the file is a valid DLC format which can be loaded, or false if not</returns>
        public static bool IsDLCFile(string path)
        {
            // Check platform
#if UNITY_WEBGL
            throw new NotSupportedException("Not supported on this platform");
#else

            // Quick load of header only
            DLCContent temp = TempContent;
            try
            {
                temp.LoadDLCContent(DLCStreamProvider.FromFile(path), DLCLoadMode.Header);
            }
            catch
            {
                return false;
            }
            finally
            {
                temp.Dispose();
            }
            return true;
#endif
        }

        /// <summary>
        /// Register a custom <see cref="IDRMServiceProvider"/> which is responsible for providing the correct DRM management for the current build configuration.
        /// </summary>
        /// <param name="serviceProvider">The custom service provider or null, in which case the default DRM service provider will be used</param>
        public static void RegisterDRMServiceProvider(IDRMServiceProvider serviceProvider)
        {
            // Check for null
            if (serviceProvider == null)
                serviceProvider = new DefaultDRMServiceProvider();

            drmServiceProvider = serviceProvider;
            drmProvider = null;
        }

        /// <summary>
        /// Register a custom <see cref="IDRMProvider"/> which is responsible for DRM management for the current build configuration.
        /// </summary>
        /// <param name="drmProvider">The DRM provider to register</param>
        /// <exception cref="ArgumentNullException">DRM provider is null</exception>
        public static void RegisterDRMProvider(IDRMProvider drmProvider)
        {
            // Check for null
            if (drmProvider == null)
                throw new ArgumentNullException(nameof(drmProvider));

            // Create service wrapper
            drmServiceProvider = new DirectDRMServiceProvider(drmProvider);
            drmProvider = null;
        }

        internal static void RegisterDRMEditorTestMode(IDRMProvider drmProvider)
        {
            // Register drm
            RegisterDRMProvider(drmProvider);

            // Set test mode
            isTestMode = true;

            // Report an informative message
            UnityEngine.Debug.Log("[Ultimate DLC Toolkit]: Editor Test Mode is enabled! You will be able to access any DLC that has been built for the current platform. To disable Test Mode: `Tools->DLC Toolkit->Test Mode->Disabled`");
        }

        internal static void RegisterEditorManifest(DLCManifest newManifest)
        {
            cachedManifestRequest = DLCAsync<DLCManifest>.Completed(true, newManifest);
        }

        /// <summary>
        /// Check that the provided hashes match the hashes that were used tby the game.
        /// Ensures that the DLC content belongs to this game unless spoofed by user.
        /// </summary>
        /// <param name="productHash">The signed DLC product hash</param>
        /// <param name="versionHash">The optional signed DLC version hash</param>
        /// <exception cref="InvalidDataException">The DLC belongs to a different game or version</exception>
        internal static void CheckSignedDLC(byte[] productHash, byte[] versionHash = null)
        {
            // Check product hash - SHA256 uses 32 bytes
            if(productHash == null || productHash.Length != 32 ||
                Config.ProductHash.SequenceEqual(productHash) == false)
                throw new InvalidDataException("Cannot load signed DLC because it was signed by a different game");

            // Check version hash - SHA256 uses 32 bytes
            if(versionHash != null)
            {
                // Version signing is optional
                if(versionHash.Length != 32 || 
                    Config.VersionHash.SequenceEqual(versionHash) == false)
                    throw new InvalidDataException("Cannot load signed DLC because the game version does not match the signed DLC version");
            }
        }

        private static IEnumerator WaitForCompleted(DLCAsync async, Action finished)
        {
            // Wait for operation
            yield return async;

            // Trigger event
            finished();
        }
    }
}
