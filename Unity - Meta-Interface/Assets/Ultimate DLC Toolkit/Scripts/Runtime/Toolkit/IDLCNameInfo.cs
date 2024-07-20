using System;

namespace DLCToolkit
{
    /// <summary>
    /// Access name information for a given DLC.
    /// </summary>
    public interface IDLCNameInfo
    {
        // Properties
        /// <summary>
        /// The friendly name of the DLC content.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The unique named key for the DLC content.
        /// </summary>
        string UniqueKey { get; }

        /// <summary>
        /// The version information for the DLC content.
        /// </summary>
        Version Version { get; }
    }
}
