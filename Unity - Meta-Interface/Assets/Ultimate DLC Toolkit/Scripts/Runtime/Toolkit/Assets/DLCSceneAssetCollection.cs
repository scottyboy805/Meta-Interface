using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DLCToolkit.Assets
{
    /// <summary>
    /// Represents a collection of <see cref="DLCSceneAsset"/> which are stored in a given DLC.
    /// </summary>
    public sealed class DLCSceneAssetCollection : DLCAssetCollection<DLCSceneAsset>
    {
        // Constructor
        internal DLCSceneAssetCollection(List<DLCSceneAsset> assets) 
            : base(assets)
        {
        }

        // Methods
        /// <summary>
        /// Load a <see cref="DLCSceneAsset"/> with the specified name or path. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to load</param>
        /// <param name="loadSceneMode">The mode to use when loading the scene</param>
        public void Load(string nameOrPath, LoadSceneMode loadSceneMode)
        {
            Find(nameOrPath)?.Load(loadSceneMode);
        }

        /// <summary>
        /// Load a <see cref="DLCSceneAsset"/> with the specified name or path. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to load</param>
        /// <param name="loadSceneParameters">The load scene parameters to use when loading the scene</param>
        public void Load(string nameOrPath, LoadSceneParameters loadSceneParameters)
        {
            Find(nameOrPath)?.Load(loadSceneParameters);
        }

        /// <summary>
        /// Load a <see cref="DLCSceneAsset"/> with the specified name or path asynchronously. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to load</param>
        /// <param name="loadSceneMode">Should the scene be additively loaded into the current scene</param>
        /// <param name="allowSceneActivation">Should the scene be activated as soon as it is loaded</param>
        public DLCAsync LoadAsync(string nameOrPath, LoadSceneMode loadSceneMode, bool allowSceneActivation = true)
        {
            return Find(nameOrPath)?.LoadAsync(loadSceneMode, allowSceneActivation);
        }

        /// <summary>
        /// Load a <see cref="DLCSceneAsset"/> with the specified name or path asynchronously. 
        /// </summary>
        /// <param name="nameOrPath">The name or path of the scene to load</param>
        /// <param name="loadSceneParameters">The parameters to use when loading the scene</param>
        /// <param name="allowSceneActivation">Should the scene be activated as soon as it is loaded</param>
        public DLCAsync LoadAsync(string nameOrPath, LoadSceneParameters loadSceneParameters, bool allowSceneActivation = true)
        {
            return Find(nameOrPath)?.LoadAsync(loadSceneParameters, allowSceneActivation);
        }

        internal static new DLCSceneAssetCollection Empty()
        {
            return new DLCSceneAssetCollection(new List<DLCSceneAsset>(0));
        }
    }
}
