using DLCToolkit.Format;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DLCToolkit.BuildTools.Format
{
    internal class DLCBuildContentBundleMetadata : DLCContentBundleMetadata, IDLCBuildBundleEntry
    {
        // Constructor
        internal DLCBuildContentBundleMetadata(string dlcRelativeFolder)
        {
            this.dlcFolderRelative = dlcRelativeFolder;
        }

        // Methods
        public void AddAssetMetadata(string assetPath)
        {
            // Load the main asset
            Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

            // Check for not loaded
            if(asset == null)
            {
                Debug.LogWarning("Could not load asset: " + assetPath);
                return;
            }

            // Get asset type
            Type assetType = asset.GetType();

            // Handle scene type
            if (Path.GetExtension(assetPath) == ".unity")
                assetType = typeof(Scene);

            // Try to get relative path
            string relativePath = assetPath;

            try
            {
                relativePath = assetPath.Remove(0, dlcFolderRelative.Length + 1);
            }
            catch { }

            // Add asset
            assets.Add(new AssetMetadata
            {
                fullPath = assetPath,
                relativePath = relativePath,
                assemblyQualifiedType = assetType.AssemblyQualifiedName,
                resolvedType = assetType,
            });
        }

        public void WriteToStream(Stream stream)
        {
            // Create writer
            BinaryWriter writer = new BinaryWriter(stream);

            // Write relative folder
            writer.Write(dlcFolderRelative);

            // Write size
            writer.Write(assets.Count);

            // Write all
            for(int i = 0; i <  assets.Count; i++)
            {
                // Write the asset metadata
                WriteAssetEntry(writer, assets[i]);
            }
        }

        private void WriteAssetEntry(BinaryWriter writer, in AssetMetadata asset)
        {
            // Full path
            DLCFormatUtils.WriteString(writer, asset.fullPath);

            // Fully qualified type
            DLCFormatUtils.WriteString(writer, asset.assemblyQualifiedType);
        }
    }
}
