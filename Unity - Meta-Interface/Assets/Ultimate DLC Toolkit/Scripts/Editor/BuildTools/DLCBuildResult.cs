using DLCToolkit.Profile;
using System;
using System.Collections.Generic;
using System.IO;
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
        private DateTime buildStartTime = DateTime.MinValue;
        private TimeSpan elapsedBuildTime = TimeSpan.Zero;

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

        /// <summary>
        /// The time that the DLC platform build request started.
        /// </summary>
        public DateTime BuildStartTime
        {
            get { return buildStartTime; }
        }

        /// <summary>
        /// The amount of time that the entire platform build took to complete.
        /// </summary>
        public TimeSpan ElapsedBuildTime
        {
            get { return elapsedBuildTime; }
        }

        // Constructor
        internal DLCBuildTask(DLCProfile profile, DLCPlatformProfile platformProfile, DateTime buildStartTime, string outputPath, bool success)
        {
            this.profile = profile;
            this.platformProfile = platformProfile;
            this.outputPath = outputPath;
            this.success = success;
            this.buildStartTime = buildStartTime;
            this.elapsedBuildTime = DateTime.Now - buildStartTime;
        }
    }

    /// <summary>
    /// Contains information about which aspects of the DLC build request were successful and which were not.
    /// </summary>
    public sealed class DLCBuildResult
    {
        // Private
        private List<DLCBuildTask> buildTasks = new List<DLCBuildTask>();
        private DLCManifest manifest = null;
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
        /// The total number of build tasks that completed successfully.
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
        /// Get the manifest for the build.
        /// </summary>
        public DLCManifest Manifest
        {
            get 
            {
                // Create manifest
                if (manifest == null)
                    BuildManifest();

                return manifest; 
            }
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
            this.buildStartTime = DateTime.Now;
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

        internal DLCBuildTask WithSuccessfulTask(DLCProfile profile, DLCPlatformProfile platformProfile, DateTime buildStartTime, string outputPath)
        {
            // Create successful
            DLCBuildTask result;
            buildTasks.Add(result = new DLCBuildTask(profile, platformProfile, buildStartTime, outputPath, true));

            return result;
        }

        internal DLCBuildTask WithFailedTask(DLCProfile profile, DLCPlatformProfile platformProfile, DateTime buildStartTime)
        {
            // Create failure
            DLCBuildTask result;
            buildTasks.Add(result = new DLCBuildTask(profile, platformProfile, buildStartTime, "", false));

            return result;
        }

        internal void WithCompleteTime()
        {
            elapsedBuildTime = DateTime.Now - buildStartTime;
        }

        private void BuildManifest()
        {
            // Store manifest entries
            List<DLCManifestEntry> manifestEntries = new List<DLCManifestEntry>();

            // Setup all entries
            foreach(DLCBuildTask build in buildTasks)
            {
                // Get the path
                string dlcPath = build.Profile.GetPlatformOutputPath(build.PlatformProfile.Platform);

                // Get file meta
                long size = 0;
                DateTime writeTime = default;

                if(File.Exists(build.OutputPath) == true)
                {
                    // Create the file info
                    FileInfo info = new FileInfo(dlcPath);

                    size = info.Length;
                    writeTime = info.LastWriteTime;
                }

                // Build manifest entry
                DLCManifestEntry entry = new DLCManifestEntry
                {
                    dlcUniqueKey = build.PlatformProfile.DlcUniqueKey,
                    dlcName = build.Profile.DLCName,
                    dlcPath = dlcPath,
                    dlcIAPName = build.PlatformProfile.DLCIAPName,
                    shipWithGame = build.PlatformProfile.ShipWithGame,
                    streamingContent = build.PlatformProfile.ShipWithGameDirectory == ShipWithGameDirectory.StreamingAssets,
                    sizeOnDisk = size,
                    lastWriteTime = writeTime,
                };

                // Add the entry
                manifestEntries.Add(entry);
            }

            // Create manifest
            manifest = new DLCManifest
            {
                // Get all entries
                dlcContents = manifestEntries.ToArray(),
            };
        }

        internal static DLCBuildResult Empty()
        {
            DLCBuildResult result = new DLCBuildResult();
            result.WithCompleteTime();
            return result;
        }
    }
}
