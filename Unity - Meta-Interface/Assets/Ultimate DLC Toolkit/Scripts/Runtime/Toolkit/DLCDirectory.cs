using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace DLCToolkit
{
    /// <summary>
    /// A utility class for mounting a specific folder path as a designated DLC install folder.
    /// Contains useful helper methods to find and access the DLC contents included in the target folder.
    /// Note that only valid DLC files/information will be returned using this API (<see cref="DLC.IsDLCFile(string)"/> = true), even if the folder contains additional non-DLC files.
    /// There is also support for detecting when DLC files are added or removed from the target folder.
    /// </summary>
    public sealed class DLCDirectory
    {
        // Events
        /// <summary>
        /// Called when a valid DLC format was installed in this directory.
        /// Note that events will only be called when `raiseFileEvents` is enabled in the constructor.
        /// </summary>
        public UnityEvent<string> OnDLCFileAdded = new UnityEvent<string>();
        /// <summary>
        /// Called when a valid DLC format was uninstalled from this directory.
        /// Note that events will only be called when `raiseFileEvents` is enabled in the constructor.
        /// </summary>
        public UnityEvent<string> OnDLCFileDeleted = new UnityEvent<string>();

        // Private
        private string dlcDirectoryPath = "";
        private SearchOption searchOption = 0;
        private string ext = null;

        private FileSystemWatcher watcher = null;

        // Properties
        /// <summary>
        /// Get the total number of valid DLC files installed in this directory.
        /// </summary>
        public int DLCCount
        {
            get
            {
                int count = 0;

                // Check all files
                foreach (string file in Directory.EnumerateFiles(dlcDirectoryPath, "*" + ext, searchOption))
                {
                    // Attempt to load metadata
                    if (DLC.IsDLCFile(file) == true)
                        count++;
                }
                return count;
            }
        }

        // Constructor
        /// <summary>
        /// Create new instance for the target directory.
        /// Note that the specified folder path must already exist or an exception will be thrown.
        /// </summary>
        /// <param name="dlcDirectoryPath">The directory path where DLC files may be installed</param>
        /// <param name="option">The search option to use when scanning for DLC content</param>
        /// <param name="extension">An optional extension for DLC content to narrow the search</param>
        /// <param name="raiseFileEvents">Should the directory trigger events when DLC files are added and removed</param>
        /// <exception cref="ArgumentException">The specified path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">The specified directory does not exist</exception>
        /// <exception cref="NotSupportedException">DLC directory is not supported on the current platform</exception>
        public DLCDirectory(string dlcDirectoryPath, SearchOption option = SearchOption.TopDirectoryOnly, string extension = null, bool raiseFileEvents = false)
        {
            // Check platform
#if !UNITY_EDITOR && UNITY_WEBGL
            throw new NotSupportedException("DLC Directory is not supported on this platform");
#else
            // Check for invalid folder
            if (string.IsNullOrEmpty(dlcDirectoryPath) == true)
                throw new ArgumentException(nameof(dlcDirectoryPath) + " cannot be null or empty");

            // Check for exists
            if (Directory.Exists(dlcDirectoryPath) == false)
                throw new DirectoryNotFoundException("The specified directory path does not exist");

            // Check for extension
            if(extension == null)
            {
                this.ext = ".*";
            }
            else
            {
                // Check for valid extension
                if (string.IsNullOrEmpty(extension) == true)
                    throw new ArgumentException(nameof(extension) + " cannot be empty");

                // Check for starting character
                if (extension[0] != '.')
                    throw new ArgumentException(nameof(extension) + " must start with the character '.'");

                this.ext = extension;
            }

            this.dlcDirectoryPath = dlcDirectoryPath;
            this.searchOption = option;


            // Check for file events enabled
            if (raiseFileEvents == true)
            {
                // Create watcher
                this.watcher = new FileSystemWatcher(dlcDirectoryPath, "*" + ext);
                this.watcher.EnableRaisingEvents = true;

                // Listen for files create
                this.watcher.Created += (object sender, FileSystemEventArgs e) =>
                {
                    // Check for dlc file
                    if (DLC.IsDLCFile(e.FullPath) == true)
                        OnDLCFileAdded.Invoke(e.FullPath);
                };

                // Listen for files destroyed
                this.watcher.Deleted += (object sender, FileSystemEventArgs e) =>
                {
                    // Check for dlc file
                    if (DLC.IsDLCFile(e.FullPath) == true)
                        OnDLCFileDeleted.Invoke(e.FullPath);
                };
            }
#endif
        }

        // Methods
        /// <summary>
        /// Override implementation of ToString.
        /// </summary>
        /// <returns>The string representation of the DLCDirectory</returns>
        public override string ToString()
        {
            return string.Format("{0}({1})", nameof(DLCDirectory), dlcDirectoryPath); 
        }

        /// <summary>
        /// Check if a valid DLC with the specified name and optional version is installed in this directory.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">The optional version of the DLC if an explicit match is required</param>
        /// <returns>True if the DLC is installed or false if not</returns>
        public bool HasDLC(string name, Version version = null)
        {
            // Check files with name
            foreach (string file in EnumerateDLCFiles(name))
            {
                // Attempt to load metadata only
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Check for matching name and version
                if (metadata != null && metadata.NameInfo.Name == name)
                {
                    // Check for matching version
                    if (version == null || metadata.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a valid DLC with the specified unique ey is installed in this directory.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>True if the DLC is installed or false if not</returns>
        public bool HasDLC(string uniqueKey)
        {
            // Check all files
            foreach (string file in EnumerateDLCFiles())
            {
                // Attempt to load metadata only
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Check for matching unique key
                if (metadata != null && metadata.NameInfo.UniqueKey == uniqueKey)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get file paths for all valid installed DLC content.
        /// </summary>
        /// <param name="searchName">An optional search name to narrow the search</param>
        /// <returns>An array of file paths for all installed DLC content</returns>
        public string[] GetDLCFiles(string searchName = null)
        {
            // Get search string
            string search = string.IsNullOrEmpty(searchName) == false 
                ? searchName + ext 
                : "*" + ext;

            // Try to find files
            string[] files = Directory.GetFiles(dlcDirectoryPath, search, searchOption);

            // Create result collection
            List<string> result = new List<string>(files.Length);

            // Only interested in DLC files
            for(int i = 0; i <  files.Length; i++)
            {
                // Register dlc file
                if (DLC.IsDLCFile(files[i]) == true)
                    result.Add(files[i]);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Enumerate file paths for all valid installed DLC content.
        /// </summary>
        /// <param name="searchName">An optional search name to narrow the search</param>
        /// <returns>An enumerable for all installed DLC files</returns>
        public IEnumerable<string> EnumerateDLCFiles(string searchName = null)
        {
            // Get search string
            string search = string.IsNullOrEmpty(searchName) == false
                ? searchName + ext
                : "*" + ext;

            // Check all files
            foreach(string file in Directory.EnumerateFiles(dlcDirectoryPath, search, searchOption))
            {
                // Only interested in dlc files
                if (DLC.IsDLCFile(file) == true)
                    yield return file;
            }
        }

        /// <summary>
        /// Get the metadata for all valid installed DLC content.
        /// </summary>
        /// <param name="searchName">An optional search name to narrow the search</param>
        /// <returns>An array of metadata for all installed DLC content</returns>
        public IDLCMetadata[] GetAllDLC(string searchName = null)
        {
            // Get search string
            string search = string.IsNullOrEmpty(searchName) == false
                ? searchName + ext
                : "*" + ext;

            // Try to find files
            string[] files = Directory.GetFiles(dlcDirectoryPath, search, searchOption);

            // Create result collection
            List<IDLCMetadata> result = new List<IDLCMetadata>(files.Length);

            // Only interested in DLC files
            for (int i = 0; i < files.Length; i++)
            {
                // Attempt to load metadata
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(files[i]);

                // Register dlc metadata
                if (metadata != null)
                    result.Add(metadata);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Enumerate the metadata for all valid installed DLC content.
        /// </summary>
        /// <param name="searchName">An optional search name to narrow the search</param>
        /// <returns>An enumerable for all installed DLC metadata</returns>
        public IEnumerable<IDLCMetadata> EnumerateAllDLC(string searchName = null)
        {
            // Get search string
            string search = string.IsNullOrEmpty(searchName) == false
                ? searchName + ext
                : "*" + ext;

            // Check all files
            foreach (string file in Directory.EnumerateFiles(dlcDirectoryPath, search, searchOption))
            {
                // Attempt to load metadata
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Register dlc metadata
                if (metadata != null)
                    yield return metadata;
            }
        }

        /// <summary>
        /// Get the unique keys for all valid installed DLC content.
        /// </summary>
        /// <param name="searchName">An optional search name to narrow the search</param>
        /// <returns>An array of unique keys for all installed DLC content</returns>
        public string[] GetDLCUniqueKeys(string searchName = null)
        {
            // Get search string
            string search = string.IsNullOrEmpty(searchName) == false
                ? searchName + ext
                : "*" + ext;

            // Try to find files
            string[] files = Directory.GetFiles(dlcDirectoryPath, search, searchOption);

            // Create result collection
            List<string> result = new List<string>(files.Length);

            // Only interested in DLC files
            for (int i = 0; i < files.Length; i++)
            {
                // Attempt to load metadata
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(files[i]);

                // Register dlc metadata
                if (metadata != null)
                    result.Add(metadata.NameInfo.UniqueKey);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Enumerate the unique keys for all valid installed DLC content.
        /// </summary>
        /// <param name="searchName">An optional search name to narrow the search</param>
        /// <returns>An enumerable for all installed DLC unique keys</returns>
        public IEnumerable<string> EnumerateDLCUniqueKeys(string searchName = null)
        {
            // Get search string
            string search = string.IsNullOrEmpty(searchName) == false
                ? searchName + ext
                : "*" + ext;

            // Check all files
            foreach (string file in Directory.EnumerateFiles(dlcDirectoryPath, search, searchOption))
            {
                // Attempt to load metadata
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Register dlc metadata
                if (metadata != null)
                    yield return metadata.NameInfo.UniqueKey;
            }
        }

        /// <summary>
        /// Try to get the file path for the DLC with the specified name and optional version.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">The optional version of the DLC if an explicit match is required</param>
        /// <returns>The file path for the DLC if found or null if the DLC is not installed</returns>
        public string GetDLCFile(string name, Version version = null)
        {
            // Check files with name
            foreach(string file in EnumerateDLCFiles(name))
            {
                // Attempt to load metadata only
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Check for matching name and version
                if(metadata != null && metadata.NameInfo.Name == name)
                {
                    // Check for matching version
                    if(version == null || metadata.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return file;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find DLC with version: " + version + " mismatch, nearest version found: " + metadata.NameInfo.Version);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the file path for the DLC with the specified unique key.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>The file path for the DLC if found or null if the DLC is not installed</returns>
        public string GetDLCFile(string uniqueKey)
        {
            // Check all files
            foreach (string file in EnumerateDLCFiles())
            {
                // Attempt to load metadata only
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Check for matching unique key
                if (metadata != null && metadata.NameInfo.UniqueKey == uniqueKey)
                    return file;
            }
            return null;
        }

        /// <summary>
        /// Try to get the metadata for the DLC with the specified name and optional version.
        /// </summary>
        /// <param name="name">The name of the DLC</param>
        /// <param name="version">The optional version of the DLC if an explicit match is required</param>
        /// <returns>The metadata for the DLC if found or null if the DLC is not installed</returns>
        public IDLCMetadata GetDLC(string name, Version version = null)
        {
            // Check files with name
            foreach (string file in EnumerateDLCFiles(name))
            {
                // Attempt to load metadata only
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Check for matching name and version
                if (metadata != null && metadata.NameInfo.Name == name)
                {
                    // Check for matching version
                    if (version == null || metadata.NameInfo.Version.CompareTo(version) == 0)
                    {
                        return metadata;
                    }
                    else
                    {
                        Debug.LogWarning("Attempting to find DLC with version: " + version + " mismatch, nearest version found: " + metadata.NameInfo.Version);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the metadata for the DLC with the specified unique key.
        /// </summary>
        /// <param name="uniqueKey">The unique key for the DLC</param>
        /// <returns>The metadata for the DLC if found or null if the DLC is not installed</returns>
        public IDLCMetadata GetDLC(string uniqueKey)
        {
            // Check all files
            foreach (string file in EnumerateDLCFiles())
            {
                // Attempt to load metadata only
                IDLCMetadata metadata = DLC.LoadDLCMetadataFrom(file);

                // Check for matching unique key
                if (metadata != null && metadata.NameInfo.UniqueKey == uniqueKey)
                    return metadata;
            }
            return null;
        }
    }
}
