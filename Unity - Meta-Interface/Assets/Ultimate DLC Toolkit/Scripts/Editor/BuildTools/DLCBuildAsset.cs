using System;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace DLCToolkit.BuildTools
{
    internal sealed class DLCBuildAsset
    {
        // Internal
        internal const string scriptExtension = ".cs";
        internal const string sceneExtension = ".unity";

        // Private
        private string guid = "";
        private string name = "";
        private string extension = "";
        private string fullPath = "";
        private string relativePath = "";

        private Object mainAsset = null;
        private Type mainAssetType = null;

        private DLCContentFlags contentFlags = 0;
        private bool isExcluded = false;

        // Properties
        public string Guid
        {
            get { return guid; }
        }

        public string Name
        {
            get { return name; }
        }

        public string Extension
        {
            get { return extension; }
        }

        public string FullPath
        {
            get { return fullPath; }
        }

        public string RelativePath
        {
            get { return relativePath; }
        }

        public bool IsScriptAsset
        {
            get { return extension == scriptExtension; }
        }

        public bool IsSceneAsset
        {
            get { return extension == sceneExtension; }
        }

        public Object MainAsset
        {
            get { return mainAsset; }
        }

        public Type MainAssetType
        {
            get { return mainAssetType; }
        }

        public DLCContentFlags ContentFlags
        {
            get { return contentFlags; }
        }

        public bool IsExcluded
        {
            get { return isExcluded; }
        }

        // Constructor
        public DLCBuildAsset(string fullPath)
        {
            this.fullPath = Path.IsPathRooted(fullPath) == true ? fullPath : Path.GetFullPath(fullPath);
            this.relativePath = FileUtil.GetProjectRelativePath(fullPath.Replace('\\', '/'));
            this.name = Path.GetFileNameWithoutExtension(fullPath);
            this.extension = Path.GetExtension(fullPath);

            // Get guid
            this.guid = AssetDatabase.AssetPathToGUID(relativePath);

            // Load the asset
            if (extension == sceneExtension)
            {
                // Update flags scene
                contentFlags |= DLCContentFlags.Scenes;
            }
            else if(extension == scriptExtension)
            {
                // Update flags script
                contentFlags |= DLCContentFlags.Scripts;
            }
            else
            {
                // Must be a shared asset
                this.mainAsset = AssetDatabase.LoadAssetAtPath(relativePath, typeof(Object));
                this.mainAssetType = mainAsset != null ? mainAsset.GetType() : null;

                // Update flags shared asset
                contentFlags |= DLCContentFlags.Assets;
            }
        }

        // Methods
        public void ExcludeFromDLC()
        {
            isExcluded = true;
        }
    }
}
