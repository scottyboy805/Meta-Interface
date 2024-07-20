using System;
using DLCToolkit.BuildTools.Events;

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Used to mark a post DLC build event called just after DLC has been built.
    /// Must implement <see cref="DLCBuildEvent"/> base class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DLCPostBuildAttribute : Attribute
    {
        // Empty class
    }
}
