
namespace DLCToolkit.BuildTools.Events
{
    /// <summary>
    /// Implement this event base when you want to hook into DLC pre or post build events.
    /// Use with <see cref="DLCPreBuildAttribute"/> or <see cref="DLCPostBuildAttribute"/>.
    /// </summary>
    public abstract class DLCBuildEvent
    {
        // Methods
        /// <summary>
        /// Called while building DLC content.
        /// </summary>
        public abstract void OnBuildEvent();
    }
}
