using DLCToolkit.Profile;
using System;
using System.Collections.Generic;

namespace DLCToolkit.BuildTools
{
    internal sealed class DLCBuildAssetCollection
    {
        // Private
        private static readonly string[] disallowedExtensions =
        {
            ".asmdef",
        };

        private static readonly Type[] disallowedTypes =
        {
            typeof(DLCProfile),
        };

        private List<DLCBuildAsset> assets = new List<DLCBuildAsset>();

        // Properties
        public int AssetCount
        {
            get { return assets.Count; }
        }

        public IReadOnlyList<DLCBuildAsset> Assets
        {
            get { return assets; }
        }

        public DLCContentFlags ContentFlags
        {
            get
            {
                DLCContentFlags flags = 0;

                // Add all content flags
                foreach (DLCBuildAsset asset in assets)
                    flags |= asset.ContentFlags;

                return flags;
            }
        }

        // Methods
        public DLCBuildAsset AddBuildAsset(string assetPath)
        {
            // Create the asset
            DLCBuildAsset asset = new DLCBuildAsset(assetPath);

            // Check for disallowed extension
            if (Array.Exists(disallowedExtensions, ext => ext == asset.Extension) == true)
                return null;

            // Check for disallowed type
            if (Array.Exists(disallowedTypes, t => t == asset.MainAssetType) == true)
                return null;

            // Check for already added
            if(assets.Exists(a => a.Guid == asset.Guid) == false)
            {
                assets.Add(asset);
            }
            else
            {
                // Find the existing asset
                asset = assets.Find(a => a.Guid == asset.Guid);
            }

            return asset;
        }

        public bool HasAssetContent(DLCContentFlags contentFlags)
        {
            foreach(DLCBuildAsset asset in assets)
            {
                if ((asset.ContentFlags & contentFlags) == contentFlags)
                    return true;
            }
            return false;
        }

        public IEnumerable<DLCBuildAsset> GetAssetsWithExtension(string extension, bool includeExcluded = false)
        {
            // Check all
            foreach(DLCBuildAsset asset in assets)
            {
                if(asset.Extension == extension && (asset.IsExcluded == false || includeExcluded == true))
                {
                    yield return asset;
                }
            }
        }

        public IEnumerable<DLCBuildAsset> GetSharedAssets(bool includeExcluded = false)
        {
            // Check all
            foreach (DLCBuildAsset asset in assets)
            { 
                // Check for shared asset
                if((asset.ContentFlags & DLCContentFlags.Assets) != 0 && (asset.IsExcluded == false || includeExcluded == true))
                {
                    yield return asset;
                }
            }
        }

        public IEnumerable<DLCBuildAsset> GetSceneAssets(bool includeExcluded = false)
        {
            // Check all
            foreach (DLCBuildAsset asset in assets)
            {
                // Check for scene asset
                if ((asset.ContentFlags & DLCContentFlags.Scenes) != 0 && (asset.IsExcluded == false || includeExcluded == true))
                {
                    yield return asset;
                }
            }
        }

        public IEnumerable<DLCBuildAsset> GetScriptAssets(bool includeExcluded = false)
        {
            // Check all
            foreach (DLCBuildAsset asset in assets)
            {
                if ((asset.ContentFlags & DLCContentFlags.Scripts) != 0 && (asset.IsExcluded == false || includeExcluded == true))
                {
                    yield return asset;
                }
            }
        }
    }
}
