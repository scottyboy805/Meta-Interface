using DLCToolkit.Assets;
using DLCToolkit.DRM;
using DLCToolkit.Format;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using System.Security.Cryptography;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLCToolkit
{
    /// <summary>
    /// The requested load amount when loading DLC content.
    /// </summary>
    internal enum DLCLoadMode
    {        
        /// <summary>
        /// Only fetch the minimum header data.
        /// Only useful for checking if a file is a valid DLC format.
        /// </summary>
        Header = 1,
        /// <summary>
        /// Only fetch the standard metadata including icons.
        /// </summary>
        Metadata = 2,
        /// <summary>
        /// Fetch the standard metadata, icons and also metadata for the shared assets and scenes that are included.
        /// Allows for listing all included contents but does not support loading the actual assets.
        /// </summary>
        MetadataWithAssets = 3,
        /// <summary>
        /// Fully load the DLC content and preload content based on the DLC load options specified in the DLC metadata.
        /// </summary>
        Full = 4,
    }

    /// <summary>
    /// The current state of DLC loadable content.
    /// </summary>
    internal enum DLCLoadState
    {
        /// <summary>
        /// The content is not currently loaded and no attempt has been made to load it yet.
        /// </summary>
        NotLoaded = 0,
        /// <summary>
        /// The content is currently being loaded.
        /// </summary>
        Loading,
        /// <summary>
        /// The content was loaded successfully.
        /// </summary>
        Loaded,
        /// <summary>
        /// The content could not be loaded due to some error.
        /// </summary>
        FailedToLoad,
    }

    /// <summary>
    /// Represents a DLC loaded in memory.
    /// Provides access to metadata, icons, plus API's for loading assets and scenes.
    /// Will remain in memory until <see cref="Unload(bool)"/>, <see cref="Dispose"/> or until the game exits.
    /// </summary>
    public sealed class DLCContent : MonoBehaviour, IDLCAsyncProvider, IDisposable
    {
        // Events
        /// <summary>
        /// Called when the DLC content is just about to be unloaded.
        /// Usually this means an unload request was made of the content was disposed.
        /// </summary>
        [HideInInspector]
        public UnityEvent OnWillUnload = new UnityEvent();
        /// <summary>
        /// Called when the DLC content has finished unloading.
        /// Usually this means an unload request was made of the content was disposed.
        /// </summary>
        [HideInInspector]
        public UnityEvent OnUnloaded = new UnityEvent();

        // Private
        private static readonly string defaultGameObjectName = "DLCContent";
        private static readonly Queue<DLCContent> availableContents = new Queue<DLCContent>();
        private static readonly List<DLCContent> allContents = new List<DLCContent>();

        private string hostName = null;
        private DLCLoadMode loadMode = DLCLoadMode.Full;
        private DLCLoadState loadState = DLCLoadState.NotLoaded;
        private IDRMProvider drmProvider = null;
        private DLCAsync<DLCContent> loadAsync = null;        
        private DLCStreamProvider streamProvider = null;

        private DLCSharedAssetCollection sharedAssetsCollection = DLCSharedAssetCollection.Empty();
        private DLCSceneAssetCollection sceneAssetsCollection = DLCSceneAssetCollection.Empty();

        // Internal
        internal DLCBundle bundle = null;
        internal DLCMetadata metadata = null;
        internal DLCIconSet iconSet = null;
        internal DLCScriptAssembly scriptAssembly = null;
        internal DLCContentBundle sharedAssetsBundle = null;
        internal DLCContentBundle sceneAssetsBundle = null;
        internal bool willDestroyInstantiatedContent = false;

        // Properties
        internal static IReadOnlyList<DLCContent> AllContents
        {
            get { return allContents; }
        }

        internal DLCAsync<DLCContent> LoadAsync
        {
            get { return loadAsync; }
        }

        internal DLCLoadMode LoadMode
        {
            get { return loadMode; }
        }

        /// <summary>
        /// Check if the DLC content has been successfully loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return loadState == DLCLoadState.Loaded; }
        }

        /// <summary>
        /// Check if the DLC content is currently loading.
        /// </summary>        
        public bool IsLoading
        {
            get { return loadState == DLCLoadState.Loading; }
        }

        /// <summary>
        /// Get the local path where the DLC content was loaded from.
        /// </summary>
        /// <exception cref="DLCNotLoadedException">The DLC is not currently loaded</exception>
        public string HintLoadPath
        {
            get 
            {
                CheckLoaded();
                return streamProvider.HintPath; 
            }
        }

        /// <summary>
        /// Get the <see cref="IDLCNameInfo"/> for the loaded DLC content.
        /// Provides access to useful identifying data such as name, version and unique key.
        /// </summary>
        /// <exception cref="DLCNotLoadedException">The DLC is not currently loaded</exception>
        public IDLCNameInfo NameInfo
        {
            get 
            {
                CheckLoaded();
                return metadata.NameInfo; 
            }
        }

        /// <summary>
        /// Get the <see cref="IDLCMetadata"/> for the loaded DLC content.
        /// Provides access to useful metadata such as name, version, author information and more.
        /// </summary>
        /// <exception cref="DLCNotLoadedException">The DLC is not currently loaded</exception>
        public IDLCMetadata Metadata
        {
            get 
            {
                CheckLoadedWithMode(DLCLoadMode.Metadata);
                return metadata;
            }
        }

        /// <summary>
        /// Get the <see cref="IDLCIconProvider"/> for the loaded DLC content.
        /// Provides access to all icons associated with the DLC if available.
        /// </summary>
        /// <exception cref="DLCNotLoadedException">The DLC is not currently loaded</exception>
        public IDLCIconProvider IconProvider
        {
            get 
            {
                CheckLoadedWithMode(DLCLoadMode.Metadata);
                return iconSet; 
            }
        }

        /// <summary>
        /// Get a collection <see cref="DLCSharedAsset"/> for the loaded DLC content listing each asset that was included.
        /// Shared assets are all non-scene assets types such as prefab, texture, material, audio clip, etc.
        /// Provides access to useful asset metadata such as name, path, extension, type and more.
        /// </summary>
        /// <exception cref="DLCNotLoadedException">The DLC is not currently loaded or is loaded in metadata only mode</exception>
        public DLCSharedAssetCollection SharedAssets
        {
            get 
            {
                CheckLoadedWithMode(DLCLoadMode.MetadataWithAssets);
                return sharedAssetsCollection;
            }
        }

        /// <summary>
        /// Get a collection <see cref="DLCSceneAsset"/> for the loaded DLC content listing each scene asset that was included.
        /// Scene assets are simply Unity scenes.
        /// Provides access to useful asset metadata such as name, path, extension, type and more.
        /// </summary>
        /// <exception cref="DLCNotLoadedException">The DLC is not currently loaded or is loaded in metadata only mode</exception>
        public DLCSceneAssetCollection SceneAssets
        {
            get 
            {
                CheckLoadedWithMode(DLCLoadMode.MetadataWithAssets);
                return sceneAssetsCollection; 
            }
        }

        // Methods
#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        private static void OnEnterPlayMode(EnterPlayModeOptions options)
        {
            // Support enter play mode options
            availableContents.Clear();
            allContents.Clear();
        }
#endif

        /// <summary>
        /// Override implementation of ToString.
        /// </summary>
        /// <returns>Get the string representation of the DLCContent</returns>
        public override string ToString()
        {
            if (loadState == DLCLoadState.Loaded)
            {
                // Get extra hint
                string extra = loadMode switch
                { 
                    DLCLoadMode.Header => "(Header Only)",
                    DLCLoadMode.Metadata => "(Metadata Only)",
                    DLCLoadMode.MetadataWithAssets => "(Metadata With Assets)",
                    _ => string.Empty,
                };

                return string.Format("{0}<{1}, {2}>{3}", nameof(DLCContent), NameInfo.Name, NameInfo.Version, extra);
            }

            return base.ToString();
        }

        private void Awake()
        {
            hostName = gameObject.name;
        }

        private void OnDestroy()
        {
            // Release resources
            Unload(true);
        }

        internal bool IsLoadPath(string path)
        {
            // Normalize the paths
            try
            {
                string hintPath = Path.GetFullPath(HintLoadPath);
                string queryPath = Path.GetFullPath(path);

                // Check for equality
                return string.Compare(hintPath, queryPath, true) == 0;
            }
            catch { }
            return false;
        }

        internal void LoadDLCContent(DLCStreamProvider streamProvider, DLCLoadMode loadMode)
        {
            Debug.Log("Load DLC content: " + streamProvider.HintPath);
            this.streamProvider = streamProvider;
            this.loadMode = loadMode;
            this.loadState = DLCLoadState.Loading;

            // Add content
            allContents.Add(this);

            // Create the bundle            
            bundle = new DLCBundle(streamProvider);
            bundle.ReadBundleContents(loadMode > DLCLoadMode.Header);

            // Check for completed - header only request
            if (loadMode <= DLCLoadMode.Header)
            {
                Debug.Log("Partial DLC load completed - Header content");
                return;
            }

            // Request metadata
            Debug.Log("Fetching metadata...");
            metadata = new DLCMetadata();
            bundle.RequestLoad(DLCBundle.ContentType.Metadata, metadata);

            // Check for completed - metadata only request
            if (loadMode <= DLCLoadMode.Metadata)
            {
                Debug.Log("Partial DLC load completed - Metadata content");
                SetLoadState(DLCLoadState.Loaded);
                return;
            }

            // Check for unity version
            if (metadata.UnityVersion != Application.unityVersion)
                throw new NotSupportedException("The DLC content was built with a different Unity version");

            // Check for toolkit version
            if (DLC.ToolkitVersion.CompareTo(metadata.ToolkitVersion) > 0)
                throw new NotSupportedException("The DLC content was created with a newer version of DLC Toolkit");

            // Request icon set
            Debug.Log("Fetching icons...");
            iconSet = new DLCIconSet(this);
            bundle.RequestLoad(DLCBundle.ContentType.IconSet, iconSet);


            // ### Load section - scripts
            if ((bundle.Flags & DLCBundle.ContentFlags.ScriptAssets) != 0)
            {
                Debug.Log("Loading script assemblies...");

                // Check for allowed
                if (DLC.IsScriptingSupported == false)
                {
                    Debug.LogWarning("DLC content includes script assemblies but scripting is not supported on this platform/runtime. Ensure that scripting is only enabled for Desktop platforms using the Mono backend");
                    Debug.LogWarning("DLC content will be loaded but may include missing scripts!");
                }
                else
                {
                    // Load our scripts
                    scriptAssembly = new DLCScriptAssembly();
                    bundle.RequestLoad(DLCBundle.ContentType.ScriptAssembly, scriptAssembly);

                    // Activate our script assemblies
                    scriptAssembly.LoadScriptAssemblyImages();
                }
            }


            // Check for load bundles
            bool loadBundles = loadMode == DLCLoadMode.Full;

            // Shared assets
            if ((bundle.Flags & DLCBundle.ContentFlags.SharedAssets) != 0)
            {
                Debug.Log("DLC content includes shared assets...");

                // Create our shared bundle
                if (loadBundles == true)
                {
                    Debug.Log("Prepare to load shared assets bundle...");
                    sharedAssetsBundle = new DLCContentBundle(bundle.GetContentStream(DLCBundle.ContentType.SharedAssetBundle));
                }

                // Load our asset metadata
                Debug.Log("Load shared assets metadata...");
                DLCContentBundleMetadata sharedAssetsMetadata = new DLCContentBundleMetadata();
                bundle.RequestLoad(DLCBundle.ContentType.SharedAssetMetadata, sharedAssetsMetadata);

                // Check for full load
                if (loadBundles == true)
                {
                    // Extract our shared asset table
                    sharedAssetsCollection = sharedAssetsMetadata.ExtractSharedAssets(this, sharedAssetsBundle, loadMode);

                    // Add to dlc
                    DLC.allSharedAssets.AddRange(sharedAssetsCollection.EnumerateAll());

                    // Preload if option is set - otherwise lazy load on demand
                    if ((bundle.Flags & DLCBundle.ContentFlags.PreloadSharedBundle) != 0)
                    {
                        Debug.Log("Preloading shared assets bundle...");
                        sharedAssetsBundle.RequestLoad();
                    }
                    else
                        Debug.Log("Shared assets bundle is not marked for preloading and will be loaded on demand...");
                }
            }

            // Scene assets
            if((bundle.Flags & DLCBundle.ContentFlags.SceneAssets) != 0)
            {
                Debug.Log("DLC content includes scene assets...");

                // Create our scene bundle
                if (loadBundles == true)
                {
                    Debug.Log("Prepare to load scene assets bundle...");
                    sceneAssetsBundle = new DLCContentBundle(bundle.GetContentStream(DLCBundle.ContentType.SceneAssetBundle));
                }

                // Load our scene metadata
                Debug.Log("Load scene assets metadata...");
                DLCContentBundleMetadata sceneAssetsMetadata = new DLCContentBundleMetadata();
                bundle.RequestLoad(DLCBundle.ContentType.SceneAssetMetadata, sceneAssetsMetadata);

                // Check for full load
                if (loadBundles == true)
                {
                    // Extract our scene asset table
                    sceneAssetsCollection = sceneAssetsMetadata.ExtractSceneAssets(this, sceneAssetsBundle, loadMode);

                    // Add to dlc
                    DLC.allSceneAssets.AddRange(sceneAssetsCollection.EnumerateAll());

                    // Preload if option is set - otherwise lazy load on demand
                    if ((bundle.Flags & DLCBundle.ContentFlags.PreloadSceneBundle) != 0)
                    {
                        Debug.Log("Preloading scene assets bundle...");
                        sceneAssetsBundle.RequestLoad();
                    }
                    else
                        Debug.Log("Scene assets bundle is not marked for preloading and will be loaded on demand...");
                }
            }


            // Set load state
            Debug.Log("DLC was loaded successfully: " + metadata.NameInfo.Name);
            SetLoadState(DLCLoadState.Loaded);

            // Track in use
            if (drmProvider != null && Application.isPlaying == true)
            {
                try
                { 
                    drmProvider.TrackDLCUsage(metadata.NameInfo.UniqueKey, true);
                }
                catch (NotSupportedException) { } // The API is optional and can be discarded using a NotSupportedException
                catch (Exception e)
                {
                    Debug.LogError("Unhandled exception thrown by DRM call: " + e);
                }
            }
        }

        internal void LoadDLCContentAsync(DLCAsync<DLCContent> async, DLCStreamProvider streamProvider, DLCLoadMode loadMode)
        {
            // Check for header only mode
            if (loadMode == DLCLoadMode.Header)
                throw new NotSupportedException("Header only mode is not supported for async loading");

            Debug.Log("Load DLC content async: " + streamProvider.HintPath);
            this.streamProvider = streamProvider;
            this.loadMode = loadMode;
            this.loadState = DLCLoadState.Loading;

            // Add content
            allContents.Add(this);

            // Create the bundle
            bundle = new DLCBundle(streamProvider);
            bundle.ReadBundleContents(loadMode > DLCLoadMode.Header);

            // Run the main async load operation
            ((IDLCAsyncProvider)this).RunAsync(LoadDLCContentAsyncRoutine(async, loadMode));
        }

        private IEnumerator LoadDLCContentAsyncRoutine(DLCAsync<DLCContent> async, DLCLoadMode loadMode)
        {
            this.loadAsync = async;

            // ### Load section - metadata
            // Request metadata
            Debug.Log("Fetching metadata...");
            metadata = new DLCMetadata();
            DLCAsync metadataAsync = bundle.RequestLoadAsync(this, DLCBundle.ContentType.Metadata, metadata);

            // Update status
            async.UpdateStatus("Loading metadata");

            // Wait for loaded
            while(metadataAsync.IsDone == false)
            {
                if (loadMode == DLCLoadMode.Metadata)
                {
                    // Use absolute progress
                    async.UpdateProgress(metadataAsync.Progress);
                }
                else
                {
                    // Update progress
                    async.UpdateProgress(Mathf.Lerp(0f, 0.05f, metadataAsync.Progress));
                }

                // Wait a frame
                yield return null;
            }

            // Check for unity version
            if (metadata.UnityVersion != Application.unityVersion)
            {
                async.Error("The DLC content was built with a different Unity version");
                loadAsync = null;
                SetLoadState(DLCLoadState.NotLoaded);
                yield break;
            }

            // Check for toolkit version
            if (DLC.ToolkitVersion.CompareTo(metadata.ToolkitVersion) > 0)
            {
                async.Error("The DLC content was created with a newer version of DLC Toolkit");
                loadAsync = null;
                SetLoadState(DLCLoadState.NotLoaded);
                yield break;
            }

            // Check for metadata only
            if (loadMode == DLCLoadMode.Metadata)
            {
                Debug.Log("Partial DLC load completed - Metadata content");
                async.Complete(true, this);
                loadAsync = null;
                SetLoadState(DLCLoadState.Loaded);
                yield break;
            }


            // ### Load section - icon set
            // Request icons
            Debug.Log("Fetching icons...");
            iconSet = new DLCIconSet(this);
            DLCAsync iconAsync = bundle.RequestLoadAsync(this, DLCBundle.ContentType.IconSet, iconSet);

            // Update status
            async.UpdateStatus("Loading icons");

            // Wait for loaded
            while(iconAsync.IsDone == false)
            {
                // Update progress
                async.UpdateProgress(Mathf.Lerp(0.05f, 0.1f, iconAsync.Progress));

                // Wait a frame
                yield return null;
            }


            // ### Load section - scripts
            if ((bundle.Flags & DLCBundle.ContentFlags.ScriptAssets) != 0)
            {
                Debug.Log("Loading script assemblies...");

                // Check for allowed
                if(DLC.IsScriptingSupported == false)
                {
                    Debug.LogWarning("DLC content includes script assemblies but scripting is not supported on this platform/runtime. Ensure that scripting is only enabled for Desktop platforms using the Mono backend");
                    Debug.LogWarning("DLC content will be loaded but may include missing scripts!");
                }
                else
                {
                    // Load our scripts
                    scriptAssembly = new DLCScriptAssembly();
                    bundle.RequestLoad(DLCBundle.ContentType.ScriptAssembly, scriptAssembly);

                    // Activate our script assemblies
                    scriptAssembly.LoadScriptAssemblyImages();
                }
            }


            // Check for load bundles
            bool loadBundles = loadMode == DLCLoadMode.Full;


            // ### Load section - shared assets
            // Shared assets
            if ((bundle.Flags & DLCBundle.ContentFlags.SharedAssets) != 0)
            {
                Debug.Log("DLC content includes shared assets...");

                // Create our shared bundle
                if (loadBundles == true)
                {
                    Debug.Log("Prepare to load shared assets bundle...");
                    sharedAssetsBundle = new DLCContentBundle(bundle.GetContentStream(DLCBundle.ContentType.SharedAssetBundle));
                }


                // Load our asset metadata
                Debug.Log("Load shared assets metadata...");
                DLCContentBundleMetadata sharedAssetsMetadata = new DLCContentBundleMetadata();
                DLCAsync sharedAssetsMetadataAsync = bundle.RequestLoadAsync(this, DLCBundle.ContentType.SharedAssetMetadata, sharedAssetsMetadata);

                // Update status
                async.UpdateStatus("Loading shared assets metadata");

                // Wait for loaded
                while(sharedAssetsMetadataAsync.IsDone == false)
                {
                    // Update progress - only show metadata progress when we are performing metadata only load, otherwise it will load too quick
                    if (loadBundles == false)
                        async.UpdateProgress(Mathf.Lerp(0.1f, 0.55f, sharedAssetsMetadataAsync.Progress));

                    // Wait a frame
                    yield return null;
                }

                // Check for full load
                if (loadBundles == true)
                {
                    // Extract our shared asset table
                    sharedAssetsCollection = sharedAssetsMetadata.ExtractSharedAssets(this, sharedAssetsBundle, loadMode);

                    // Add to dlc
                    DLC.allSharedAssets.AddRange(sharedAssetsCollection.EnumerateAll());


                    // Preload if option is set - otherwise lazy load on demand
                    if ((bundle.Flags & DLCBundle.ContentFlags.PreloadSharedBundle) != 0)
                    {
                        // Create request
                        Debug.Log("Preloading shared assets bundle...");
                        DLCAsync sharedAssetsAsync = sharedAssetsBundle.RequestLoadAsync(this);

                        // Update status
                        async.UpdateStatus("Preloading shared assets");

                        // Wait for loaded
                        while (sharedAssetsAsync.IsDone == false)
                        {
                            // Calculate max progress
                            float max = ((bundle.Flags & DLCBundle.ContentFlags.SceneAssets) != 0)
                                ? 0.55f : 1f;

                            // Update progress
                            async.UpdateProgress(Mathf.Lerp(0.1f, max, sharedAssetsAsync.Progress));

                            // Wait a frame
                            yield return null;
                        }
                    }
                    else
                        Debug.Log("Shared assets bundle is not marked for preloading and will be loaded on demand...");
                }
            }


            // ### Load section - scene assets
            // Scene assets
            if ((bundle.Flags & DLCBundle.ContentFlags.SceneAssets) != 0)
            {
                Debug.Log("DLC content includes scene assets...");

                // Create our scene bundle
                if (loadBundles == true)
                {
                    Debug.Log("Prepare to load scene assets bundle...");
                    sceneAssetsBundle = new DLCContentBundle(bundle.GetContentStream(DLCBundle.ContentType.SceneAssetBundle));
                }


                // Load our scene metadata
                Debug.Log("Load scene assets metadata...");
                DLCContentBundleMetadata sceneAssetsMetadata = new DLCContentBundleMetadata();
                DLCAsync sceneAssetsMetadataAsync = bundle.RequestLoadAsync(this, DLCBundle.ContentType.SceneAssetMetadata, sceneAssetsMetadata);

                // Update status
                async.UpdateStatus("Loading scene metadata");

                // Wait for loaded
                while(sceneAssetsMetadataAsync.IsDone == false)
                {
                    // Update progress - only show metadata progress when we are performing metadata only load, otherwise it will load too quick
                    if(loadBundles == false)
                        sceneAssetsMetadataAsync.UpdateProgress(Mathf.InverseLerp(0.55f, 1f, sceneAssetsMetadataAsync.Progress));

                    // Wait a frame
                    yield return null;
                }

                // Check for full load
                if (loadBundles == true)
                {
                    // Extract our scene asset table
                    sceneAssetsCollection = sceneAssetsMetadata.ExtractSceneAssets(this, sceneAssetsBundle, loadMode);

                    // Add to dlc
                    DLC.allSceneAssets.AddRange(sceneAssetsCollection.EnumerateAll());


                    // Preload if option is set - otherwise lazy load on demand
                    if ((bundle.Flags & DLCBundle.ContentFlags.PreloadSceneBundle) != 0)
                    {
                        // Create request
                        Debug.Log("Preload scene assets bundle...");
                        DLCAsync sceneAssetsAsync = sceneAssetsBundle.RequestLoadAsync(this);

                        // Update status
                        async.UpdateStatus("Preloading scene assets");

                        // Wait for loaded
                        while (sceneAssetsAsync.IsDone == false)
                        {
                            // Calculate max progress
                            float min = ((bundle.Flags & DLCBundle.ContentFlags.SharedAssets) != 0)
                                ? 0.55f : 0.1f;

                            // Update progress
                            async.UpdateProgress(Mathf.Lerp(min, 1f, sceneAssetsAsync.Progress));

                            // Wait a frame
                            yield return null;
                        }
                    }
                    else
                        Debug.Log("Scene assets bundle is not marked for preloading and will be loaded on demand...");
                }
            }


            // Set load state
            Debug.Log("DLC was loaded successfully: " + metadata.NameInfo.Name);
            loadAsync = null;
            SetLoadState(DLCLoadState.Loaded);

            // Complete operation
            async.Complete(true, this);

            // Track in use
            if (drmProvider != null && Application.isPlaying == true)
            {
                try
                { 
                    drmProvider.TrackDLCUsage(metadata.NameInfo.UniqueKey, true);
                }
                catch (NotSupportedException) { } // The API is optional and can be discarded using a NotSupportedException
                catch (Exception e)
                {
                    Debug.LogError("Unhandled exception thrown by DRM call: " + e);
                }
            }
        }

        internal DLCAsync<DLCContent> AwaitLoadedAsync(IDLCAsyncProvider asyncProvider)
        {
            // Check for already loaded
            if (IsLoading == false)
                return DLCAsync<DLCContent>.Completed(true, this);

            // Check for async loading
            if (loadAsync != null)
                return loadAsync;

            // Create async
            DLCAsync<DLCContent> async = new DLCAsync<DLCContent>();

            // Wait for sync loading
            asyncProvider.RunAsync(AwaitLoadedRoutine(async));

            return async;
        }

        private IEnumerator AwaitLoadedRoutine(DLCAsync<DLCContent> async)
        {
            // Wait for loaded
            while (IsLoaded == false)
                yield return null;

            // Complete operation
            async.Complete(IsLoaded, this);
        }

#if UNITY_EDITOR
        [ContextMenu("Unload DLC")]
        private void UnloadInEditor() => Unload();
#endif

        /// <summary>
        /// Request that the DLC contents be unloaded from memory.
        /// This will cause DLC metadata, assets and scenes to be unloaded.
        /// Note that the <see cref="DLCContent"/> container object will not be destroyed and will remain in memory until manually destroyed.
        /// Use <see cref="Dispose"/> to unload the content and to recycle the <see cref="DLCContent"/> container object for use in other DLC load operations.
        /// </summary>
        /// <param name="withAssets">Should asset instances also be unloaded</param>
        /// <param name="destroyInstantiatedContent">Should objects in any scene that were instantiated from DLC prefabs also be destroyed</param>
        public void Unload(bool withAssets = true, bool destroyInstantiatedContent = false)
        {
            // Remove this
            if (allContents.Contains(this) == true)
                allContents.Remove(this);

            // Check for loaded
            if (loadState != DLCLoadState.Loaded)
                return;

            // Track no longer use
            if (drmProvider != null && Application.isPlaying == true)
            {
                try
                {
                    drmProvider.TrackDLCUsage(metadata.NameInfo.UniqueKey, false);
                    drmProvider = null;
                }
                catch (NotSupportedException) { } // The API is optional and can be discarded using a NotSupportedException
                catch (Exception e)
                {
                    Debug.LogError("Unhandled exception thrown by DRM call: " + e);
                }
            }

            // Check for instantiated content
            if (destroyInstantiatedContent == true)
                willDestroyInstantiatedContent = true;

            // Trigger event
            OnWillUnload.Invoke();
            DLC.OnContentWillUnload.Invoke(this);

            // Release loaded shared assets
            if (sharedAssetsBundle != null)
            {
                sharedAssetsBundle.Unload(withAssets);
                sharedAssetsBundle = null;
            }

            // Release loaded scenes
            if (sceneAssetsBundle != null)
            {
                sceneAssetsBundle.Unload(withAssets);
                sceneAssetsBundle = null;
            }

            // Reset state
            metadata = null;

            // Dispose of icons
            if (iconSet != null)
            {
                iconSet.Dispose();
                iconSet = null;
            }

            // Remove from dlc
            foreach (DLCSharedAsset asset in sharedAssetsCollection)
                DLC.allSharedAssets.Remove(asset);

            foreach(DLCSceneAsset asset in sceneAssetsCollection)
                DLC.allSceneAssets.Remove(asset);

            // Reset collections
            sharedAssetsCollection = DLCSharedAssetCollection.Empty();
            sceneAssetsCollection = DLCSceneAssetCollection.Empty();

            // Reset state
            loadMode = DLCLoadMode.Full;
            SetLoadState(DLCLoadState.NotLoaded);

            // Dispose of bundle
            if (bundle != null)
            { 
                bundle.Dispose();
                bundle = null;
            }
            streamProvider = null;

            // Trigger event
            OnUnloaded.Invoke();
            DLC.OnContentUnloaded.Invoke(this);

            // Reset content flag after event
            willDestroyInstantiatedContent = false;

            // Cache this host - Check for not destroyed
            if (this != null)
                availableContents.Enqueue(this);
        }

        /// <summary>
        /// Request that the DLC contents be unloaded from memory asynchronously.
        /// This will cause DLC metadata, assets and scenes to be unloaded.
        /// Note that the <see cref="DLCContent"/> container object will not be destroyed and will remain in memory until manually destroyed.
        /// Use <see cref="Dispose"/> to unload the content and to recycle the <see cref="DLCContent"/> container object for use in other DLC load operations.
        /// </summary>
        /// <param name="withAssets">Should asset instances also be unloaded</param>
        /// <param name="destroyInstantiatedContent">Should objects in any scene that were instantiated from DLC prefabs also be destroyed</param>
        public DLCAsync UnloadAsync(bool withAssets = true, bool destroyInstantiatedContent = false)
        {
            // Remove this
            if (allContents.Contains(this) == true)
                allContents.Remove(this);

            // Check for loaded
            if (loadState != DLCLoadState.Loaded)
                return DLCAsync.Completed(true);

            // Track no longer use
            if (drmProvider != null && Application.isPlaying == true)
            {
                try
                { 
                    drmProvider.TrackDLCUsage(metadata.NameInfo.UniqueKey, false);
                    drmProvider = null;
                }
                catch (NotSupportedException) { } // The API is optional and can be discarded using a NotSupportedException
                catch (Exception e)
                {
                    Debug.LogError("Unhandled exception thrown by DRM call: " + e);
                }
            }

            // Check for instantiated content
            if (destroyInstantiatedContent == true)
                willDestroyInstantiatedContent = true;

            // Trigger event
            OnWillUnload.Invoke();
            DLC.OnContentWillUnload.Invoke(this);
       
            // Reset state
            metadata = null;

            // Dispose of icons
            if (iconSet != null)
            {
                iconSet.Dispose();
                iconSet = null;
            }

            // Remove from dlc
            foreach (DLCSharedAsset asset in sharedAssetsCollection)
                DLC.allSharedAssets.Remove(asset);

            foreach (DLCSceneAsset asset in sceneAssetsCollection)
                DLC.allSceneAssets.Remove(asset);

            // Reset collections
            sharedAssetsCollection = DLCSharedAssetCollection.Empty();
            sceneAssetsCollection = DLCSceneAssetCollection.Empty();

            // Reset state
            loadMode = DLCLoadMode.Full;
            SetLoadState(DLCLoadState.NotLoaded);

            // Create async
            DLCAsync async = new DLCAsync();
            ((IDLCAsyncProvider)this).RunAsync(WaitForUnloadAsync(this, async, withAssets));

            return async;
        }

        private IEnumerator WaitForUnloadAsync(IDLCAsyncProvider asyncProvider, DLCAsync async, bool withAssets)
        {
            int bundleCount = 0;
            if (sharedAssetsBundle != null) bundleCount++;
            if(sceneAssetsBundle != null) bundleCount++;

            // Release loaded shared assets
            if (sharedAssetsBundle != null)
            {
                // Update status
                async.UpdateStatus("Unloading shared assets bundle");

                // Request async unload
                DLCAsync bundleAsync = sharedAssetsBundle.UnloadAsync(asyncProvider, withAssets);

                // Wait for completed
                while(bundleAsync.IsDone == false)
                {
                    // Update progress
                    async.UpdateProgress(bundleAsync.Progress / bundleCount);
                    yield return null;
                }
                sharedAssetsBundle = null;
            }

            // Release loaded scenes
            if (sceneAssetsBundle != null)
            {
                // Update status
                async.UpdateStatus("Unloading scene assets bundle");

                // Request async unload
                DLCAsync bundleAsync = sceneAssetsBundle.UnloadAsync(asyncProvider, withAssets);

                // Wait for completed
                while(bundleAsync.IsDone == false)
                {
                    // Update progress
                    async.UpdateProgress(bundleCount > 1 ? 0.5f + (bundleAsync.Progress / bundleCount) : bundleAsync.Progress);
                    yield return null;
                }
                sceneAssetsBundle = null;
            }


            // Dispose of bundle
            if (bundle != null)
            {
                bundle.Dispose();
                bundle = null;
            }
            streamProvider = null;

            // Trigger event
            OnUnloaded.Invoke();
            DLC.OnContentUnloaded.Invoke(this);

            // Reset flag after event
            willDestroyInstantiatedContent = false;
        }

        private void SetLoadState(DLCLoadState loadState)
        {
            this.loadState = loadState;

            // Update name
            gameObject.name = loadState == DLCLoadState.Loaded
                ? string.Format("{0}<{1}, {2}>", hostName, NameInfo.Name, NameInfo.Version)
                : hostName;
        }

        /// <summary>
        /// Unload this DLC and release all associated resources.
        /// Will call <see cref="Unload(bool)"/> with `true` argument internally to cause the DLC to be unloaded from memory.
        /// Note that this will cause the <see cref="DLCContent"/> container object to be recycled and will become available for other load operations.
        /// For that reason references should not be kept after dispose has been called.
        /// </summary>
        public void Dispose()
        {
            // Destroy the host object triggering a full unload and destruction of the host
            Destroy(gameObject);
        }

        Coroutine IDLCAsyncProvider.RunAsync(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        private void CheckLoaded()
        {
            if (loadState != DLCLoadState.Loaded)
                throw new DLCNotLoadedException();
        }

        private void CheckLoadedWithMode(DLCLoadMode mode)
        {
            if (loadState != DLCLoadState.Loaded)
                throw new DLCNotLoadedException();

            if (loadMode < mode)
                throw new DLCNotLoadedException("The requested DLC content is not available in the current load mode");
        }

        internal static DLCContent GetDLCContentHost(IDRMProvider provider = null)
        {
            DLCContent result = null;

            // Get first element
            if (availableContents.Count > 0)
                result = availableContents.Dequeue();

            // Create new
            if(result == null)
            {
                // Create new content host
                GameObject temp = new GameObject(defaultGameObjectName);
                result = temp.AddComponent<DLCContent>();

                // Keep alive in play mode
                if(Application.isPlaying == true)
                    DontDestroyOnLoad(temp);
            }

            // Update provider
            result.drmProvider = provider;

            return result;
        }
    }
}
