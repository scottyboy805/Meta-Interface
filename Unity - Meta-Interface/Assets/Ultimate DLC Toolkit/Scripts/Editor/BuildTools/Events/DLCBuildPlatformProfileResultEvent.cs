using DLCToolkit.Profile;

namespace DLCToolkit.BuildTools.Events
{
    /// <summary>
    /// Implement this event base when you want to hook into the DLC build profile platform completed event.
    /// Use with <see cref="DLCPostBuildPlatformProfileAttribute"/>
    /// </summary>
    public abstract class DLCBuildPlatformProfileResultEvent
    {
        // Methods
        /// <summary>
        /// Called while building DLC content for the specified profile and platform with result information.
        /// </summary>
        /// <param name="profile">The profile that was built</param>
        /// <param name="platformProfile">The platform that was built</param>
        /// <param name="success">Did the build succeed</param>
        /// <param name="output">The output path of the DLC content file</param>
        public abstract void OnBuildProfileEvent(DLCProfile profile, DLCPlatformProfile platformProfile, bool success, string output);
    }
}
