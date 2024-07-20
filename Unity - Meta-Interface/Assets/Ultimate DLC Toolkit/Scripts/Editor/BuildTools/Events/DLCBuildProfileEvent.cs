using DLCToolkit.Profile;

namespace DLCToolkit.BuildTools.Events
{
    /// <summary>
    /// Implement this event base when you want to hook into DLC build profile event.
    /// Use with <see cref="DLCPreBuildProfileAttribute"/>.
    /// </summary>
    public abstract class DLCBuildProfileEvent
    {
        // Methods
        /// <summary>
        /// Called while building DLC content for the specified profile.
        /// </summary>
        /// <param name="profile">The profile that is currently being built</param>
        public abstract void OnBuildProfileEvent(DLCProfile profile);
    }
}
