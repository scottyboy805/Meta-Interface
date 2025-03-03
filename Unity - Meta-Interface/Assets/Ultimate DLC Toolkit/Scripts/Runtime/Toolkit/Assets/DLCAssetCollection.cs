using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DLCToolkit.Assets
{
    /// <summary>
    /// Represents a collection of <see cref="DLCAsset"/> which are stored in a given DLC.
    /// Note that this collection can contain either shared assets or scene assets, but they will not be stored together.
    /// <see cref="DLCContent.SharedAssets"/> provides access to all shared assets for the DLC, and <see cref="DLCContent.SceneAssets"/> provides access to all scene assets.
    /// </summary>
    /// <typeparam name="T">The generic type of <see cref="DLCAsset"/> to store in this collection</typeparam>
    public class DLCAssetCollection<T> : IEnumerable<T> where T : DLCAsset
    {
        // Private
        private const bool ignoreCase = true;
        private List<T> assets = null;

        // Properties
        /// <summary>
        /// Get the total number of shared assets that are available in this DLC.
        /// </summary>
        public int AssetCount
        {
            get { return assets.Count; }
        }

        // Constructor
        internal DLCAssetCollection(List<T> assets)
        {
            this.assets = assets;
        }

        // Methods
        /// <summary>
        /// Check if an asset with the specified name or path exists in the DLC.
        /// </summary>
        /// <param name="nameOfPath">The asset name or path</param>
        /// <returns>True if the asset exists or false if not</returns>
        public bool Exists(string nameOfPath)
        {
            // Check if the asset can be found
            return Find(nameOfPath) != null;
        }

        /// <summary>
        /// Check if an asset with the specified id exists in the DLC.
        /// </summary>
        /// <param name="assetID">The asset ID to check</param>
        /// <returns>True if the asset exists or false if not</returns>
        public bool Exists(int assetID)
        {
            // Check if the asset can be found
            return Find(assetID) != null;
        }

        #region FindAll
        /// <summary>
        /// Find the names of all assets that are available in this DLC.
        /// </summary>
        /// <returns>An array of asset names or an empty array if no assets are available</returns>
        public string[] FindAllNames()
        {
            // Create the return array
            string[] all = new string[assets.Count];

            // Get the name of each asset
            for (int i = 0; i < all.Length; i++)
                all[i] = assets[i].Name;

            return all;
        }

        /// <summary>
        /// Find the relative names of all assets that are available in this DLC.
        /// The relative name is the asset path relative to the DLC content folder.
        /// </summary>
        /// <returns>An array of asset relative names or an empty array if no assets are available</returns>
        public string[] FindAllRelativeNames()
        {
            // Create the return array
            string[] all = new string[assets.Count];

            // Get the relative name of each asset
            for (int i = 0; i < all.Length; i++)
                all[i] = assets[i].RelativeName;

            return all;
        }

        /// <summary>
        /// Find all assets that are available in this DLC.
        /// </summary>
        /// <returns>An array of assets or an empty array if no assets are available</returns>
        public T[] FindAll()
        {
            // Make a clone so that we don't invalidate our copy
            return assets.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC of the specified type.
        /// Note that this will check for type equality and not sub types. Use <see cref="FindAllSubTypesOf(Type)"/> if you want to discover derived types too.
        /// </summary>
        /// <param name="type">The type of asset to find, for example: <see cref="Texture2D"/></param>
        /// <returns>An array of assets matching the specified type or an empty array if no matching assets are available</returns>
        public T[] FindAllOfType(Type type)
        {
            // List of results
            List<T> match = new List<T>();

            // Check all assets
            foreach(T asset in assets)
            {
                if(asset.IsAssetType(type) == true)
                    match.Add(asset);
            }

            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC of the specified generic type.
        /// Note that this will check for type equality and not sub types. Use <see cref="FindAllSubTypesOf{TType}()"/> if you want to discover derived types too.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset to find, for example: <see cref="Texture2D"/></typeparam>
        /// <returns>An array of assets matching the specified generic type or an empty array if no matching assets are available</returns>
        public T[] FindAllOfType<TType>()
        {
            return FindAllOfType(typeof(TType));
        }

        /// <summary>
        /// Find all assets that are available in this DLC with are of the specified type, or derived from the specified type.
        /// Supports discovering derived types, for example: Searching for <see cref="Texture"/> type will also return derived <see cref="Texture2D"/> types.
        /// </summary>
        /// <param name="type">The type of asset or derived asset to find</param>
        /// <returns>An array of assets matching or derived from the specified type or an empty array if no matching assets are available</returns>
        public T[] FindAllSubTypesOf(Type type)
        {
            // List of results
            List<T> match = new List<T>();

            // Check all assets
            foreach (T asset in assets)
            {
                if (asset.IsAssetSubType(type) == true)
                    match.Add(asset);
            }

            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC with are of the specified generic type, or derived from the specified generic type.
        /// Supports discovering derived types, for example: Searching for <see cref="Texture"/> type will also return derived <see cref="Texture2D"/> types.
        /// </summary>
        /// <typeparam name="TType">The type of asset or derived asset to find</typeparam>
        /// <returns>An array of assets matching or derived from the specified generic type or an empty array if no matching assets are available</returns>
        public T[] FindAllSubTypesOf<TType>()
        {
            return FindAllSubTypesOf(typeof(TType));
        }

        /// <summary>
        /// Find all assets that are available in this DLC with the specified name.
        /// Note that this method may return multiple results with the same name if they are located in different folders.
        /// </summary>
        /// <param name="name">The name of asset to find</param>
        /// <returns>An array of matching assets or an empty array if no matches are available</returns>
        /// <exception cref="ArgumentException">The specified name was null or empty</exception>
        public T[] FindAllWithName(string name)
        {
            // Check for invalid name
            if (string.IsNullOrEmpty(name) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(name));

            List<T> match = new List<T>();

            // Check all assets
            foreach (T asset in assets)
            {
                // Check for assets with matching name
                if (string.Compare(asset.Name, name, ignoreCase) == 0)
                {
                    // Add the asset to match
                    match.Add(asset);
                }
            }

            // Get as array
            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC with the specified file extension.
        /// </summary>
        /// <param name="extension">The file extension to find</param>
        /// <returns>An array of matching assets or an empty array if no matches are available</returns>
        /// <exception cref="ArgumentException">The Specified extension was null or empty</exception>
        /// <exception cref="ArgumentException">The specified extension was incorrectly formatted and did not start with a leading '.' character</exception>
        public T[] FindAllWithExtension(string extension)
        {
            // Check for invalid extension
            if (string.IsNullOrEmpty(extension) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(extension));

            // Check for '.'
            if (extension[0] != '.')
                throw new ArgumentException("Value must be a valid extension including '.' at beginning", nameof(extension));

            List<T> match = new List<T>();

            // Check all assets
            foreach (T asset in assets)
            {
                // Check for assets with matching extension
                if (string.Compare(asset.Extension, extension, ignoreCase) == 0)
                {
                    // Add the asset to match
                    match.Add(asset);
                }
            }

            // Get as array
            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC and are located in the specified folder.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An array of assets that are in the specified folder or an empty array if no matches were found</returns>
        public T[] FindAllInFolder(string path)
        {
            // If the path is empty then treat it as the root
            if (string.IsNullOrEmpty(path) == true)
                return FindAll();

            List<T> match = new List<T>();

            // Check for assets in folder
            foreach (T asset in assets)
            {
                // Check if the asset relative path contains the specified path at the start
                if (IsAssetInFolder(asset.FullName, path) == true)
                {
                    // Add the asset to match
                    match.Add(asset);
                }
                else if (IsAssetInFolder(asset.RelativeName, path) == true)
                {
                    match.Add(asset);
                }
            }

            // Get as array
            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC and are located in the specified folder and are of the specified type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// Note that this will check for type equality and will not discover derived types. If you want to find derived types in addition, you should use <see cref="FindAllSubTypesOfInFolder(Type, string)"/>.
        /// </summary>
        /// <param name="type">The type of asset to find</param>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An array of assets of the specified type that are located in the specified folder or an empty array if no matches were found</returns>
        public T[] FindAllOfTypeInFolder(Type type, string path)
        {
            // If the path is empty then treat it as the root
            bool empty = string.IsNullOrEmpty(path);

            List<T> match = new List<T>();

            // Check for assets in folder
            foreach (T asset in assets)
            {
                if (asset.IsAssetType(type) == true)
                {
                    // Check for empty
                    if (empty == true)
                    {
                        match.Add(asset);
                    }
                    // Check if the asset relative path contains the specified path at the start
                    else if (IsAssetInFolder(asset.FullName, path) == true)
                    {
                        // Add the asset to match
                        match.Add(asset);
                    }
                    else if (IsAssetInFolder(asset.RelativeName, path) == true)
                    {
                        match.Add(asset);
                    }
                }
            }

            // Get as array
            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC and are located in the specified folder and are of the specified generic type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// Note that this will check for type equality and will not discover derived types. If you want to find derived types in addition, you should use <see cref="FindAllSubTypesOfInFolder{TType}(string)"/>.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset to find</typeparam>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An array of assets of the specified generic type that are located in the specified folder or an empty array if no matches were found</returns>
        public T[] FindAllOfTypeInFolder<TType>(string path)
        {
            return FindAllOfTypeInFolder(typeof(TType), path);
        }

        /// <summary>
        /// Find all assets that are available in this DLC and are located in the specified folder and are of the specified type or derived from the specified type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <param name="type">The type of asset to find</param>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An array of assets of the specified type or derived from the specified type that are located in the specified folder or an empty array if no matches were found</returns>
        public T[] FindAllSubTypesOfInFolder(Type type, string path)
        {
            // If the path is empty then treat it as the root
            bool empty = string.IsNullOrEmpty(path);

            List<T> match = new List<T>();

            // Check for assets in folder
            foreach (T asset in assets)
            {
                if (asset.IsAssetSubType(type) == true)
                {
                    // Check for empty
                    if (empty == true)
                    {
                        match.Add(asset);                            
                    }
                    // Check if the asset relative path contains the specified path at the start
                    else if (IsAssetInFolder(asset.FullName, path) == true)
                    {
                        // Add the asset to match
                        match.Add(asset);
                    }
                    else if (IsAssetInFolder(asset.RelativeName, path) == true)
                    {
                        match.Add(asset);
                    }
                }
            }

            // Get as array
            return match.ToArray();
        }

        /// <summary>
        /// Find all assets that are available in this DLC and are located in the specified folder and are of the specified generic type or derived from the specified generic type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset to find</typeparam>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An array of assets of the specified generic type or derived from the specified generic type that are located in the specified folder or an empty array if no matches were found</returns>
        public T[] FindAllSubTypesOfInFolder<TType>(string path)
        {
            return FindAllSubTypesOfInFolder(typeof(TType), path);
        }

        /// <summary>
        /// Find all assets that are located in the specified folder and has the specified file extension.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <param name="path">The folder path to search in</param>
        /// <param name="extension">The file extension to find</param>
        /// <returns>An array of assets in the specified folder with the specified extension or an empty array if no matches were found</returns>
        /// <exception cref="ArgumentException">The Specified extension was null or empty</exception>
        /// <exception cref="ArgumentException">The specified extension was incorrectly formatted and did not start with a leading '.' character</exception>
        public T[] FindAllInFolderWithExtension(string path, string extension)
        {
            // Check for invalid extension
            if (string.IsNullOrEmpty(extension) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(extension));

            // Check for '.'
            if (extension[0] != '.')
                throw new ArgumentException("Value must be a valid extension including '.' at beginning", nameof(extension));

            List<T> match = new List<T>();

            // Check filtered assets
            foreach (T asset in FindAllInFolder(path))
            {
                // Check for assets with matching extension
                if (string.Compare(asset.Extension, extension, ignoreCase) == 0)
                {
                    // Add the asset to match
                    match.Add(asset);
                }
            }

            // Get as array
            return match.ToArray();
        }
        #endregion

        #region EnumerateAll
        /// <summary>
        /// Enumerate the names of all assets that are available in this DLC.
        /// </summary>
        /// <returns>An enumerable of asset names</returns>
        public IEnumerable<string> EnumerateAllNames()
        {
            // Get the name of each asset
            foreach (T asset in assets)
                yield return asset.Name;
        }

        /// <summary>
        /// Enumerate the relative names of all assets that are available in this DLC.
        /// The relative name is the asset path relative to the DLC content folder.
        /// </summary>
        /// <returns>An enumerable of asset relative names</returns>
        public IEnumerable<string> EnumerateAllRelativeNames()
        {
            // Get the name of each asset
            foreach (T asset in assets)
                yield return asset.RelativeName;
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC.
        /// </summary>
        /// <returns>An enumerable of assets</returns>
        public IEnumerable<T> EnumerateAll()
        {
            return assets;
        }

        /// <summary>
        /// Enumerate all assets that are available in ths DLC of the specified type.
        /// Note that this will check for type equality and not sub types. Use <see cref="EnumerateAllSubTypesOf(Type)"/> if you want to discover derived types too.
        /// </summary>
        /// <param name="type">The type of asset to find, for example: <see cref="Texture2D"/></param>
        /// <returns>An enumerable of assets of the specified type</returns>
        public IEnumerable<T> EnumerateAllOfType(Type type)
        {
            // Check all assets
            foreach (T asset in assets)
            {
                if (asset.IsAssetType(type) == true)
                    yield return asset;
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in ths DLC of the specified generic type.
        /// Note that this will check for type equality and not sub types. Use <see cref="EnumerateAllSubTypesOf{TType}()"/> if you want to discover derived types too.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset to find, for example: <see cref="Texture2D"/></typeparam>
        /// <returns>An enumerable of assets of the specified generic type</returns>
        public IEnumerable<T> EnumerateAllOfType<TType>()
        {
            return EnumerateAllOfType(typeof(TType));
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC which are of the specified type, or derived from the specified type.
        /// Supports discovering derived types, for example: Searching for <see cref="Texture"/> type will also return derived <see cref="Texture2D"/> types.
        /// </summary>
        /// <param name="type">The type of asset or derived asset to find</param>
        /// <returns>An enumerable of assets matching or derived from the specified type</returns>
        public IEnumerable<T> EnumerateAllSubTypesOf(Type type)
        {
            // Check all assets
            foreach (T asset in assets)
            {
                if (asset.IsAssetSubType(type) == true)
                    yield return asset;
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC which are of the specified type, or derived from the specified generic type.
        /// Supports discovering derived types, for example: Searching for <see cref="Texture"/> type will also return derived <see cref="Texture2D"/> types.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset or derived asset to find</typeparam>
        /// <returns>An enumerable of assets matching or derived from the specified generic type</returns>
        public IEnumerable<T> EnumerateAllSubTypesOf<TType>()
        {
            return EnumerateAllSubTypesOf(typeof(TType));
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC with the specified name.
        /// Note that this method may return multiple results with the same name if they are located in different folders.
        /// </summary>
        /// <param name="name">The name of the asset to find</param>
        /// <returns>An enumerable of assets with the specified name</returns>
        /// <exception cref="ArgumentException">The specified name was null or empty</exception>
        public IEnumerable<T> EnumerateAllWithName(string name)
        {
            // Check for invalid name
            if (string.IsNullOrEmpty(name) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(name));

            // Check all assets
            foreach (T asset in assets)
            {
                // Check for assets with matching name
                if (string.Compare(asset.Name, name, ignoreCase) == 0)
                {
                    // Add the asset to match
                    yield return asset;
                }
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC with the specified file extension.
        /// </summary>
        /// <param name="extension">The file extension to find</param>
        /// <returns>An enumerable of assets with the specified extension</returns>
        /// <exception cref="ArgumentException">The Specified extension was null or empty</exception>
        /// <exception cref="ArgumentException">The specified extension was incorrectly formatted and did not start with a leading '.' character</exception>
        public IEnumerable<T> EnumerateAllWithExtension(string extension)
        {
            // Check for invalid extension
            if (string.IsNullOrEmpty(extension) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(extension));

            // Check for '.'
            if (extension[0] != '.')
                throw new ArgumentException("Value must be a valid extension including '.' at beginning", nameof(extension));

            // Check all assets
            foreach (T asset in assets)
            {
                // Check for assets with matching extension
                if (string.Compare(asset.Extension, extension, ignoreCase) == 0)
                {
                    // Add the asset to match
                    yield return asset;
                }
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC and are located in the specified folder.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An enumerable of assets that are in the specified folder</returns>
        public IEnumerable<T> EnumerateAllInFolder(string path)
        {
            // If the path is empty then treat it as the root
            bool empty = string.IsNullOrEmpty(path);

            // Check for assets in folder
            foreach (T asset in assets)
            {
                // Check for empty path
                if (empty == true)
                {
                    yield return asset;
                }
                // Check if the asset relative path contains the specified path at the start
                else if (IsAssetInFolder(asset.FullName, path) == true)
                {
                    // Add the asset to match
                    yield return asset;
                }
                else if (IsAssetInFolder(asset.RelativeName, path) == true)
                {
                    yield return asset;
                }
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC and are located in the specified folder and are of the specified type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// Note that this will check for type equality and will not discover derived types. If you want to find derived types in addition, you should use <see cref="EnumerateAllSubTypesOfInFolder(Type, string)"/>.
        /// </summary>
        /// <param name="type">The type of asset to find</param>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An enumerable of assets of the specified type that are located in the specified folder</returns>
        public IEnumerable<T> EnumerateAllOfTypeInFolder(Type type, string path)
        {
            // If the path is empty then treat it as the root
            bool empty = string.IsNullOrEmpty(path);

            // Check for assets in folder
            foreach (T asset in assets)
            {
                if (asset.IsAssetType(type) == true)
                {
                    // Check for empty
                    if (empty == true)
                    {
                        yield return asset;
                    }
                    // Check if the asset relative path contains the specified path at the start
                    else if (IsAssetInFolder(asset.FullName, path) == true)
                    {
                        // Add the asset to match
                        yield return asset;
                    }
                    else if (IsAssetInFolder(asset.RelativeName, path) == true)
                    {
                        yield return asset;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC and are located in the specified folder and are of the specified generic type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// Note that this will check for type equality and will not discover derived types. If you want to find derived types in addition, you should use <see cref="EnumerateAllOfTypeInFolder{TType}(string)"/>.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset to find</typeparam>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An enumerable of assets of the specified generic type that are located in the specified folder</returns>
        public IEnumerable<T> EnumerateAllOfTypeInFolder<TType>(string path)
        {
            return EnumerateAllOfTypeInFolder(typeof(TType), path);
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC and are located in the specified folder and are of the specified type or derived from the specified type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <param name="type">The type of asset to find</param>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An enumerable of assets of the specified type or derived from the specified type that are located in the specified folder</returns>
        public IEnumerable<T> EnumerateAllSubTypesOfInFolder(Type type, string path)
        {
            // If the path is empty then treat it as the root
            bool empty = string.IsNullOrEmpty(path);

            // Check for assets in folder
            foreach (T asset in assets)
            {
                if (asset.IsAssetSubType(type) == true)
                {
                    // Check for empty
                    if(empty == true)
                        yield return asset;

                    // Check if the asset relative path contains the specified path at the start
                    if (IsAssetInFolder(asset.FullName, path) == true)
                    {
                        // Add the asset to match
                        yield return asset;
                    }
                    else if (IsAssetInFolder(asset.RelativeName, path) == true)
                    {
                        yield return asset;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerate all assets that are available in this DLC and are located in the specified folder and are of the specified type or derived from the specified generic type.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <typeparam name="TType">The generic type of asset to find</typeparam>
        /// <param name="path">The folder path to search in</param>
        /// <returns>An enumerable of assets of the specified generic type or derived from the specified type that are located in the specified folder</returns>
        public IEnumerable<T> EnumerateAllSubTypesOfInFolder<TType>(string path)
        {
            return EnumerateAllSubTypesOfInFolder(typeof(TType), path);
        }

        /// <summary>
        /// Enumerate all assets that are located in ths specified folder and has the specified file extension.
        /// The folder path should either be relative to the DLC content folder, or relative to the Unity assets folder.
        /// </summary>
        /// <param name="path">The folder path to search in</param>
        /// <param name="extension">The file extension to find</param>
        /// <returns>An enumerable of assets in the specified folder with the specified extension</returns>
        /// <exception cref="ArgumentException">The Specified extension was null or empty</exception>
        /// <exception cref="ArgumentException">The specified extension was incorrectly formatted and did not start with a leading '.' character</exception>
        public IEnumerable<T> EnumerateAllInFolderWithExtension(string path, string extension)
        {
            // Check for invalid extension
            if (string.IsNullOrEmpty(extension) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(extension));

            // Check for '.'
            if (extension[0] != '.')
                throw new ArgumentException("Value must be a valid extension including '.' at beginning", nameof(extension));

            // Check filtered assets
            foreach (T asset in FindAllInFolder(path))
            {
                // Check for assets with matching extension
                if (string.Compare(asset.Extension, extension, ignoreCase) == 0)
                {
                    // Add the asset to match
                    yield return asset;
                }
            }
        }
        #endregion

        /// <summary>
        /// Try to find the asset with the specified name or path.
        /// Note that if searching by name and multiple assets with the same name exist in the DLC, this method will return the first asset found during the search.
        /// </summary>
        /// <param name="nameOrPath">The name or path of the asset</param>
        /// <returns>An asset if found or null</returns>
        /// <exception cref="ArgumentException">The nameOrPath is null or empty</exception>
        public T Find(string nameOrPath)
        {
            // Check for invalid string
            if (string.IsNullOrEmpty(nameOrPath) == true)
                throw new ArgumentException("Value cannot be null or empty", nameof(nameOrPath));

            // Get the extension of the specified path
            string ext = Path.GetExtension(nameOrPath);

            // Check for extension
            bool hasExtension = string.IsNullOrEmpty(ext) == false;

            // Remove the extension - It will still be checked later if required
            nameOrPath = Path.ChangeExtension(nameOrPath, null);

            foreach (T asset in assets)
            {
                // Check for extension
                if (hasExtension == false)
                {
                    string fullName = asset.FullName;
                    string relativeName = asset.RelativeName;

                    // Check for extension
                    if (string.IsNullOrEmpty(ext) == true)
                    {
                        fullName = Path.ChangeExtension(fullName, null);
                        relativeName = Path.ChangeExtension(relativeName, null);
                    }

                    // Check for full path
                    if (string.Compare(fullName, nameOrPath, ignoreCase) == 0)
                        return asset;

                    // Check for relative path
                    if (string.Compare(relativeName, nameOrPath, ignoreCase) == 0)
                        return asset;
                }
                else
                {
                    // Check for full path
                    if (string.Compare(asset.FullName, nameOrPath + ext, ignoreCase) == 0)
                        return asset;

                    // Check for relative path
                    if (string.Compare(asset.RelativeName, nameOrPath + ext, ignoreCase) == 0)
                    {
                        // Check for no extension
                        if (string.IsNullOrEmpty(ext) == true)
                            return asset;

                        // We need to compare extension too
                        if (string.Compare(asset.Extension, ext, ignoreCase) == 0)
                            return asset;
                    }
                }

                // Check for name only
                if (string.Compare(asset.Name, nameOrPath, ignoreCase) == 0)
                {
                    // Check for no extension
                    if (string.IsNullOrEmpty(ext) == true)
                        return asset;

                    // We need to compare extension too
                    if (string.Compare(asset.Extension, ext, ignoreCase) == 0)
                        return asset;
                }
            }

            // No matching asset found
            return default(T);
        }

        /// <summary>
        /// Try to find the asset with the specified asset ID.
        /// </summary>
        /// <param name="assetID">The id of the asset</param>
        /// <returns>An asset if found or null if not</returns>
        public T Find(int assetID)
        {
            // Validate index
            if (assetID < 0 || assetID >= assets.Count)
                return default(T);

            // Asset id maps to array index for better performance
            return assets[assetID];
        }

        internal static bool IsAssetInFolder(string assetPath, string folderPath)
        {
            string temp = assetPath;

            // Remove trailing slash if there is any
            if (folderPath[folderPath.Length - 1] == '/')
                folderPath = folderPath.Remove(folderPath.Length - 1);

            // Make sure the path starts at 0
            if (temp.IndexOf(folderPath) == 0)
            {
                // Remove the folder path
                temp = temp.Remove(folderPath.Length + 1);

                // Expect a folder separator next
                if (string.IsNullOrEmpty(temp) == false)
                {
                    // The asset path is inside the folder
                    if (temp[temp.Length - 1] == '/')
                        return true;
                }
            }

            return false;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)assets).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return assets.GetEnumerator();
        }

        internal static DLCAssetCollection<T> Empty()
        {
            return new DLCAssetCollection<T>(new List<T>(0));
        }
    }
}
