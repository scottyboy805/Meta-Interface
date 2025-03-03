using DLCToolkit.BuildTools.Events;
using DLCToolkit.BuildTools.Format;
using DLCToolkit.BuildTools.Scripting;
using DLCToolkit.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.BuildTools
{
    internal sealed class DLCBuildRequest
    {
        // Private
        private const string tempBundleCache = "Temp/DLCBundles";
        private const string androidAssetPackFolder = "src/main/assets";
        private DLCProfile[] profiles = null;

        // Constructor
        public DLCBuildRequest(DLCProfile[] profiles)
        {
            this.profiles = profiles;
        }

        // Methods
        public DLCBuildResult BuildDLCForPlatforms(string outputFolder, BuildTarget[] platforms, DLCBuildOptions buildOptions)
        {
            int totalBuildCount = 0;
            DLCBuildResult result = new DLCBuildResult();

            // Scan and register all build profiles for enabled dlc
            Dictionary<BuildTarget, List<DLCBuildContext>> platformBuilds = 
                RegisterDLCPlatformContent(platforms, (buildOptions & DLCBuildOptions.IncludeDisabledDLC) != 0, out totalBuildCount);

            // Check for no content
            if(platformBuilds.Count == 0)
            {
                Debug.LogError("DLC build failed! There is no content to build for the selected platforms!");
                result.WithCompleteTime();
                return result;
            }

            // Create build folder
            if(Directory.Exists(tempBundleCache) == false)
                Directory.CreateDirectory(tempBundleCache);
            
            // Store a shortlist of all dlc that passed all checks and will be built to disk
            List<DLCBuildContext> outputBuilds = new List<DLCBuildContext>();
            
            // Process all platforms
            foreach (KeyValuePair<BuildTarget, List<DLCBuildContext>> platformBuild in platformBuilds)
            {
                // Update debug
                Debug.logPrefix = string.Format("DLC Build ({0}): ", DLCPlatformProfile.GetFriendlyPlatformName(platformBuild.Key));

                // Check for build target supported
                if (DLCPlatformProfile.IsDLCBuildTargetAvailable(platformBuild.Key) == false)
                {
                    Debug.LogError("DLC content cannot be built for the target platform because the build tools are not installed: " + DLCPlatformProfile.GetFriendlyPlatformName(platformBuild.Key));
                    Debug.LogError(platformBuild.Key + " build tools must be installed first via the Unity hub!");
                    continue;
                }

                // Store asset bundle builds
                List<AssetBundleBuild> bundleBuilds = new List<AssetBundleBuild>();

                // Process all builds
                foreach (DLCBuildContext context in platformBuild.Value)
                {
                    // Get platform build start time
                    context.platformBuildStartTime = DateTime.Now;

                    try
                    { 
                        // Check for scripting supported
                        bool scriptingSupported = (buildOptions & DLCBuildOptions.DisableScripting) == 0
                            && IsScriptBuildPlatform(context.PlatformProfile.Platform);

                        // Update debug
                        Debug.logPrefix = string.Format("DLC Build ({0}, {1}): ", context.Profile.DLCName, DLCPlatformProfile.GetFriendlyPlatformName(platformBuild.Key));
                        Debug.Log("Prepare to build DLC...");

                        // Update build time
                        context.Profile.UpdateLastBuildTargets(platforms);
                        context.Profile.lastBuildTime = DateTime.Now.ToFileTime();
                        context.PlatformProfile.lastPlatformBuildTime = DateTime.Now.ToFileTime();

                        // Validate profile
                        if (context.ValidateBuildProfile() == false)
                        {
                            Debug.LogError("DLC will not be built because the DLC profile is incomplete or contains invalid data: " + context.Profile.DLCProfileName);
                            result.WithFailedTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime);
                            MarkAsFailed(context);
                            continue;
                        }

                        // Collect all assets
                        if (context.CollectBuildAssets(bundleBuilds, scriptingSupported) == false)
                        {
                            Debug.LogError("DLC will not be built because it does not have any associated assets: " + context.Profile.DLCProfileName);
                            result.WithFailedTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime);
                            MarkAsFailed(context);
                            continue;
                        }

                        // Compile scripts
                        if (scriptingSupported == true && context.ScriptCollection.HasScriptAssemblies == true)
                        {
                            // Get the compilation batch
                            ScriptAssemblyBatch compilationBatch = context.ScriptCollection.CompilationBatch;

                            // Get platform group
                            BuildTargetGroup platformGroup = BuildPipeline.GetBuildTargetGroup(context.PlatformProfile.Platform);

                            // Request compilation of scripts for player
                            CompilationResult compileResult = compilationBatch.RequestPlayerCompilation(
                                context.PlatformProfile.Platform, platformGroup,
                                context.ScriptCollection.IncludeOrderedCompilations,
                                context.PlatformProfile.PlatformDefines,
                                (buildOptions & DLCBuildOptions.DebugScripting) != 0);

                            // Check for success
                            if (compileResult == CompilationResult.Failed)
                            {
                                Fail(context, "DLC build failed! There were script compilation errors");
                                result.WithFailedTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime);
                                MarkAsFailed(context);
                                continue;
                            }
                            else if(compileResult == CompilationResult.CompiledWithoutSymbols)
                            {
                                Debug.LogWarning("Script compilation was successful but debug symbols could not be generated for the target platform! Script debugging may not be available");
                            }
                            else
                            {
                                Debug.Log(string.Format("Script compilation successful! ({0})", context.ScriptCollection.IncludeCompilations.Count));
                            }
                        }
                        else if (context.ScriptCollection.HasScriptAssemblies == true)
                        {
                            Debug.LogWarning("DLC contains scripts but the target platform does not support scripting! Scripts may be missing in DLC content!");
                        }


                        // Get the platform profile
                        DLCPlatformProfile platformProfile = context.PlatformProfile;

                        // Create bundle options
                        BuildAssetBundleOptions options = 0;

                        // WebGL platform must use uncompressed bundles for loading purposes - so we can enforce that here
#if !UNITY_WEBGL
                        // Compression
                        if (platformProfile.UseCompression == true)
                        {
                            options |= BuildAssetBundleOptions.ChunkBasedCompression;
                        }
                        else
#endif
                        {
                            options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                        }

                        // Strict build
                        if (platformProfile.StrictBuild == true)
                            options |= BuildAssetBundleOptions.StrictMode;

                        // Force rebuild
                        if ((buildOptions & DLCBuildOptions.ForceRebuild) != 0)
                            options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;

                        // Report status
                        Debug.Log("Building required asset bundles for platform...");

                        // Build all bundles as a batch
                        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(tempBundleCache,
                            bundleBuilds.ToArray(),
                            options,
                            platformBuild.Key);

                        // Reload profile asset
                        // ### Nasty workaround - Building asset bundles can cause the DLC Profile asset to be destroyed from memory (profile == "null" in Unity check) in some versions - We need to check if that is the case and reload from disk in those situations, since the profile asset is needed to report back build information
                        // Note that while the profile asset is considered `null` by Unity, it is still possible to access all managed information and fields with no issue, but will cause an exception if passed to a Unity method like `SetDirty()`
                        if (context.Profile == null)
                            context.ReloadProfileFromAssetDatabase();

                        // Check manifest to see if the requested bundles were created
                        if (context.ValidateBundleContent(manifest) == true)
                            outputBuilds.Add(context);

                    }
                    catch (Exception e)
                    {
                        Debug.LogError("An unhandled exception occurred during the main build stage: " + e.ToString());
                        Fail(context, "DLC Build Failed! See previous errors!");
                        result.WithFailedTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime);
                        continue;
                    }
                }
            }

            // Check for any finalized DLC content
            if (outputBuilds.Count == 0)
            {
                // Update debug
                Debug.logPrefix = "DLC Build: ";
                FailWithCleanup(null, "DLC Build Failed! See previous errors!");
                result.WithCompleteTime();
                return result;
            }

            // We can start to write out all built DLC's to disk since there were no issues
            foreach (DLCBuildContext context in outputBuilds)
            {
                try
                {
                    // Check for scripting supported
                    bool scriptingSupported = (buildOptions & DLCBuildOptions.DisableScripting) == 0
                        && IsScriptBuildPlatform(context.PlatformProfile.Platform)
                        && context.ScriptCollection.HasScriptAssemblies == true;

                    // Check for scripting debug more
                    bool scriptingDebug = (buildOptions & DLCBuildOptions.DebugScripting) != 0;

                    // Update debug
                    Debug.logPrefix = string.Format("DLC Build ({0}, {1}): ", context.Profile.DLCName, DLCPlatformProfile.GetFriendlyPlatformName(context.PlatformProfile.Platform));

                    // Build the DLC content bundle
                    DLCBuildBundle bundle = context.BuildDLCContent(tempBundleCache, scriptingSupported, scriptingDebug);

                    // Reload profile asset after build
                    // ### Nasty workaround - Building asset bundles can cause the DLC Profile asset to be destroyed from memory (profile == "null" in Unity check) in some versions - We need to check if that is the case and reload from disk in those situations, since the profile asset is needed to report back build information
                    // Note that while the profile asset is considered `null` by Unity, it is still possible to access all managed information and fields with no issue, but will cause an exception if passed to a Unity method like `SetDirty()`
                    if (context.Profile == null)
                        context.ReloadProfileFromAssetDatabase();

                    // Get output path
                    string platformOutputPath = context.Profile.GetPlatformOutputFolder(context.PlatformProfile.Platform);

                    Debug.Log("Preparing output location: " + platformOutputPath);

                    // Make sure output folder exists
                    if (Directory.Exists(platformOutputPath) == false)
                        Directory.CreateDirectory(platformOutputPath);

                    // Get output file
                    string dlcOutputPath = context.Profile.GetPlatformOutputPath(context.PlatformProfile.Platform);

                    // Write all to disk
                    Debug.Log("Writing DLC content to disk...");
                    using (Stream outputStream = File.Create(dlcOutputPath))
                    {
                        // Write the bundle
                        bundle.WiteToSteam(outputStream);
                    }

                    // At this point the DLC has been built with no errors so we can mark the profile as no longer requiring a build
                    context.Profile.dlcContentPendingBuild = false;
                    EditorUtility.SetDirty(context.Profile);

                    Debug.Log("DLC was successfully written to disk: " + dlcOutputPath);

                    // Check for Android
                    if (context.PlatformProfile.Platform == BuildTarget.Android)
                    {
                        Debug.Log("Preparing additional platform specific build steps...");

                        // Get android profile
                        DLCPlatformProfileAndroid androidPlatform = context.PlatformProfile as DLCPlatformProfileAndroid;

                        // Create build directory
                        string androidCustomAssetPackDirectory = androidPlatform.GetDLCCustomAssetPackPath(androidPlatform.DlcUniqueKey);

                        // Append android path
                        androidCustomAssetPackDirectory = Path.Combine(androidCustomAssetPackDirectory, androidAssetPackFolder);

                        // Create folder
                        if (Directory.Exists(androidCustomAssetPackDirectory) == false)
                            Directory.CreateDirectory(androidCustomAssetPackDirectory);

                        // Copy DLC build to folder
                        string androidCustomAssetPackContentFile = Path.Combine(androidCustomAssetPackDirectory, androidPlatform.DlcUniqueKey);

                        Debug.Log("Writing Android custom asset pack to disk...");

                        // Create android custom asset pack content file
                        File.Copy(dlcOutputPath, androidCustomAssetPackContentFile, true);


                        Debug.Log("Generating build.gradle for Android DLC...");

                        // Write gradle file
                        File.WriteAllText(androidPlatform.GetDLCCustomAssetPackGradlePath(androidPlatform.DlcUniqueKey),
                            androidPlatform.GetDLCustomAssetGradleContents(androidPlatform.DlcUniqueKey));

                        // Allow editor to import assets
                        Debug.Log("Refreshing assets and import Android custom asset pack...");
                        AssetDatabase.Refresh();
                    }

                    // Report successful
                    DLCBuildTask platformBuildTask;
                    if(context.isFaulted == true)
                    {
                        // Update result
                        context.Profile.lastBuildSuccess = false;
                        context.PlatformProfile.lastBuildSuccess = false;

                        platformBuildTask = result.WithFailedTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime);
                    }
                    else
                    {
                        // Update result
                        context.Profile.lastBuildSuccess = true;
                        context.PlatformProfile.lastBuildSuccess = true;

                        platformBuildTask = result.WithSuccessfulTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime, dlcOutputPath);
                    }

                    // Trigger build finished event
                    DLCBuildEventHooks.SafeInvokeBuildEventHookImplementations<DLCPostBuildPlatformProfileAttribute, DLCBuildPlatformProfileResultEvent>
                        ((DLCBuildPlatformProfileResultEvent e) => e.OnBuildProfileEvent(context.Profile, context.PlatformProfile, platformBuildTask));
                }
                catch (Exception e)
                {
                    Debug.LogError("An unhandled exception occurred during the final build stage: " + e.ToString());
                    Fail(context, "DLC Build Failed! See previous errors!");
                    result.WithFailedTask(context.Profile, context.PlatformProfile, context.platformBuildStartTime);
                    continue;
                }
            }

            // Run cleanup
            Cleanup();

            // Success
            UnityEngine.Debug.Log("DLC Build Successful! (" + outputBuilds.Count(b => b.isFaulted == false)+ " / " + totalBuildCount + ")");

            // Mark complete time
            result.WithCompleteTime();
            return result;
        }

        private Dictionary<BuildTarget, List<DLCBuildContext>> RegisterDLCPlatformContent(BuildTarget[] platforms, bool includeDisabledDLC, out int totalBuildCount)
        {
            totalBuildCount = 0;

            // Check if we should build all platforms
            bool allPlatforms = platforms == null;

            // Create collection where platforms will be registered
            Dictionary<BuildTarget, List<DLCBuildContext>> platformBuilds = 
                new Dictionary<BuildTarget, List<DLCBuildContext>>();

            // Register all profiles
            foreach (DLCProfile profile in profiles)
            {
                // Check fir dlc should be built
                if(profile.EnabledForBuild == false && includeDisabledDLC == false)
                {
                    Debug.LogWarning("DLC will not be built because it is not enabled for build: " + profile.DLCName);
                    continue;
                }


                // Trigger profile build event
                DLCBuildEventHooks.SafeInvokeBuildEventHookImplementations<DLCPreBuildProfileAttribute, DLCBuildProfileEvent>(
                    (DLCBuildProfileEvent e) => e.OnBuildProfileEvent(profile));


                // Process all platforms
                foreach(DLCPlatformProfile platformProfile in profile.Platforms)
                {
                    // Check if platform should be built
                    if((platformProfile.Enabled == true || includeDisabledDLC == true) && 
                        (allPlatforms == true || IsBuildPlatform(platforms, platformProfile.Platform) == true))
                    {
                        // Trigger platform profile build event
                        DLCBuildEventHooks.SafeInvokeBuildEventHookImplementations<DLCPreBuildPlatformProfileAttribute, DLCBuildPlatformProfileEvent>
                            ((DLCBuildPlatformProfileEvent e) => e.OnBuildProfileEvent(profile, platformProfile));


                        // Create build context
                        DLCBuildContext context = new DLCBuildContext(profile, platformProfile);

                        // Register for build
                        if (platformBuilds.ContainsKey(platformProfile.Platform) == false)
                            platformBuilds[platformProfile.Platform] = new List<DLCBuildContext>();

                        // Add to build list
                        platformBuilds[platformProfile.Platform].Add(context);
                        totalBuildCount++;
                    }
                }
            }

            return platformBuilds;
        }

        private void FailWithCleanup(DLCBuildContext context, string reason)
        {
            Fail(context, reason);
            Cleanup();
        }

        private void Fail(DLCBuildContext context, string reason)
        {
            // Report error
            Debug.LogError(reason);

            // Set failed state
            if (context != null)
            {
                context.isFaulted = true;
                MarkAsFailed(context);
            }
        }

        private void MarkAsFailed(DLCBuildContext context)
        {
            // Set failed state
            if (context != null)
            {
                context.Profile.lastBuildSuccess = false;
                context.PlatformProfile.lastBuildSuccess = false;
            }
        }

        private void Cleanup()
        {
            // Cleanup scripting
            if (Directory.Exists(ScriptAssemblyCompilation.compilationDirectory) == true)
                Directory.Delete(ScriptAssemblyCompilation.compilationDirectory, true);
        }


        private bool IsBuildPlatform(BuildTarget[] buildPlatforms, BuildTarget buildProfile)
        {
            if (buildPlatforms != null)
            {
                foreach(BuildTarget platform in buildPlatforms)
                {
                    if (platform == buildProfile)
                        return true;
                }
            }
            return false;
        }

        private bool IsScriptBuildPlatform(BuildTarget buildPlatform)
        {
            switch(buildPlatform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64: return true;
            }
            return false;
        }
    }
}
