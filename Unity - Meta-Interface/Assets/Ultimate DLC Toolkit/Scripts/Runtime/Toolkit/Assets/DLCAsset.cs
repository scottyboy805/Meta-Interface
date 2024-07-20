using DLCToolkit.Format;
using System;
using System.IO;

namespace DLCToolkit.Assets
{
    /// <summary>
    /// Represents a specific asset included in the DLC.
    /// Provides access to useful metadata such as name, path, extension, type and more which can be quickly accessed without forcing the asset to be loaded into memory.
    /// Note that this can represent shared assets and scene assets in the same form.
    /// </summary>
    public abstract class DLCAsset
    {
        // Internal
        internal IDLCAsyncProvider asyncProvider = null;
        internal DLCContentBundle contentBundle = null;
        internal object loadedObject = null;
        internal DLCLoadMode loadMode = DLCLoadMode.Full;

        // Protected
        /// <summary>
        /// The unique ID for this asset.
        /// </summary>
        protected int assetID = -1;
        /// <summary>
        /// The type of this asset.
        /// </summary>
        protected Type assetType = null;
        /// <summary>
        /// The full name relative to the original Unity project assets folder of the asset.
        /// </summary>
        protected string fullName = string.Empty; // Full path relative to assets including extension - used for loading
        /// <summary>
        /// The relative name relative to the original DLC content folder of the asset.
        /// </summary>
        protected string relativeName = string.Empty; // Path relative to mod folder without extension
        /// <summary>
        /// The name of the asset.
        /// </summary>
        protected string name = string.Empty;
        /// <summary>
        /// The file extension of the asset.
        /// </summary>
        protected string extension = string.Empty;        

        // Properties
        /// <summary>
        /// Check if the asset is currently loaded in memory.
        /// </summary>
        public bool IsLoaded
        {
            get { return loadedObject != null; }
        }

        /// <summary>
        /// Check if the asset can be loaded into memory.
        /// Only possible when the DLC content has been fully loaded as opposed to partially loaded using a method such as <see cref="DLC.LoadDLCMetadataWithAssets(string)"/>.
        /// </summary>
        public bool IsLoadable
        {
            get { return loadMode == DLCLoadMode.Full; }
        }

        /// <summary>
        /// Check if the containing bundle is currently loaded in memory.
        /// If false, any load requests issued may take longer than expected as the containing bundle will first need to be loaded.
        /// </summary>
        public bool IsBundleLoaded
        {
            get { return contentBundle.IsLoaded; }
        }

        /// <summary>
        /// The unique id for this asset.
        /// This is guaranteed to be unique during the current session.
        /// Id's are not persistent.
        /// </summary>
        public int AssetID
        {
            get { return assetID; }
        }

        /// <summary>
        /// Get the type of the main asset.
        /// For example this value will be `GameObject` when dealing with a prefab asset, or `Texture2D` when dealing with a texture.
        /// </summary>
        public Type AssetMainType
        {
            get { return assetType; }
        }

        /// <summary>
        /// Get the full name of the asset.
        /// The full name is defined as the asset path relative the the export project folder including the asset extension, For example: 'assets/mydlc/subfolder/cube.prefab'.
        /// </summary>
        public string FullName
        {
            get { return fullName; }
        }

        /// <summary>
        /// Get the relative name of the asset.
        /// The relative name is defined as the asset path relative to the mod folder without the asset extension, For example: 'subfolder/cube'.
        /// If the asset is not in a sub folder then the value wil be equal to <see cref="Name"/>. 
        /// </summary>
        public string RelativeName
        {
            get { return relativeName; }
        }

        /// <summary>
        /// Get the name of the asset.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// Get the extension of the asset, For example: '.prefab'.
        /// The extension will always begin with a '.'.
        /// </summary>
        public string Extension
        {
            get { return extension; }
        }

        // Constructor
        internal DLCAsset(IDLCAsyncProvider asyncProvider, DLCContentBundle contentBundle, DLCLoadMode loadMode, int assetID, Type type, string fullName, string relativeName)
        {
            this.asyncProvider = asyncProvider;
            this.contentBundle = contentBundle;
            this.loadMode = loadMode;

            this.assetID = assetID;
            this.assetType = type;
            this.relativeName = relativeName;
            this.fullName = fullName;

            // Find asset details
            this.name = Path.GetFileNameWithoutExtension(fullName);
            this.extension = Path.GetExtension(fullName);            
        }

        // Methods
        /// <summary>
        /// Convert to string representation.
        /// </summary>
        /// <returns>This asset as a string</returns>
        public override string ToString()
        {
            return string.Format("DLCAsset({0})", relativeName);
        }

        /// <summary>
        /// Check if this asset is of the specified type.
        /// Note that this will not check for derived types and you can use <see cref="IsAssetSubType(Type)"/> instead.
        /// </summary>
        /// <param name="type">The type to check for equality</param>
        /// <returns>True if the asset is of specified type of false if not</returns>
        public bool IsAssetType(Type type)
        {
            return assetType == type;
        }

        /// <summary>
        /// Check if this asset is of the specified generic type.
        /// Note that this will not check for derived types and you can use <see cref="IsAssetSubType{T}()"/> instead.
        /// </summary>
        /// <typeparam name="T">The generic type to check for equality</typeparam>
        /// <returns>True if the asset is of specified generic type of false if not</returns>
        public bool IsAssetType<T>()
        {
            return assetType == typeof(T);
        }

        /// <summary>
        /// Check if this asset is of the specified type or is derived from the specified type.
        /// For example: A `Texture2D` asset will return true if `UnityEngine.Object` is passed as the parameter type, since textures derive from the base object type.
        /// </summary>
        /// <param name="type">The type to check for equality or derivation</param>
        /// <returns>True if the asset is of the specified type, or is derived from the specified type otherwise false</returns>
        public bool IsAssetSubType(Type type)
        {
            return type.IsAssignableFrom(assetType);
        }

        /// <summary>
        /// Check if this asset is of the specified generic type or is derived from the specified type.
        /// For example: A `Texture2D` asset will return true if `UnityEngine.Object` is passed as the parameter type, since textures derive from the base object type.
        /// </summary>
        /// <typeparam name="T">The generic type to check for equality or derivation</typeparam>
        /// <returns>True if the asset is of the specified generic type, or is derived from the specified type otherwise false</returns>
        public bool IsAssetSubType<T>()
        {
            return typeof(T).IsAssignableFrom(assetType);
        }
    }
}
