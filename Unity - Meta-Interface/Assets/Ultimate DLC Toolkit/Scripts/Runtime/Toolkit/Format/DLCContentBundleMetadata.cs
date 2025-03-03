using DLCToolkit.Assets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DLCToolkit.Format
{
    internal class DLCContentBundleMetadata : IDLCBundleEntry
    {
        // Type
        internal struct AssetMetadata
        {
            // Internal
            internal Type resolvedType;

            // Public
            public string fullPath;
            public string relativePath;
            public string assemblyQualifiedType;
        }

        // Protected
        protected string dlcFolderRelative = "";
        protected List<AssetMetadata> assets = new List<AssetMetadata>();

        // Methods
        public DLCSharedAssetCollection ExtractSharedAssets(IDLCAsyncProvider asyncProvider, DLCContentBundle contentBundle, DLCLoadMode loadMode)
        {
            // Create shared assets collection
            return new DLCSharedAssetCollection(ExtractAnyAssets((AssetMetadata metadata, int id) =>
            {
                // Create new instance of shared asset
                return new DLCSharedAsset(asyncProvider, contentBundle, loadMode, id, metadata.resolvedType, metadata.fullPath, metadata.relativePath);
            }));
        }

        public DLCSceneAssetCollection ExtractSceneAssets(IDLCAsyncProvider asyncProvider, DLCContentBundle contentBundle, DLCLoadMode loadMode)
        {
            // Create scene assets collection
            return new DLCSceneAssetCollection(ExtractAnyAssets((AssetMetadata metadata, int id) =>
            {
                // Create new instance of scene asset
                return new DLCSceneAsset(asyncProvider, contentBundle, loadMode, id, metadata.fullPath, metadata.relativePath);
            }));
        }

        private List<T> ExtractAnyAssets<T>(Func<AssetMetadata, int, T> convert)
        {
            // Create our actual array
            List<T> anyAssets = new List<T>(assets.Count);

            // Fill out entries
            for (int i = 0; i < assets.Count; i++)
            {
                // Create the shared asset
                anyAssets.Add(convert(assets[i], i + 1));
            }

            return anyAssets;
        }

        public void ReadFromStream(Stream stream)
        {
            // Create reader
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read relative string
                dlcFolderRelative = reader.ReadString();

                // Read size
                int size = reader.ReadInt32();

                // Load all
                for (int i = 0; i < size; i++)
                {
                    // Load the asset metadata
                    ReadAssetEntry(reader);
                }
            }
        }

        public DLCAsync ReadFromStreamAsync(IDLCAsyncProvider asyncProvider, Stream stream)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(ReadFromStreamAsyncRoutine(async, stream));

            return async;
        }

        private IEnumerator ReadFromStreamAsyncRoutine(DLCAsync async, Stream stream)
        {
            // Create reader
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read relative string
                dlcFolderRelative = reader.ReadString();

                // Read size
                int size = reader.ReadInt32();
                int asyncUpdate = 1000;

                // Load all
                for (int i = 0, j = 0; i < size; i++, j++)
                {
                    // Update status
                    async.UpdateStatus("Loading asset metadata: " + (i + 1) + " / " + size);
                    async.UpdateProgress(i + 1, size);

                    // Load the asset metadata
                    ReadAssetEntry(reader);

                    // Wait a frame
                    if (j > asyncUpdate)
                    {
                        j = 0;
                        yield return null;
                    }
                }
            }

            // Mark as completed
            async.Complete(true);
        }

        private void ReadAssetEntry(BinaryReader reader)
        {
            string fullPath = DLCFormatUtils.ReadString(reader); //reader.ReadString();
            string relativePath = fullPath;

            // Try to process relative path
            try
            {
                relativePath = fullPath.Remove(0, dlcFolderRelative.Length + 1);
            }
            catch { }

            // Get type string
            string assemblyQualifiedType = DLCFormatUtils.ReadString(reader); //reader.ReadString();
            Type resolvedType = null;

            try
            {
                // Try to resolve
                resolvedType = Type.GetType(assemblyQualifiedType, true);
            }
            catch
            {
                Debug.LogWarning("Could not load meta type: " +  assemblyQualifiedType);
            }

            // Add the new asset entry
            assets.Add(new AssetMetadata
            {
                fullPath = fullPath,
                relativePath = relativePath,
                assemblyQualifiedType = assemblyQualifiedType,
                resolvedType = resolvedType,
            });
        }
    }
}
