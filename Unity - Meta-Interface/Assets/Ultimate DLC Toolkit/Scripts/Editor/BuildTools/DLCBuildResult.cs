using Codice.CM.Common.Tree.Partial;
using DLCToolkit.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace DLCToolkit.BuildTools
{
    /// <summary>
    /// Represents an individual build task of a DLC profile for a specific build platform.
    /// </summary>
    public sealed class DLCBuildTask
    {
        // Private
        private DLCProfile profile = null;
        private DLCPlatformProfile platformProfile = null;
        private string outputPath = "";
        private bool success = false;

        // Properties
        /// <summary>
        /// The DLC profile for this build.
        /// </summary>
        public DLCProfile Profile
        {
            get { return profile; }
        }

        /// <summary>
        /// The DLC platform profile for this build.
        /// </summary>
        public DLCPlatformProfile PlatformProfile
        {
            get { return platformProfile; }
        }

        /// <summary>
        /// The output path for this build if successful or empty string if not.
        /// </summary>
        public string OutputPath
        {
            get { return outputPath; }
        }

        /// <summary>
        /// Was the build successful.
        /// </summary>
        public bool Success
        {
            get { return success; }
        }

        // Constructor
        internal DLCBuildTask(DLCProfile profile, DLCPlatformProfile platformProfile, string outputPath, bool success)
        {
            this.profile = profile;
            this.platformProfile = platformProfile;
            this.outputPath = outputPath;
            this.success = success;
        }
    }

    /// <summary>
    /// Contains information about which aspects of the DLC build request were successful and which were not.
    /// </summary>
    public sealed class DLCBuildResult
    {
        // Private
        private List<DLCBuildTask> buildTasks = new List<DLCBuildTask>();
        private DateTime buildStartTime = DateTime.MinValue;
        private TimeSpan elapsedBuildTime = TimeSpan.Zero;

        // Properties
        /// <summary>
        /// All build tasks that were included in the request whether they were successful or not.
        /// </summary>
        public IReadOnlyList<DLCBuildTask> BuildTasks
        {
            get { return buildTasks; }
        }

        /// <summary>
        /// The total number of build tasks that were run.
        /// </summary>
        public int BuildTaskCount
        {
            get { return buildTasks.Count; }
        }

        /// <summary>
        /// The total number of build taks that completed successfully.
        /// </summary>
        public int BuildSuccessCount
        {
            get { return buildTasks.Count(t => t.Success == true); }
        }

        /// <summary>
        /// The total number of build tasks that failed.
        /// </summary>
        public int BuildFailedCount
        {
            get { return buildTasks.Count(t => t.Success == false); }
        }

        /// <summary>
        /// Were all build tasks that were started completed successfully.
        /// No errors no failed builds.
        /// </summary>
        public bool AllSuccessful
        {
            get { return BuildTaskCount == BuildSuccessCount; }
        }

        /// <summary>
        /// The time that the DLC build request started.
        /// </summary>
        public DateTime BuildStartTime
        {
            get { return buildStartTime; }
        }

        /// <summary>
        /// The amount of time that the entire build batch took to complete.
        /// </summary>
        public TimeSpan ElapsedBuildTime
        {
            get { return elapsedBuildTime; }
        }

        // Constructor
        internal DLCBuildResult() 
        {
            buildStartTime = DateTime.Now;
        }

        // Methods
        /// <summary>
        /// Get all successful build tasks.
        /// </summary>
        /// <returns>An enumerable of successful build tasks</returns>
        public IEnumerable<DLCBuildTask> GetSuccessfulBuildTasks()
        {
            return buildTasks.Where(t => t.Success == true);
        }

        /// <summary>
        /// Get all failed build tasks.
        /// </summary>
        /// <returns>An enumerable of failed build tasks</returns>
        public IEnumerable<DLCBuildTask> GetFailedBuildTasks()
        {
            return buildTasks.Where(t => t.Success == false);
        }

        /// <summary>
        /// Check if any build tasks were run for the specified platform.
        /// </summary>
        /// <param name="target">The platform to check</param>
        /// <returns>True if one or more build tasks were run for the target platform whether successful or not</returns>
        public bool HasBuildTasksForPlatform(BuildTarget target)
        {
            return buildTasks.Any(t => t.PlatformProfile.Platform == target);
        }

        /// <summary>
        /// Get all build tasks for the target platform.
        /// </summary>
        /// <param name="target">The platform of interest</param>
        /// <returns>An enumerable of build tasks for the target platform</returns>
        public IEnumerable<DLCBuildTask> GetBuildTasksForPlatform(BuildTarget target)
        {
            return buildTasks.Where(t => t.PlatformProfile.Platform == target);
        }

        /// <summary>
        /// Get all build tasks that completed successfully for the target platform.
        /// </summary>
        /// <param name="target">The platform of interest</param>
        /// <returns>An enumerable of successful build tasks for the target platform</returns>
        public IEnumerable<DLCBuildTask> GetSuccessfulBuildTasksForPlatform(BuildTarget target)
        {
            return GetBuildTasksForPlatform(target)
                .Where(t => t.Success == true);
        }

        /// <summary>
        /// Get all build tasks that failed for the target platform.
        /// </summary>
        /// <param name="target">The platform of interest</param>
        /// <returns>An enumerable of failed build tasks for the target platform</returns>
        public IEnumerable<DLCBuildTask> GetFailedBuildTasksForPlatform(BuildTarget target)
        {
            return GetBuildTasksForPlatform(target)
                .Where(t => t.Success == false);
        }

        internal void WithSuccessfulTask(DLCProfile profile, DLCPlatformProfile platformProfile, string outputPath)
        {
            buildTasks.Add(new DLCBuildTask(profile, platformProfile, outputPath, true));
        }

        internal void WithFailedTask(DLCProfile profile, DLCPlatformProfile platformProfile)
        {
            buildTasks.Add(new DLCBuildTask(profile, platformProfile, "", false));
        }

        internal void WithCompleteTime()
        {
            elapsedBuildTime = DateTime.Now - buildStartTime;
        }

        internal static DLCBuildResult Empty()
        {
            DLCBuildResult result = new DLCBuildResult();
            result.WithCompleteTime();
            return result;
        }
    }
}
