using System;
using DLCToolkit.BuildTools.Events;

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Used to mark a pre DLC build event called just before a specific DLC profile will be built.
    /// Must implement <see cref="DLCBuildProfileEvent"/> base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DLCPreBuildProfileAttribute : Attribute
    {
        // Empty class
    }
}
