using System;
using DLCToolkit.BuildTools.Events;

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Used to mark a pre DLC build event called just before DLC will be built.
    /// Must implement <see cref="DLCBuildEvent"/> base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DLCPreBuildAttribute : Attribute
    {
        // Empty class
    }
}
