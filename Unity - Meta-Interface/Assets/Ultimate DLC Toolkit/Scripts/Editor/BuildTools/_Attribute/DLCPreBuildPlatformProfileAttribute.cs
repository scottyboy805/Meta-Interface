using System;
using DLCToolkit.BuildTools.Events;

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Used to mark a pre DLC build event called just before a specific DLC profile platform will be built.
    /// Must implement <see cref="DLCBuildPlatformProfileEvent"/> base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DLCPreBuildPlatformProfileAttribute : Attribute
    {
        // Empty class
    }
}
