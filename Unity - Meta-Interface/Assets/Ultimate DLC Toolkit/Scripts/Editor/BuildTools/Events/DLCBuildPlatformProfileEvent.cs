using DLCToolkit.Profile;

namespace DLCToolkit.BuildTools.Events
{
    /// <summary>
    /// Implement this event base when you want to hook into DLC build profile platform event.
    /// Use with <see cref="DLCPreBuildPlatformProfileAttribute"/>.
    /// </summary>
    public abstract class DLCBuildPlatformProfileEvent
    {
        // Methods
        /// <summary>
        /// Called while building DLC content for the specified profile and platform.
        /// </summary>
        /// <param name="profile">The profile that is currently being built</param>
        /// <param name="platformProfile">The platform that is currently being build</param>
        public abstract void OnBuildProfileEvent(DLCProfile profile, DLCPlatformProfile platformProfile);
    }
}
