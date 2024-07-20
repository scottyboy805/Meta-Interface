using DLCToolkit.Format;
using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DLCToolkit.Assets
{
    /// <summary>
    /// Represents a shared asset included in the DLC.
    /// Shared assets are content assets such as prefabs, textures, materials, audio clips, scriptable objects and more.
    /// </summary>
    public sealed class DLCSharedAsset : DLCAsset
    {
        // Constructor
        internal DLCSharedAsset(IDLCAsyncProvider asyncProvider, DLCContentBundle contentBundle, DLCLoadMode loadMode, int assetID, Type type, string fullName, string relativeName)
            : base(asyncProvider, contentBundle, loadMode, assetID, type, fullName, relativeName)
        {
            this.loadMode = loadMode;
        }

        // Methods
        ///<summary>
        /// Attempts to load this asset from the dlc.
        /// </summary>
        /// <returns>The loaded asset</returns>
        public Object Load()
        {
            return Load<Object>();
        }

        /// <summary>
        /// Attempts to load this asset from the dlc as the specified generic type. 
        /// </summary>
        /// <typeparam name="T">The generic type to load the asset as</typeparam>
        /// <returns>The loaded asset as the generic type</returns>
        public T Load<T>() where T : Object
        {
            // Check for loadable
            CheckLoaded();

            // Check for bundle
            if (contentBundle.IsNotLoaded == true)
            {
                Debug.Log("Loading shared assets bundle on demand...");
                contentBundle.RequestLoad();
            }

            // Check for loaded
            if (contentBundle.IsLoaded == true)
            {
                // Load and cache the asset
                Debug.Log("Loading shared asset: " + relativeName);
                T result = contentBundle.Bundle.LoadAsset<T>(fullName);
                loadedObject = result;

                return result;
            }

            // Could not load bundle or asset
            return null;
        }

        /// <summary>
        /// Attempts to load this asset with all associated sub assets. 
        /// </summary>
        /// <returns>An array of sub assets for this asset</returns>
        public Object[] LoadWithSubAssets()
        {
            return LoadWithSubAssets<Object>();
        }

        /// <summary>
        /// Attempts to load this asset with all associated sub assets. 
        /// This overload will only return assets that are of the specified generic type such as 'Mesh'.
        /// </summary>
        /// <typeparam name="T">The generic asset type to return</typeparam>
        /// <returns>An array of sub assets for this asset</returns>
        public T[] LoadWithSubAssets<T>() where T : Object
        {
            // Check for loadable
            CheckLoaded();

            // Check for bundle
            if (contentBundle.IsNotLoaded == true)
            {
                Debug.Log("Loading shared assets bundle on demand...");
                contentBundle.RequestLoad();
            }

            // Check for loaded
            if (contentBundle.IsLoaded == true)
            {
                // Load and cache the assets
                Debug.Log("Loading shared asset with sub assets: " + relativeName);
                T[] result = contentBundle.Bundle.LoadAssetWithSubAssets<T>(fullName);
                loadedObject = result;

                return result;
            }

            return null;
        }

        /// <summary>
        /// Attempts to load this asset from the dlc asynchronously. 
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<Object> LoadAsync()
        {
            // Check for loadable
            CheckLoaded();

            // Create async
            DLCAsync<Object> async = new DLCAsync<Object>();

            // Issue load request
            asyncProvider.RunAsync(LoadAssetFromBundleAsync(asyncProvider, async));

            return async;
        }

        /// <summary>
        /// Attempts to load this asset from the dlc asynchronously. 
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <typeparam name="T">The generic type to load the asset as</typeparam>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<T> LoadAsync<T>() where T : Object
        {
            // Check for loadable
            CheckLoaded();

            // Create async
            DLCAsync<T> async = new DLCAsync<T>();

            // Issue load request
            asyncProvider.RunAsync(LoadAssetFromBundleAsync(asyncProvider, async));

            return async;
        }

        /// <summary>
        /// Attempts to load this asset with all associated sub assets from the dlc asynchronously.
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<Object[]> LoadWithSubAssetsAsync()
        {
            // Check for loadable
            CheckLoaded();

            // Create async
            DLCAsync<Object[]> async = new DLCAsync<Object[]>();

            // Issue load request
            asyncProvider.RunAsync(LoadWithSubAssetsFromBundleAsync(asyncProvider, async));

            return async;
        }

        /// <summary>
        /// Attempts to load this asset with all associated sub assets from the dlc asynchronously.
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <typeparam name="T">The generic asset type used to specify which sub asset types to load</typeparam>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<T[]> LoadWithSubAssetsAsync<T>() where T : Object
        {
            // Check for loadable
            CheckLoaded();

            // Create async
            DLCAsync<T[]> async = new DLCAsync<T[]>();

            // Issue load request
            asyncProvider.RunAsync(LoadWithSubAssetsFromBundleAsync(asyncProvider, async));

            return async;
        }


        private IEnumerator LoadAssetFromBundleAsync<T>(IDLCAsyncProvider asyncProvider, DLCAsync<T> async)
        {
            bool didLoadBundle = false;

            // Check for bundle
            if (contentBundle.IsNotLoaded == true)
            {
                // Request load of bundle
                Debug.Log("Loading shared assets bundle async on demand...");
                DLCAsync asyncBundle = contentBundle.RequestLoadAsync(asyncProvider);

                // Update status
                async.UpdateStatus("Loading content bundle");
                didLoadBundle = true;

                // Wait for load
                while (asyncBundle.IsDone == false)
                {
                    // Update progress
                    async.UpdateProgress(asyncBundle.Progress * 0.5f);

                    // Wait a frame
                    yield return null;
                }

                // Check for success
                if(asyncBundle.IsSuccessful == false)
                {
                    async.UpdateStatus("Failed to load content bundle");
                    async.Complete(false);
                    yield break;
                }
            }

            // Issue the load request
            Debug.Log("Load shared asset: " + relativeName);
            AssetBundleRequest request = contentBundle.Bundle.LoadAssetAsync<T>(fullName);

            // Update status
            async.UpdateStatus("Loading asset: " + relativeName);

            // Wait for completed
            while(request.isDone == false)
            {
                // Update progress
                async.UpdateProgress(didLoadBundle ? request.progress * 0.5f : request.progress);

                // Wait a frame
                yield return null;
            }

            // Update loaded object
            loadedObject = request.asset;

            // Update status
            async.UpdateStatus("Loading complete");

            // Complete operation
            async.Complete(request.asset != null, request.asset);
        }

        private IEnumerator LoadWithSubAssetsFromBundleAsync<T>(IDLCAsyncProvider asyncProvider, DLCAsync<T[]> async) where T : Object
        {
            bool didLoadBundle = false;

            // Check for bundle
            if (contentBundle.IsNotLoaded == true)
            {
                // Request load of bundle
                Debug.Log("Loading shared assets bundle async on demand...");
                DLCAsync asyncBundle = contentBundle.RequestLoadAsync(asyncProvider);

                // Update status
                async.UpdateStatus("Loading content bundle");
                didLoadBundle = true;

                // Wait for load
                while (asyncBundle.IsDone == false)
                {
                    // Update progress
                    async.UpdateProgress(asyncBundle.Progress * 0.5f);

                    // Wait a frame
                    yield return null;
                }

                // Check for success
                if (asyncBundle.IsSuccessful == false)
                {
                    async.UpdateStatus("Failed to load content bundle");
                    async.Complete(false);
                    yield break;
                }
            }

            // Issue the load request
            Debug.Log("Loading shared asset with sub assets: " + relativeName);
            AssetBundleRequest request = contentBundle.Bundle.LoadAssetWithSubAssetsAsync<T>(fullName);

            // Update status
            async.UpdateStatus("Loading asset: " + relativeName);

            // Wait for completed
            while (request.isDone == false)
            {
                // Update progress
                async.UpdateProgress(didLoadBundle ? request.progress * 0.5f : request.progress);

                // Wait a frame
                yield return null;
            }

            // Convert the results
            T[] allAssets = Array.ConvertAll(request.allAssets, new Converter<Object, T>(t => t as T));

            // Update loaded object
            loadedObject = allAssets;

            // Update status
            async.UpdateStatus("Loading complete");

            // Complete operation
            async.Complete(allAssets != null, allAssets);
        }

        private void CheckLoaded()
        {
            if (loadMode != DLCLoadMode.Full)
                throw new DLCNotLoadedException("Cannot load assets because the DLC is loaded in metadata only mode");
        }
    }
}
