﻿using System;

namespace DLCToolkit.DRM
{
    /// <summary>
    /// Main API to interact with a DLC DRM service independent of the build platform.
    /// </summary>
    public interface IDRMProvider
    {
        // Properties
        /// <summary>
        /// Get all unique id keys for all DLC published via the DRM platform (Steamworks DLC for example).
        /// It is allowed for this property to return an empty array or only a partial array of the potentially available DLC Contents.
        /// <see cref="IsDLCAvailableAsync(IDLCAsyncProvider, string)"/> will be used to determine truly whether DLC content is valid and available at any given time, even if this property does not list it.
        /// Can throw a <see cref="NotSupportedException"/> if the DRM platform does not support listing contents.
        /// </summary>
        DLCAsync<string[]> DLCUniqueKeysAsync { get; }

        // Methods
        /// <summary>
        /// Check if the specified DLC is purchased and installed.
        /// Some providers may need to make a web request to check for purchased DLC, so this operations must be async.
        /// </summary>
        /// <param name="asyncProvider">The async provider to allow async tasks to be started</param>
        /// <param name="uniqueKey">The unique key of the dlc</param>
        /// <returns>True if the dlc is installed or false if not</returns>
        DLCAsync<bool> IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey);

        /// <summary>
        /// Attempt to get the stream provider for the DLC to allow loading.
        /// </summary>
        /// <param name="uniqueKey">The unique key of the dlc</param>
        /// <returns>The stream provider for the dlc if installed or null if it is not available</returns>
        DLCStreamProvider GetDLCStream(string uniqueKey);

        /// <summary>
        /// Request that the dlc with the provided unique key is installed onto the system if it is available to the user.
        /// </summary>
        /// <param name="asyncProvider">The async provider to allow async tasks to be started</param>
        /// <param name="uniqueKey">The unique key of the dlc</param>
        DLCAsync RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey);

        /// <summary>
        /// Request that the dlc with the provided unique key is uninstalled from the system if it is currently installed.
        /// </summary>
        /// <param name="uniqueKey">The unique key of the dlc</param>
        void RequestUninstallDLC(string uniqueKey);

        /// <summary>
        /// Allow for progress tracking/hours tracking from the drm service by setting which dlc is currently being used.
        /// </summary>
        /// <param name="uniqueKey">The unique key of the dlc</param>
        /// <param name="isInUse">True if the game is currently using the dlc or false if not. Used to enable or disable progress tracking</param>
        void TrackDLCUsage(string uniqueKey, bool isInUse);
    }
}
