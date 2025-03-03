using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace DLCToolkit.Assets
{
    /// <summary>
    /// Represents a collection of <see cref="DLCSharedAsset"/> which are stored in a given DLC.
    /// </summary>
    public sealed class DLCSharedAssetCollection : DLCAssetCollection<DLCSharedAsset>
    {
        // Constructor
        internal DLCSharedAssetCollection(List<DLCSharedAsset> assets) 
            : base(assets)
        {
        }

        // Methods
        ///<summary>
        /// Attempts to load an asset from the dlc with the specified name or path.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <returns>The loaded asset</returns>
        public Object Load(string nameOrPath)
        {
            return Find(nameOrPath)?.Load();
        }

        /// <summary>
        /// Attempts to load an asset from the dlc with the specified name or path as the specified generic type. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <typeparam name="T">The generic type to load the asset as</typeparam>
        /// <returns>The loaded asset as the generic type</returns>
        public T Load<T>(string nameOrPath) where T : Object
        {
            return Find(nameOrPath)?.Load<T>();
        }

        /// <summary>
        /// Attempts to load an asset with the specified name or path and with all associated sub assets. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <returns>An array of sub assets for this asset</returns>
        public Object[] LoadWithSubAssets(string nameOrPath)
        {
            return Find(nameOrPath)?.LoadWithSubAssets();
        }

        /// <summary>
        /// Attempts to load an asset with the specified name or path and with all associated sub assets. 
        /// This overload will only return assets that are of the specified generic type such as 'Mesh'.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <typeparam name="T">The generic asset type to return</typeparam>
        /// <returns>An array of sub assets for this asset</returns>
        public T[] LoadWithSubAssets<T>(string nameOrPath) where T : Object
        {
            return Find(nameOrPath)?.LoadWithSubAssets<T>();
        }

        /// <summary>
        /// Attempts to load an asset from the dlc with the specified name or path asynchronously. 
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<Object> LoadAsync(string nameOrPath)
        {
            DLCSharedAsset asset;
            return (asset = Find(nameOrPath)) != null
                ? asset.LoadAsync()
                : DLCAsync<Object>.Error("Could not find asset: " + nameOrPath);
        }

        /// <summary>
        /// Attempts to load this asset from the dlc with the specified name or path asynchronously. 
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <typeparam name="T">The generic type to load the asset as</typeparam>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<T> LoadAsync<T>(string nameOrPath) where T : Object
        {
            DLCSharedAsset asset;
            return (asset = Find(nameOrPath)) != null
                ? asset.LoadAsync<T>()
                : DLCAsync<T>.Error("Could not find asset: " + nameOrPath);
        }

        /// <summary>
        /// Attempts to load an asset with the specified name or path and with all associated sub assets from the dlc asynchronously.
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<Object[]> LoadWithSubAssetsAsync(string nameOrPath)
        {
            DLCSharedAsset asset;
            return (asset = Find(nameOrPath)) != null
                ? asset.LoadWithSubAssetsAsync()
                : DLCAsync<Object[]>.Error("Could not find asset: " + nameOrPath);
        }

        /// <summary>
        /// Attempts to load an asset with the specified name or path and with all associated sub assets from the dlc asynchronously.
        /// This method returns a <see cref="DLCAsync"/> object which is yieldable and contains information about the loading progress and status. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset to load</param>
        /// <typeparam name="T">The generic asset type used to specify which sub asset types to load</typeparam>
        /// <returns>A yieldable <see cref="DLCAsync"/> object</returns>
        public DLCAsync<T[]> LoadWithSubAssetsAsync<T>(string nameOrPath) where T : Object
        {
            DLCSharedAsset asset;
            return (asset = Find(nameOrPath)) != null
                ? asset.LoadWithSubAssetsAsync<T>()
                : DLCAsync<T[]>.Error("Could not find asset: " + nameOrPath);
        }

        internal static new DLCSharedAssetCollection Empty()
        {
            return new DLCSharedAssetCollection(new List<DLCSharedAsset>(0));
        }
    }
}
