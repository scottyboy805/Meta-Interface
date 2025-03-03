using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DLCToolkit
{
    /// <summary>
    /// Contains information about a specific built DLC that was known to the game at build time.
    /// </summary>
    [Serializable]
    public sealed class DLCManifestEntry
    {
        // Internal
        [SerializeField]
        internal string dlcUniqueKey = "";
        [SerializeField]
        internal string dlcName = "";
        [SerializeField]
        internal string dlcPath = "";
        [SerializeField]
        internal string dlcIAPName = "";
        [SerializeField]
        internal bool shipWithGame = false;
        [SerializeField]
        internal bool streamingContent = false;
        [SerializeField]
        internal long sizeOnDisk = 0;
        [SerializeField]
        internal DateTime lastWriteTime = DateTime.MinValue;
        [SerializeField]
        internal List<string> assets = new List<string>();
        [SerializeField]
        internal List<string> scenes = new List<string>();

        // Properties
        /// <summary>
        /// The unique key of the DLC content.
        /// Will be the unique key for the current platform.
        /// </summary>
        public string DLCUniqueKey
        {
            get { return dlcUniqueKey; }
        }

        /// <summary>
        /// The name of the DLC content.
        /// </summary>
        public string DLCName
        {
            get { return dlcName; }
        }

        /// <summary>
        /// The optional relative path of the DLC content if it was marked as `Ship With Game`.
        /// </summary>
        public string DLCPath
        {
            get { return dlcPath; }
        }

        /// <summary>
        /// The optional name of the in-app purchase associated with this DLC, used for ownership verification purposes.
        /// </summary>
        public string DLCIAPName
        {
            get { return dlcIAPName; }
        }

        /// <summary>
        /// Is the DLC content marked as `Ship With Game`. 
        /// If it is, then in most cases it will be available for loading at runtime locally.
        /// </summary>
        public bool ShipWithGame
        {
            get { return shipWithGame; }
        }

        /// <summary>
        /// Is the DLC content marked as `Streaming` in the `Ship With Game` options.
        /// Streaming means that the DLC will be included in the game streaming assets directory.
        /// </summary>
        public bool StreamingContent
        {
            get { return streamingContent; }
        }

        /// <summary>
        /// The size of the built DLC content on disk for the target platform.
        /// </summary>
        public long SizeOnDisk
        {
            get { return sizeOnDisk; }
        }

        /// <summary>
        /// The last time the built DLC was written to, usually indicating the last built time of the DLC.
        /// </summary>
        public DateTime LastWriteTime
        {
            get { return lastWriteTime; }
        }

        /// <summary>
        /// Get a list of asset paths associated with this DLC.
        /// </summary>
        public IList<string> Assets
        {
            get { return assets; }
        }

        /// <summary>
        /// Get a list of scene paths associated with this DLC.
        /// </summary>
        public List<string> Scenes
        {
            get { return scenes; }
        }

        // Constructor
        internal DLCManifestEntry() { }

        // Methods
        /// <summary>
        /// Get the full loadable path for the DLC if it is marked as `Ship With Game`.
        /// </summary>
        /// <returns>The DLC load path for the installed DLC, or an empty string if the DLC is not available locally</returns>
        public string GetDLCLoadPath()
        {
            if (shipWithGame == true)
            {
                // Check for streaming
                if (streamingContent == true)
                {
                    return Application.streamingAssetsPath + "/" + dlcPath;
                }
                else
                {
                    // Get parent directory
                    Uri parent = new Uri(new Uri(Application.dataPath), ".");

                    // Build the final url - / is not required here since it is already has one
                    return parent.AbsoluteUri + dlcPath;
                }
            }

            // Fallback to relative path
            return dlcPath;
        }

        /// <summary>
        /// Check whether this manifest entry contains an asset with the specified name or path
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to search for</param>
        /// <returns>True if a matching asset was found or false if not</returns>
        public bool HasAsset(string nameOrPath)
        {
            return HasPath(assets, nameOrPath);
        }

        /// <summary>
        /// Check whether this manifest entry contains a scene with the specified name or path
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to search for</param>
        /// <returns>True if a matching scene was found or false if not</returns>
        public bool HasScene(string nameOrPath)
        {
            return HasPath(scenes, nameOrPath);
        }

        private bool HasPath(IList<string> searchPaths, string nameOrPath)
        {
            bool ignoreCase = true;

            // Get the extension of the specified path
            string ext = Path.GetExtension(nameOrPath);

            // Check for extension
            bool hasExtension = string.IsNullOrEmpty(ext) == false;

            // Remove the extension - It will still be checked later if required
            nameOrPath = Path.ChangeExtension(nameOrPath, null);

            //foreach (string searchPath in searchPaths)
            for(int i = 0; i < searchPaths.Count; i++)
            {
                string searchPath = searchPaths[i];

                // Check for no extension
                if (hasExtension == false)
                    searchPath = Path.ChangeExtension(searchPath, null);

                // Check for full path
                if (string.Compare(searchPath, nameOrPath + ext, ignoreCase) == 0)
                    return true;

                // Check for relative path
                if (string.Compare(searchPath, nameOrPath, ignoreCase) == 0)
                {
                    // Check for no extension
                    if (string.IsNullOrEmpty(ext) == true)
                        return true;

                    // We need to compare extension too
                    if (string.Compare(Path.GetExtension(searchPath), ext, ignoreCase) == 0)
                        return true;
                }

                // Check for name only
                if (string.Compare(Path.GetFileNameWithoutExtension(searchPath), nameOrPath, ignoreCase) == 0)
                {
                    // Check for no extension
                    if (string.IsNullOrEmpty(ext) == true)
                        return true;

                    // We need to compare extension too
                    if (string.Compare(Path.GetExtension(searchPath), ext, ignoreCase) == 0)
                        return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Contains information about all built DLC that existed at the time of building the game.
    /// Additionally, if the DLC is marked as `Ship With Game`, the manifest will also include the DLC load path relative to the base game.
    /// </summary>
    [Serializable]
    public sealed class DLCManifest
    {
        // Internal
        [SerializeField]
        internal int version = 110;
        [SerializeField]
        internal DLCManifestEntry[] dlcContents = null;

        // Public
        /// <summary>
        /// Get the default name of the DLC manifest that should exist in all builds as part of steaming assets.
        /// </summary>
        public const string ManifestName = "dlc.manifest";

        // Properties
        /// <summary>
        /// Get the version of the manifest.
        /// </summary>
        public int Version
        {
            get { return version; }
        }

        /// <summary>
        /// Get all DLC entries that were known to the game at build time.
        /// </summary>
        public DLCManifestEntry[] DLCContents
        {
            get { return dlcContents; }
        }

        // Constructor
        internal DLCManifest() { }

        // Methods
        /// <summary>
        /// Save this manifest as json string.
        /// </summary>
        /// <returns>The json string representing the manifest</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Save this manifest as a json file
        /// </summary>
        /// <param name="jsonFilePath">The file path to save to</param>
        public void ToJsonFile(string jsonFilePath)
        {
            // Get contents
            string json = ToJson();

            // Write to file
            File.WriteAllText(jsonFilePath, json);
        }

        /// <summary>
        /// Load the manifest from the specified json string.
        /// </summary>
        /// <param name="json">The json formatted string containing manifest information</param>
        /// <returns>The <see cref="DLCManifest"/> created from the json string</returns>
        public static DLCManifest FromJson(string json)
        {
            return JsonUtility.FromJson<DLCManifest>(json);
        }

        /// <summary>
        /// Load the manifest from the specified json file.
        /// </summary>
        /// <param name="jsonFilePath">the file path to load from</param>
        /// <returns>The <see cref="DLCManifest"/> created from the json file</returns>
        public static DLCManifest FromJsonFile(string jsonFilePath)
        {
            // Read contents
            string json = File.ReadAllText(jsonFilePath);

            // Get json
            return FromJson(json);
        }

        /// <summary>
        /// Attempt to load the manifest from the specified url via web request.
        /// The manifest will always be included in the built game in the streaming assets location.
        /// </summary>
        /// <returns></returns>
        public static DLCAsync<DLCManifest> FromJsonUrlAsync(string jsonUrl, IDLCAsyncProvider asyncProvider = null)
        {
            // Check for async handler
            if (asyncProvider == null)
                asyncProvider = DLC.AsyncCommon;

            // Create async
            DLCAsync<DLCManifest> async = new DLCAsync<DLCManifest>();

            // Start async routine
            asyncProvider.RunAsync(FetchManifestRoutine());
            IEnumerator FetchManifestRoutine()
            {
                // Create request
                using (UnityWebRequest request = UnityWebRequest.Get(jsonUrl))
                {
                    // Set handler
                    request.downloadHandler = new DownloadHandlerBuffer();

                    // Wait for completed
                    yield return request.SendWebRequest();

                    // Check for success
                    if(request.result == UnityWebRequest.Result.Success)
                    {
                        // Create manifest
                        DLCManifest manifest = FromJson(request.downloadHandler.text);

                        // Complete operation
                        async.Complete(true, manifest);
                    }
                    else
                    {
                        // Report error
                        async.Error(request.error);
                    }
                }
            }
            return async;
        }

        /// <summary>
        /// Attempt to load the manifest from the game project.
        /// The manifest will always be included in the built game in the streaming assets location.
        /// </summary>
        /// <returns></returns>
        public static DLCAsync<DLCManifest> FromProjectAsync(IDLCAsyncProvider asyncProvider = null)
        {
            // Get the path
            string pathOrUrl = Application.streamingAssetsPath + "/" + ManifestName;

#if UNITY_WEBGL || UNITY_ANDROID
            // Get from url
            return FromJsonUrlAsync(pathOrUrl, asyncProvider);
#else
            // Try to load
            DLCManifest manifest = FromJsonFile(pathOrUrl);

            // Create result
            return DLCAsync<DLCManifest>.Completed(manifest != null, manifest);
#endif
        }
    }
}
