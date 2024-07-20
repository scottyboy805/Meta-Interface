﻿using UnityEngine;

namespace DLCToolkit
{
    /// <summary>
    /// The built in icon size that we need to access.
    /// </summary>
    public enum DLCIconType
    {
        /// <summary>
        /// Access the small icon for the DLC.
        /// Usually between 16 - 32px square in size.
        /// </summary>
        Small,
        /// <summary>
        /// Access the medium icon for the DLC.
        /// Usually between 32 - 64px square in size.
        /// </summary>
        Medium,
        /// <summary>
        /// Access the large icon for the DLC.
        /// Usually between 64 - 256px square in size.
        /// </summary>
        Large,
        /// <summary>
        /// Access the extra large icon for the DLC.
        /// Usually a high res icon atleast 512px square or larger.
        /// </summary>
        ExtraLarge,
    }

    /// <summary>
    /// Provides access to various icon content that has been registered with the DLC content.
    /// Useful for displaying in UI's or splash screens to indicate that a certain DLC has been detected and activated.
    /// </summary>
    public interface IDLCIconProvider
    {
        // Methods
        /// <summary>
        /// Check if the specified icon is available for this DLC.
        /// </summary>
        /// <param name="iconType">The type of icon</param>
        /// <returns>True if the icon is present or false if not</returns>
        bool HasIcon(DLCIconType iconType);

        /// <summary>
        /// Check if the custom icon with the specified key is available for this DLC.
        /// </summary>
        /// <param name="iconKey">The string key for the custom icon</param>
        /// <returns>True if the icon is present or false if not</returns>
        bool HasCustomIcon(string iconKey);

        /// <summary>
        /// Load the icon of the specified type or null if no icon was assigned.
        /// </summary>
        /// <param name="iconType">The icon type to fetch</param>
        /// <returns>The texture for the requested icon</returns>
        Texture2D LoadIcon(DLCIconType iconType);

        /// <summary>
        /// Load the custom icon specified by the unique icon key string that is assigned when setting up your DLC content.
        /// Use this method when all the <see cref="DLCIconType"/> options have been used up or do not properly describe the icon content. 
        /// </summary>
        /// <param name="iconKey">The unique string key of the icon to fetch</param>
        /// <returns>The texture for the requested icon</returns>
        Texture2D LoadCustomIcon(string iconKey);

        /// <summary>
        /// Load the icon of the specified type asynchronously or null if no icon was assigned.
        /// </summary>
        /// <param name="iconType">The icon type to fetch</param>
        /// <returns>The texture for the requested icon</returns>
        DLCAsync<Texture2D> LoadIconAsync(DLCIconType iconType);

        /// <summary>
        /// Load the custom icon specified by the unique icon key string asynchronously that is assigned when setting up your DLC content.
        /// Use this method when all the <see cref="DLCIconType"/> options have been used up or do not properly describe the icon content. 
        /// </summary>
        /// <param name="iconKey">The unique string key of the icon to fetch</param>
        /// <returns>The texture for the requested icon</returns>
        DLCAsync<Texture2D> LoadCustomIconAsync(string iconKey);
    }
}
