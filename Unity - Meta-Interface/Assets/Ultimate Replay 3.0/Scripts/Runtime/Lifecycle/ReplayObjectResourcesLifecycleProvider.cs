using System;
using UnityEngine;

namespace UltimateReplay.Lifecycle
{
    [Serializable]
    public class ReplayObjectResourcesLifecycleProvider : ReplayObjectLifecycleProvider
    {
        // Private
        private ReplayObject replayResourcesPrefab = null;
        private ResourceRequest request = null;
        private bool loadFailed = false;
        private ReplayObjectPool pool = null;

        // Public
        public string resourcesPath = "MyReplayPrefab";
        public bool allowPooling = true;
        public bool asyncLoadOnStartup = true;

        // Properties
        public override bool IsAssigned
        {
            get { return string.IsNullOrEmpty(resourcesPath) == false; }
        }

        public override string ItemName
        {
            get { return resourcesPath; }
        }

        public override ReplayIdentity ItemPrefabIdentity
        {
            get
            {
                EnsurePrefabIsLoaded();
                return replayResourcesPrefab != null ? replayResourcesPrefab.PrefabIdentity : ReplayIdentity.invalid;
            }
        }

        // Methods
        private void OnEnable()
        {
            pool = new ReplayObjectPool(InstantiateReplayInstance, DestroyReplayInstance);

            // Request load
            if (asyncLoadOnStartup == true)
                RequestPrefabAsyncLoad();
        }

        public override ReplayObject InstantiateReplayInstance(Vector3 position, Quaternion rotation)
        {
            // Ensure loaded
            EnsurePrefabIsLoaded();

            // Check for assigned
            if(replayResourcesPrefab != null)
            {
                // Check for pooled
                if(allowPooling == true)
                {
                    // Get instance
                    ReplayObject pooledInstance = pool.Get();

                    // Update transform
                    pooledInstance.transform.position = position;
                    pooledInstance.transform.rotation = rotation;

                    return pooledInstance;
                }
                else
                {
                    // Create instance
                    return Instantiate(replayResourcesPrefab, position, rotation);
                }
            }

            return null;
        }

        public ReplayObject InstantiateReplayInstance()
        {
            EnsurePrefabIsLoaded();
            return Instantiate(replayResourcesPrefab);
        }

        public override void DestroyReplayInstance(ReplayObject replayInstance)
        {
            if(allowPooling == true)
            {
                pool.Release(replayInstance);
            }
            else
            {
                // Destroy
                Destroy(replayInstance.gameObject);
            }
        }

        private void RequestPrefabAsyncLoad()
        {
            // Create request
            request = Resources.LoadAsync<ReplayObject>(resourcesPath);
        }

        private void EnsurePrefabIsLoaded()
        {
            if(replayResourcesPrefab == null && loadFailed == false)
            {
                // Check for async request
                if(request != null && request.isDone == true && request.asset != null)
                {
                    // Store asset
                    replayResourcesPrefab = request.asset as ReplayObject;

                    // Check for success
                    loadFailed = replayResourcesPrefab == null;

                    // Check for success
                    if (replayResourcesPrefab != null)
                        return;
                }

                // Load immediate
                replayResourcesPrefab = Resources.Load<ReplayObject>(resourcesPath);

                // Check for success
                loadFailed = replayResourcesPrefab == null;
            }
        }
    }
}
