﻿
namespace DLCToolkit.DRM
{
    /// <summary>
    /// Provides access to the <see cref="IDRMProvider"/> for the current platform.
    /// </summary>
    public interface IDRMServiceProvider
    {
        // Methods
        /// <summary>
        /// Try to get the <see cref="IDRMProvider"/> for this platform.
        /// </summary>
        /// <returns>A DRM provider or null if DRM is not supported on this platform</returns>
        IDRMProvider GetDRMProvider();
    }
}
