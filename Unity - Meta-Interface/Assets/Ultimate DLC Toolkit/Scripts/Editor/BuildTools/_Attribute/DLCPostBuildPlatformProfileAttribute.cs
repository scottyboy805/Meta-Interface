using System;
using DLCToolkit.BuildTools.Events;

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Used to mark a post DLC build event called just after a specific DLC profile platform has been built.
    /// Must implement <see cref="DLCBuildPlatformProfileResultEvent"/> base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DLCPostBuildPlatformProfileAttribute : Attribute
    {
        // Empty class
    }
}
