using System;
using System.IO;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A virtual DRM provider mounted to a local directory which offers DRM like ability without any verification.
    /// Supports listing all DLC unique keys that are available in the specified directory.
    /// Does not support ownership verification and will simply report that any DLC in the specified folder is owned by the user and can be loaded.
    /// Supported on any platform with IO support (Not WebGL for example) and may be useful for free DLC or expansion packs that can be simply dropped into a game folder and be auto-detected.
    /// You can also manually handle this sort of setup using the <see cref="DLCDirectory"/> instead, which is what this DRM provider is using internally, but it may offer you more control for things like install/uninstall events.
    /// </summary>
    public sealed class LocalDirectoryDRM : IDRMProvider
    {
        // Private
        private DLCDirectory directory = null;

        // Properties
        DLCAsync<string[]> IDRMProvider.DLCUniqueKeysAsync
        {
            get { return DLCAsync<string[]>.Completed(true, directory.GetDLCUniqueKeys()); }
        }

        // Constructor
        /// <summary>
        /// Create a new instance from the target folder.
        /// Note that the specified folder must already exist or an exception will be thrown.
        /// </summary>
        /// <param name="localFolderPath">The path where DLC content may be stored</param>
        /// <exception cref="ArgumentException">The folder path is null or empty</exception>
        /// <exception cref="DirectoryNotFoundException">The folder path does not exist</exception>
        public LocalDirectoryDRM(string localFolderPath)
        {
            // Check for empty
            if (string.IsNullOrEmpty(localFolderPath) == true)
                throw new ArgumentException(nameof(localFolderPath) + " cannot be null or empty");
            
            this.directory = new DLCDirectory(localFolderPath);
        }

        // Methods
        DLCAsync<bool> IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            return DLCAsync<bool>.Completed(true, directory.HasDLC(uniqueKey));
        }

        DLCStreamProvider IDRMProvider.GetDLCStream(string uniqueKey)
        {
            string file = directory.GetDLCFile(uniqueKey);

            // Check for null
            if (file != null)
                return DLCStreamProvider.FromFile(file);

            return null;
        }

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            throw new NotSupportedException("Cannot install DLC on this DRM platform");
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            throw new NotSupportedException("Cannot uninstall DLC on this DRM platform");
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Do nothing
        }
    }
}
