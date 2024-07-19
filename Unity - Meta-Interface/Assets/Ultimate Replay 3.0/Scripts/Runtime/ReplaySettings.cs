using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateReplay.Lifecycle;
using UltimateReplay.StatePreparation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateReplay
{
    /// <summary>
    /// Stores global settings used by the replay system.
    /// </summary>
#if ULTIMATEREPLAY_DEV
    [CreateAssetMenu(menuName = "Ultimate Replay/Replay Settings")]
#endif
    [Serializable]
    public sealed class ReplaySettings : ScriptableObject
    {
        // Internal
        [SerializeField]
        internal ReplayRecordOptions recordOptions = new ReplayRecordOptions();
        [SerializeField]
        internal ReplayPlaybackOptions playbackOptions = new ReplayPlaybackOptions();
        [SerializeField]
        internal ReplaySceneDiscovery sceneDiscovery = new ReplaySceneDiscovery();

        // Private
        [SerializeField, HideInInspector]
        private List<ReplayObjectLifecycleProvider> prefabProviders = new List<ReplayObjectLifecycleProvider>();
        private List<ReplayObjectLifecycleProvider> runtimePrefabProviders = new List<ReplayObjectLifecycleProvider>(); // Added at runtime - non-serialized

        [SerializeField, HideInInspector]
        private DefaultReplayPreparer defaultReplayPreparer = new DefaultReplayPreparer();

        // Properties
        /// <summary>
        /// Get the default <see cref="ReplayPlaybackOptions"/> that will be used if no options are provided by code.
        /// </summary>
        public ReplayPlaybackOptions PlaybackOptions
        {
            get { return playbackOptions; }
        }

        /// <summary>
        /// Get the default <see cref="ReplayRecordOptions"/> that will be used if no options are provided by code.
        /// </summary>
        public ReplayRecordOptions RecordOptions
        {
            get { return recordOptions; }
        }

        /// <summary>
        /// Get the replay objects discovery mode to use when searching Unity scenes.
        /// </summary>
        public ReplaySceneDiscovery SceneDiscovery
        {
            get { return sceneDiscovery; }
        }

        /// <summary>
        /// Get all <see cref="ReplayObjectLifecycleProvider"/> that have been setup by the user.
        /// </summary>
        public IReadOnlyList<ReplayObjectLifecycleProvider> PrefabProviders
        {
            get { return prefabProviders; }
        }

        /// <summary>
        /// Get the <see cref="DefaultReplayPreparer"/> that will be used to prepare replay objects by default.
        /// </summary>
        public DefaultReplayPreparer DefaultReplayPreparer
        {
            get { return defaultReplayPreparer; }
        }

        // Methods
        /// <summary>
        /// Attempt to instantiate a replay prefab instance for the specified prefab id.
        /// </summary>
        /// <param name="prefabId">The replay prefab id for the target replay object prefab</param>
        /// <param name="position">The position where the replay object should be instantiated</param>
        /// <param name="rotation">The initial rotation of the replay object</param>
        /// <returns>An instantiated <see cref="ReplayObject"/> or null if the specified prefab id could not be found</returns>
        public ReplayObject InstantiatePrefabProvider(ReplayIdentity prefabId, Vector3 position, Quaternion rotation)
        {
            // Get provider
            ReplayObjectLifecycleProvider provider = GetPrefabProvider(prefabId);

            // Check for found
            if (provider == null)
                return null;

            // Try to create instance
            try
            {
                // Create instance
                ReplayObject result = provider.InstantiateReplayInstance(position, rotation);

                // Check for null
                if (result != null)
                {
                    // Associate provider
                    result.LifecycleProvider = provider;
                    return result;
                }
            }
            catch(Exception e)
            {
                Debug.LogError("Exception while invoking prefab provider: " + provider);
                Debug.LogException(e);
            }

            // Provider failed
            Debug.LogWarning("Prefab provider failed to return an instance of a replay object: " + provider);
            return null;
        }

        /// <summary>
        /// Get the <see cref="ReplayObjectLifecycleProvider"/> for the replay prefab with the specified prefab id.
        /// </summary>
        /// <param name="prefabId">The replay id for the replay prefab</param>
        /// <returns>The associated <see cref="ReplayObjectLifecycleProvider"/> or null if the prefab id could not be found</returns>
        public ReplayObjectLifecycleProvider GetPrefabProvider(ReplayIdentity prefabId)
        {
            // Check for runtime providers
            if(runtimePrefabProviders.Count > 0)
            {
                foreach(ReplayObjectLifecycleProvider provider in runtimePrefabProviders)
                {
                    if (provider != null && provider.ItemPrefabIdentity == prefabId)
                        return provider;
                }
            }

            // Check for serialized providers
            foreach(ReplayObjectLifecycleProvider provider in prefabProviders)
            {
                if (provider != null && provider.ItemPrefabIdentity == prefabId)
                    return provider;
            }
            return null;
        }

        /// <summary>
        /// Returns a value indicating whether the specified replay id is a valid prefab id and has a <see cref="ReplayObjectLifecycleProvider"/> associated with it.
        /// </summary>
        /// <param name="prefabId">The replay id for a given replay prefab</param>
        /// <returns>True if a provider is registered or false if not</returns>
        public bool HasPrefabProvider(ReplayIdentity prefabId)
        {
            return GetPrefabProvider(prefabId) != null;
        }

        public void AddPrefabProvider(ReplayObjectLifecycleProvider provider)
        {
            // Check for null
            if(provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Check for already added
            if (provider.ItemPrefabIdentity != ReplayIdentity.invalid && HasPrefabProvider(provider.ItemPrefabIdentity) == true)
                throw new InvalidOperationException("A prefab provider with an identical prefab id already exists");

            // Check for runtime
            if (Application.isPlaying == true)
            {
                // Add to runtime collection - non-serialized
                runtimePrefabProviders.Add(provider);
            }
            else
            {
                // Add to collection
                prefabProviders.Add(provider);

#if UNITY_EDITOR
                // Check for save
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(provider)) == true)
                {
                    // Get this asset path
                    string thisPath = AssetDatabase.GetAssetPath(this);

                    // Serialize as part of this asset
                    AssetDatabase.AddObjectToAsset(provider, thisPath);
                    EditorUtility.SetDirty(this);
                }
#endif
            }
        }

        public void RemovePrefabProvider(ReplayObjectLifecycleProvider provider)
        {
            // Check for null
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            // Check for remove runtime
            if(runtimePrefabProviders.Contains(provider) == true)
            {
                runtimePrefabProviders.Remove(provider);
            }
            // Check for remove editor
            else if (prefabProviders.Contains(provider) == true)
            {
                // Remove from collection
                prefabProviders.Remove(provider);

                // Check for remove asset
#if UNITY_EDITOR
                string thisPath = AssetDatabase.GetAssetPath(this);
                string providerPath = AssetDatabase.GetAssetPath(provider);

                // Check for same path
                if (string.IsNullOrEmpty(providerPath) == false && thisPath == providerPath)
                {
                    // Remove from object
                    AssetDatabase.RemoveObjectFromAsset(provider);
                    EditorUtility.SetDirty(this);
                }
#endif
            }
        }
    }
}
