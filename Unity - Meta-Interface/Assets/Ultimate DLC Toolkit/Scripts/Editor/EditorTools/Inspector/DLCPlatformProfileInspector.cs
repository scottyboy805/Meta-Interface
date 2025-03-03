using UnityEngine;
using UnityEditor;
using DLCToolkit.Profile;
using System.Linq;
using System;
using System.Collections.Generic;
using DLCToolkit.BuildTools;
using System.IO;
using System.Reflection;

namespace DLCToolkit.EditorTools
{
    [CustomEditor(typeof(DLCProfile))]
    public sealed class DLCPlatformProfileInspector : Editor
    {
        // Private
        private static readonly GUIContent guidLabel = new GUIContent("DLC GUID", "The unique Guid assigned to this DLC");
        private static readonly GUIContent contentFolderLabel = new GUIContent("DLC Content Folder", "The folder where DLC assets should be placed");
        private static readonly GUIContent buildFolderLabel = new GUIContent("DLC Build Folder", "The folder where the DLC content will be exported to upon build");
        private static readonly GUIContent nameLabel = new GUIContent("DLC Name", "The name of this DLC content");
        private static readonly GUIContent versionLabel = new GUIContent("DLC Version", "The current version of this DLC in the format X.X.X");
        private static readonly GUIContent signingLabel = new GUIContent("DLC Signing", "Should the DLC content be signed during build so that it will only be loadable by this game project");
        private static readonly GUIContent signingWithVersionLabel = new GUIContent("DLC Version Signing", "Should the DLC content be version signed, meaning that it will only be loadable by the version of the game project it was built for");
        private static readonly GUIContent enabledLabel = new GUIContent("Enabled For Build", "Should this DLC content be included when building project DLC's");
        private static readonly GUIContent lastBuildTimeLabel = new GUIContent("Last Build Time", "The last time a build attempt was made for this DLC profile");
        private static readonly GUIContent lastBuildResultLabel = new GUIContent("Last Build Result", "The result of the last build attempt for this DLC profile");
        private static readonly GUIContent descriptionLabel = new GUIContent("Description", "A short description for this DLC which will be available at runtime as part of the DLC metadata");
        private static readonly GUIContent developerLabel = new GUIContent("Developer", "The name of the developer or studio that created this DLC content which will be available at runtime as part of the DLC metadata");
        private static readonly GUIContent publisherLabel = new GUIContent("Publisher", "The name of the publisher who distributed this DLC content which will be available at runtime as part of the DLC metadata");
        private static readonly GUIContent smallIconLabel = new GUIContent("Small Icon", "The small icon for this DLC. Recommended to use a square image of approximately 32x32px");
        private static readonly GUIContent customMetadataLabel = new GUIContent("Custom Metadata", "The custom metadata for this DLC profile which allows additional quick access user data to be associated with the DLC content");
        private static readonly GUIContent mediumIconLabel = new GUIContent("Medium Icon", "The medium icon for this DLC. Recommended to use a square image of approximately 64x64px");
        private static readonly GUIContent largeIconLabel = new GUIContent("Large Icon", "The large icon for this DLC. Recommended to use a square image of approximately 256x256px");
        private static readonly GUIContent extraLargeIconLabel = new GUIContent("Extra Large Icon", "The extra large icon for this DLC. Recommended to use a square image or approximately 512x512px");
        private static readonly GUIContent customIconLabel = new GUIContent("Custom Icons", "An extra collection of icons for this DLC");
        private static readonly GUIContent platformEnabledLabel = new GUIContent("Platform Enabled", "Should this DLC content be included on the selected platform when building project DLC's");
        private static readonly GUIContent platformUniqueKeyLabel = new GUIContent("DLC Unique Key *", "The DLC unique key for the selected platform");
        private static readonly GUIContent platformExtensionLabel = new GUIContent("DLC Extension", "The DLC file extension for the selected platform. Can be empty if no extension is required");
        private static readonly GUIContent useCompressionLabel = new GUIContent("Use Compression", "Should the DLC be built with compression for the selected platform");
        private static readonly GUIContent strictBuildLabel = new GUIContent("Strict Build", "Should the DLC asset bundles be built in strict mode for the selected platform");
        private static readonly GUIContent dlcIAPNameLabel = new GUIContent("DLC IAP Name", "The name of the In-App Purchase to link this DLC to for ownership verification purposes. Linking to an IAP will allow ownership checks before loading the DLC content to ensure that the user has purchased the content if it is paid for. You can simply leave this blank if the DLC does not require ownership verification (Or is provided via another service) and is available for all users (If you are using DLC toolkit simply as a content management system for example) NOTE - Requires Unity.Purchasing to be setup in the project");
        private static readonly GUIContent preloadSharedAssetsLabel = new GUIContent("Preload Shared Assets", "Should the DLC preload shared assets when it is loaded into the game for the selected platform. Shared assets will be loaded on demand if disabled");
        private static readonly GUIContent preloadSceneAssetsLabel = new GUIContent("Preload Scene Assets", "Should the DLC preload scene assets when it is loaded into the game for the selected platform. Scene assets will be loaded on demand if disabled");
        private static readonly GUIContent shipWithGameLabel = new GUIContent("Ship With Game", "Should the DLC content be included with the game at build time. This can include content that the user should not own until purchased, which can be handled by the DRM provider");
        private static readonly GUIContent shipWithGameDirectoryLabel = new GUIContent("Ship With Game Directory", "The directory where the DLC content will be packaged in the built game");
        private static readonly GUIContent shipWithGamePathLabel = new GUIContent("Ship With Game Path", "The path relative to the ship directory where DLC content will be packaged in the built game");
        private static readonly GUIContent platformSpecificFolderLabel = new GUIContent("Platform Specific Folder", "The folder path relative to the build folder where the DLC will be built for this platform");
        private static readonly GUIContent platformBuildOutputLabel = new GUIContent("Platform Build Output", "The output folder or files that will be build for this platform");

        private static readonly GUIContent dlcAssetPackDirectoryLabel = new GUIContent("Asset Pack Directory", "The directory in the project where the built DLC APK will be placed along with the generated build.gradle");
        private static readonly GUIContent deliveryTypeLabel = new GUIContent("Delivery Type", "The play store delivery mode used to install the DLC content. Options are `install-time`, `fast-follow` and `on-demand`");
        
        private DLCProfile profile = null;
        private DLCPlatformProfile[] platforms = null;
        private List<BuildTarget> missingPlatforms = null;
        private Editor customMetadataEditor = null;
        private GUIContent buildSuccessIcon = null;
        private GUIContent buildErrorIcon = null;
        private GUIContent viewFolderIcon = null;
        private GUIContent pingAssetIcon = null;
        private GUIContent platformSpecificIcon = null;
        private GUIContent[] platformIcons = null;
        private bool tableStyle = true;

        private bool buildResultExpanded = false;
        private bool metadataExpanded = false;
        private bool customMetadataExpanded = false;
        private bool iconsExpanded = false;
        private bool assetsExpanded = false;
        private bool platformsExpanded = true;
        private int selectedPlatform = 0;

        private string[] includedAssetPaths = null;

        // Properties
        private string[] IncludedAssetPaths
        {
            get
            {
                if (includedAssetPaths == null)
                    includedAssetPaths = DLCBuildPipeline.GetAllAssetPathsForDLCProfile(profile, false, false);

                return includedAssetPaths;
            }
        }

        // Methods
        private void OnEnable()
        {
            profile = (DLCProfile)target;

            // Init custom metadata inspector
            if(profile.CustomMetadata != null)
                Editor.CreateCachedEditor(profile.CustomMetadata, null, ref customMetadataEditor);

            // Update platform contents
            RebuildPlatforms();


            // Update shared icons
            buildSuccessIcon = EditorGUIUtility.IconContent("d_FilterSelectedOnly");
            buildSuccessIcon.tooltip = "The last build attempt was successful";
            buildErrorIcon = EditorGUIUtility.IconContent("d_console.erroricon");
            buildErrorIcon.tooltip = "The last build attempt failed!";
            viewFolderIcon = EditorGUIUtility.IconContent("d_Project");
            viewFolderIcon.tooltip = "Open folder location";
            pingAssetIcon = EditorGUIUtility.IconContent("Lighting");
            pingAssetIcon.tooltip = "Ping asset in project window";
            platformSpecificIcon = EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow");

            // Add modification listener
            DLCAssetModifiedProcessor.OnModifiedAsset.AddListener(OnAssetModified);


            // Clear cache
            ResetCachedAssetPaths();
        }

        private void OnDisable()
        {
            // Remove modification listener
            DLCAssetModifiedProcessor.OnModifiedAsset.RemoveListener(OnAssetModified);

            // Clear cache
            ResetCachedAssetPaths();
        }

        private void ResetCachedAssetPaths()
        {
            // Clear cache
            includedAssetPaths = null;
        }

        private void RebuildPlatforms()
        {
            // Update platform contents
            platforms = profile.Platforms;
            missingPlatforms = profile.GetMissingDLCPlatforms();
            platformIcons = new GUIContent[platforms.Length + 1];

            platformIcons[0] = EditorGUIUtility.IconContent("CustomTool");

            for (int i = 1; i < platformIcons.Length; i++)
            {
                platformIcons[i] = GUIIcons.GetIconForPlatform(platforms[i - 1].Platform);
            }
        }

        private void OnAssetModified()
        {
            // Reset cached paths and force them to be recalculated
            includedAssetPaths = null;
        }

        public override void OnInspectorGUI()
        {
            tableStyle = true;


            // Make sure profile path is synced
            try
            {
                profile.UpdateDLCContentPath();
            }
            catch { }


            // Display build required warning
            if(profile.EnabledForBuild == true && IncludedAssetPaths.Length > 0 && profile.DLCRebuildRequired == true)
            {
                EditorGUILayout.HelpBox("The DLC has modified assets or settings and needs to be rebuilt to sync changes!", MessageType.Warning);
            }

            // Display no assets hint
            if(profile.EnabledForBuild == true && IncludedAssetPaths.Length == 0)
            {
                EditorGUILayout.HelpBox("The DLC does not have any content yet. Create assets here or in a sub folder to include them in this DLC!", MessageType.Warning);
            }

            // Display no platforms hint
            if (profile.EnabledForBuild == true)
            {
                if (profile.IsAnyPlatformEnabled() == false)
                    EditorGUILayout.HelpBox("The DLC is enabled for build but does not have any platforms enabled. No DLC content will be produced until a platform is enabled for build!", MessageType.Warning);
            }
            else
                EditorGUILayout.HelpBox("The DLC is not enabled for build and will be ignored by the DLC pipeline. You can still build this DLC manually using the `Build DLC` button below", MessageType.Warning);


            ProfileInfoGUI();
            BuildResultGUI();
            MetadataGUI();
            IconsGUI();
            AssetsGUI();
            PlatformsGUI();


            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            // Build buttons
            GUILayout.BeginHorizontal();
            {
                // Draw build button
                if (GUILayout.Button("Build DLC") == true)
                {
                    // Build this profile for all platforms
                    DLCBuildPipeline.BuildDLCContent(profile);
                }
                GUILayout.Space(-6);
                if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.Width(20)) == true)
                {
                    // Create the menu
                    GenericMenu menu = new GenericMenu();

                    // Rebuild DLC
                    menu.AddItem(new GUIContent("Rebuild DLC"), false, () =>
                    {
                        // Force rebuild this profile for all platforms
                        DLCBuildPipeline.BuildDLCContent(profile, null, null, DLCBuildOptions.ForceRebuild | DLCBuildOptions.DebugScripting_UseConfig);
                    });

                    foreach (DLCPlatformProfile platformProfile in profile.Platforms)
                    {
                        if (platformProfile.Enabled == true)
                        {
                            menu.AddItem(new GUIContent(string.Format("Rebuild DLC Platform/{0}", platformProfile.PlatformFriendlyName)), false, () =>
                            {
                                // Build this profile only for the specified platform
                                DLCBuildPipeline.BuildDLCContent(profile, null, new BuildTarget[] { platformProfile.Platform }, DLCBuildOptions.ForceRebuild | DLCBuildOptions.DebugScripting_UseConfig);
                            });
                        }
                    }

                    // Build the menu
                    menu.AddSeparator("");
                    foreach(DLCPlatformProfile platformProfile in profile.Platforms)
                    {
                        if (platformProfile.Enabled == true)
                        {
                            menu.AddItem(new GUIContent(string.Format("Build DLC Platform/{0}", platformProfile.PlatformFriendlyName)), false, () =>
                            {
                                // Build this profile only for the specified platform
                                DLCBuildPipeline.BuildDLCContent(profile, null, new BuildTarget[] { platformProfile.Platform });
                            });
                        }
                    }                    

                    // Show output location
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Show Output Folder"), false, () =>
                    {
                        EditorUtility.RevealInFinder(profile.DLCBuildPath);
                    });

                    // Clean output location
                    menu.AddItem(new GUIContent("Clean Output Folder"), false, () =>
                    {
                        if(EditorUtility.DisplayDialog("Clean Output Folder for this Profile?", "This will delete any built DLC content in the output location for this profile only", "Confirm", "Cancel") == true)
                        {
                            foreach (DLCPlatformProfile platformProfile in profile.platforms)
                            {
                                // Get the file path
                                string outputFile = profile.GetPlatformOutputPath(platformProfile.Platform);

                                // Delete the file
                                if (File.Exists(outputFile) == true)
                                    File.Delete(outputFile);
                            }
                        }
                    });

                    // Show the menu
                    menu.ShowAsContext();
                }
            }
            GUILayout.EndHorizontal();

        }

        private void ProfileInfoGUI()
        {
            // Guid
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(guidLabel, GUIStyles.LabelWidth);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(profile.DLCGuid);
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();

            // Content path
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(contentFolderLabel, GUIStyles.LabelWidth);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(profile.DLCContentPath);
                EditorGUI.EndDisabledGroup();

                // Goto button
                if(GUILayout.Button(viewFolderIcon, GUILayout.Width(28)) == true)
                    EditorUtility.RevealInFinder(profile.DLCContentPath);
            }
            GUILayout.EndHorizontal();

            // Build path
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(buildFolderLabel, GUIStyles.LabelWidth);
                string result = EditorGUILayout.TextField(profile.DLCBuildPath);

                // Check for changed
                if (result != profile.DLCBuildPath)
                    profile.DLCBuildPath = result;

                // Goto button
                if (GUILayout.Button(viewFolderIcon, GUILayout.Width(28)) == true)
                    EditorUtility.RevealInFinder(profile.DLCBuildPath);
            }
            GUILayout.EndHorizontal();

            // Name
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(nameLabel, GUIStyles.LabelWidth);
                string result = EditorGUILayout.TextField(profile.DLCName);

                // Check for changed
                if (result != profile.DLCName)
                    profile.DLCName = result;
            }
            GUILayout.EndHorizontal();

            // Version
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(versionLabel, GUIStyles.LabelWidth);
                string result = EditorGUILayout.TextField(profile.DLCVersionString);

                // Check for changed
                if (result != profile.DLCVersionString)
                    profile.DLCVersionString = result;
            }
            GUILayout.EndHorizontal();

            // Signing
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(signingLabel, GUIStyles.LabelWidth);
                bool result = EditorGUILayout.Toggle(profile.SignDLC);

                // Check for changed
                if (result != profile.SignDLC)
                    profile.SignDLC = result;
            }
            GUILayout.EndHorizontal();

            // Sign with version
            EditorGUI.BeginDisabledGroup(profile.SignDLC == false);
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(signingWithVersionLabel, GUIStyles.LabelWidth);
                bool result = EditorGUILayout.Toggle(profile.SignDLCVersion);

                // Check for changed
                if (result != profile.SignDLCVersion)
                    profile.SignDLCVersion = result;
            }
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            // Enabled for build
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(enabledLabel, GUIStyles.LabelWidth);
                bool result = EditorGUILayout.Toggle(profile.EnabledForBuild);

                // Check for changed
                if (result != profile.EnabledForBuild)
                    profile.EnabledForBuild = result;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void BuildResultGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                buildResultExpanded = EditorGUILayout.Foldout(buildResultExpanded, GUIContent.none);
                GUILayout.Label("Build Result", EditorStyles.largeLabel);
            }
            GUILayout.EndHorizontal();

            if(buildResultExpanded == true)
            {
                GUILayout.Space(-4);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {

                    // Check for any build
                    if (profile.HasLastBuildTime == true)
                    {
                        // Last Build Time
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            GUILayout.Label(lastBuildTimeLabel, GUIStyles.LabelWidth);
                            GUILayout.Label(GetLastBuildTimeFriendlyString(profile.LastBuildTime));
                        }
                        GUILayout.EndHorizontal();

                        // Last build result
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            int successful = profile.LastSuccessfulBuildPlatformCount;
                            int total = profile.LastBuildPlatformCount;

                            GUILayout.Label(lastBuildResultLabel, GUIStyles.LabelWidth);
                            GUILayout.Label(successful == total
                                ? string.Format("Successful ({0}/{1})", successful, total)
                                : string.Format("Failed ({0}/{1})", successful, total));

                            // Draw icon
                            GUILayout.Label(successful == total ? buildSuccessIcon : buildErrorIcon, GUILayout.Width(24));
                        }
                        GUILayout.EndHorizontal();

                        // Last build target info
                        foreach(BuildTarget buildTarget in profile.LastBuildTargets)
                        {
                            // Get the platform profile
                            DLCPlatformProfile platformProfile = profile.GetPlatform(buildTarget);

                            // Last build result
                            int height = 10;
                            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle), GUILayout.Height(height));
                            {
                                GUILayout.Space(30);
                                GUILayout.Label("-" + platformProfile.PlatformFriendlyName, EditorStyles.miniLabel, GUILayout.Width(EditorGUIUtility.labelWidth - 28), GUILayout.Height(height));
                                GUILayout.Label(platformProfile.LastBuildSuccess == true
                                    ? "Successful"
                                    : "Failed", EditorStyles.miniLabel, GUILayout.Height(height));

                                // Draw icon
                                GUILayout.Label(platformProfile.lastBuildSuccess == true ? buildSuccessIcon : buildErrorIcon, GUILayout.Width(24), GUILayout.Height(height));
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("This DLC has not been built yet for any platforms. The build result will show up here once the first build attempt has been run!", MessageType.Info);
                    }
                }
                GUILayout.EndVertical();


            //    // Description
            //    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            //    {
            //        GUILayout.Label("Last Build Result", GUIStyles.LabelWidth);
            //        string result = EditorGUILayout.TextArea("Success", GUILayout.Height(40));

            //        // Check for changed
            //        if (result != profile.Description)
            //            profile.Description = result;
            //    }
            //    GUILayout.EndHorizontal();

            }
        }

        private void MetadataGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                metadataExpanded = EditorGUILayout.Foldout(metadataExpanded, GUIContent.none);
                GUILayout.Label("Metadata", EditorStyles.largeLabel);
            }
            GUILayout.EndHorizontal();


            if (metadataExpanded == true)
            {
                GUILayout.Space(-4);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Description
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(descriptionLabel, GUIStyles.LabelWidth);
                        string result = GUILayout.TextArea(profile.Description, GUILayout.Height(40));

                        // Check for changed
                        if (result != profile.Description)
                            profile.Description = result;
                    }
                    GUILayout.EndHorizontal();

                    // Developer
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(developerLabel, GUIStyles.LabelWidth);
                        string result = EditorGUILayout.TextField(profile.Developer);

                        // Check for changed
                        if (result != profile.Developer)
                            profile.Developer = result;
                    }
                    GUILayout.EndHorizontal();

                    // Publisher
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(publisherLabel, GUIStyles.LabelWidth);
                        string result = EditorGUILayout.TextField(profile.Publisher);

                        // Check for changed
                        if (result != profile.Publisher)
                            profile.Publisher = result;
                    }
                    GUILayout.EndHorizontal();


                    // Custom metadata
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        int offset = 0;

                        // Show foldout
                        if (profile.CustomMetadata != null)
                        {
                            GUILayout.Space(16);
                            customMetadataExpanded = EditorGUILayout.Foldout(customMetadataExpanded, GUIContent.none);
                            GUILayout.Space(-50);
                            offset = 26;
                        }

                        GUILayout.Label(customMetadataLabel, GUILayout.Width(EditorGUIUtility.labelWidth - offset));
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField(profile.CustomMetadata != null ? profile.CustomMetadata.GetType().FullName : "(None)");
                        EditorGUI.EndDisabledGroup();

                        // Select button
                        if(GUILayout.Button(EditorGUIUtility.IconContent("CustomTool")) == true)
                        {
                            ShowCustomMetadataSelectionMenu();
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Check for expanded
                    if (customMetadataExpanded == true)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(20);
                            GUILayout.BeginVertical();
                            {
                                // Draw the custom metadata
                                if (profile.CustomMetadata != null && customMetadataEditor != null)
                                {
                                    // Check for changes to metadata
                                    if (customMetadataEditor.DrawDefaultInspector() == true)
                                        profile.MarkAsModified();

                                }
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }

                }
                GUILayout.EndVertical();
            }
        }

        private void IconsGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                iconsExpanded = EditorGUILayout.Foldout(iconsExpanded, GUIContent.none);
                GUILayout.Label("Icons", EditorStyles.largeLabel);
            }
            GUILayout.EndHorizontal();


            if (iconsExpanded == true)
            {
                GUILayout.Space(-4);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Small Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label(smallIconLabel, GUIStyles.LabelWidth);

                        if (profile.smallIcon != null)
                            GUILayout.Label(string.Format("  ({0} x {1})", profile.smallIcon.width, profile.smallIcon.height), EditorStyles.miniLabel);

                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        Texture2D result = EditorGUILayout.ObjectField(profile.smallIcon, typeof(Texture2D), false, GUILayout.Width(50), GUILayout.Height(50)) as Texture2D;

                        // Check for changed
                        if (result != profile.SmallIcon)
                            profile.SmallIcon = result;
                    }
                    GUILayout.EndHorizontal();

                    // Medium Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label(mediumIconLabel, GUIStyles.LabelWidth);

                        if (profile.mediumIcon != null)
                            GUILayout.Label(string.Format("  ({0} x {1})", profile.mediumIcon.width, profile.mediumIcon.height), EditorStyles.miniLabel);

                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        Texture2D result = EditorGUILayout.ObjectField(profile.MediumIcon, typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60)) as Texture2D;

                        // Check for changed
                        if (result != profile.MediumIcon)
                            profile.MediumIcon = result;
                    }
                    GUILayout.EndHorizontal();

                    // Large Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label(largeIconLabel, GUIStyles.LabelWidth);

                        if (profile.largeIcon != null)
                            GUILayout.Label(string.Format("  ({0} x {1})", profile.largeIcon.width, profile.largeIcon.height), EditorStyles.miniLabel);

                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        Texture2D result = EditorGUILayout.ObjectField(profile.LargeIcon, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70)) as Texture2D;

                        // Check for changed
                        if (result != profile.LargeIcon)
                            profile.LargeIcon = result;
                    }
                    GUILayout.EndHorizontal();

                    // Extra Large Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.BeginVertical();
                        GUILayout.Label(extraLargeIconLabel, GUIStyles.LabelWidth);

                        if (profile.extraLargeIcon != null)
                            GUILayout.Label(string.Format("  ({0} x {1})", profile.extraLargeIcon.width, profile.extraLargeIcon.height), EditorStyles.miniLabel);

                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        Texture2D result = EditorGUILayout.ObjectField(profile.ExtraLargeIcon, typeof(Texture2D), false, GUILayout.Width(80), GUILayout.Height(80)) as Texture2D;

                        // Check for changed
                        if (result != profile.ExtraLargeIcon)
                            profile.ExtraLargeIcon = result;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        private void AssetsGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                assetsExpanded = EditorGUILayout.Foldout(assetsExpanded, GUIContent.none);
                GUILayout.Label("Assets", EditorStyles.largeLabel);
            }
            GUILayout.EndHorizontal();

            if (assetsExpanded == true)
            {
                // Check for any
                if (IncludedAssetPaths.Length > 0)
                {
                    foreach (string assetPath in IncludedAssetPaths)
                    {
                        // Check for in platform specific folder
                        string platformFolder;
                        bool isPlatformSpecific = DLCBuildContext.IsSpecialPlatformFolder(assetPath, out platformFolder);

                        // Asset path
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            // Draw icon
                            GUILayout.Label(GUIContent.none, GUILayout.Width(16), GUILayout.Height(16));

                            Rect iconRect = GUILayoutUtility.GetLastRect();
                            GUI.DrawTexture(iconRect, GetIconTextureForAssetPath(assetPath));

                            // Draw path
                            GUILayout.Label(assetPath, GUILayout.MaxWidth(Screen.width - 50));

                            // Platform specific info
                            if (isPlatformSpecific == true)
                            {
                                platformSpecificIcon.tooltip = "Asset will only be included for platform: " + platformFolder;
                                GUILayout.Label(platformSpecificIcon, GUILayout.Width(28));
                                GUILayout.Space(-12);
                            }

                            // Ping button
                            if (GUILayout.Button(pingAssetIcon, EditorStyles.label, GUILayout.Width(28)) == true)
                                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object)));
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        EditorGUILayout.HelpBox("There are no assets associated with this DLC yet. This section will update automatically as assets are created!", MessageType.Info);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void PlatformsGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                platformsExpanded = EditorGUILayout.Foldout(platformsExpanded, GUIContent.none);
                GUILayout.Label("Platforms", EditorStyles.largeLabel);
            }
            GUILayout.EndHorizontal();


            if (platformsExpanded == true)
            {
                GUILayout.Space(-4);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    GUILayout.BeginHorizontal();
                    {
                        // Show toolbar
                        selectedPlatform = GUILayout.Toolbar(selectedPlatform, platformIcons, GUILayout.Width(Screen.width - 60), GUILayout.Height(30));
                        GUILayout.Space(-6);

                        // Add button
                        EditorGUI.BeginDisabledGroup(missingPlatforms.Count == 0);
                        if(GUILayout.Button(EditorGUIUtility.IconContent("CreateAddNew"), GUILayout.Height(30)) == true)
                        {
                            // Show menu
                            ShowAddPlatformSelectionMenu();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    GUILayout.EndHorizontal();


                    // Platform name
                    bool deletePlatform = false;
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(selectedPlatform == 0
                            ? "All Platforms"
                            : platforms[selectedPlatform - 1].PlatformFriendlyName, EditorStyles.largeLabel);
                        GUILayout.FlexibleSpace();

                        if(selectedPlatform > 0)
                        {
                            if(GUILayout.Button(EditorGUIUtility.IconContent("d_winbtn_mac_close_a"), EditorStyles.label) == true)
                            {
                                // Set delete option
                                deletePlatform = true;
                            }
                        }
                    }
                    GUILayout.EndHorizontal();


                    // Handle delete
                    if(deletePlatform == true)
                    {
                        // Get platform name
                        string platformName = platforms[selectedPlatform - 1].PlatformFriendlyName;

                        if (EditorUtility.DisplayDialog(
                            string.Format("Delete {0} Platform", platformName),
                            "Are you sure you want to delete this platform profile? All platform settings will be lost, but you can add the platform again at a later time", 
                            "Confirm", "Cancel") == true)
                        {
                            // Remove from profile
                            profile.DeleteDLCPlatform(platforms[selectedPlatform - 1].Platform);

                            // Update platforms UI
                            RebuildPlatforms();

                            // Reset selected platform
                            if (selectedPlatform >= platforms.Length + 1)
                                selectedPlatform = platforms.Length;

                            // Refresh window
                            Repaint();
                            return;
                        }
                    }


                    // Enabled
                    bool enabled = true;
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        enabled = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.Enabled);

                        GUILayout.Label(platformEnabledLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        bool result = EditorGUILayout.Toggle(enabled);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != enabled)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.Enabled = result);
                    }
                    GUILayout.EndHorizontal();

                    // Platform Specific folder
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        string value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.PlatformSpecificFolder);

                        GUILayout.Label(platformSpecificFolderLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        string result = EditorGUILayout.TextField(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.PlatformSpecificFolder = result);
                    }
                    GUILayout.EndHorizontal();

                    // Platform output path
                    if (selectedPlatform != 0)
                    {
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            string platformOutput = profile.GetPlatformOutputPath(platforms[selectedPlatform - 1].Platform);

                            GUILayout.Label(platformBuildOutputLabel, GUIStyles.LabelWidth);
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.TextField(platformOutput);
                            EditorGUI.EndDisabledGroup();

                            // Goto button - Only show if there is output available
                            EditorGUI.BeginDisabledGroup(File.Exists(platformOutput) == false);
                            if (GUILayout.Button(viewFolderIcon, GUILayout.Width(28)) == true)
                                EditorUtility.RevealInFinder(platformOutput);
                            EditorGUI.EndDisabledGroup();
                        }
                        GUILayout.EndHorizontal();
                    }

                    // Unique key
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        string value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.DlcUniqueKey);

                        GUILayout.Label(platformUniqueKeyLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        string result = EditorGUILayout.TextField(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.DlcUniqueKey = result);
                    }
                    GUILayout.EndHorizontal();

                    // Extension
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        string value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.DLCExtension);

                        GUILayout.Label(platformExtensionLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        string result = EditorGUILayout.TextField(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.DLCExtension = result);
                    }
                    GUILayout.EndHorizontal();

                    // Use compression
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        bool value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.UseCompression);

                        GUILayout.Label(useCompressionLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        bool result = EditorGUILayout.Toggle(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.UseCompression = result);
                    }
                    GUILayout.EndHorizontal();

                    // Strict build
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        bool value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.StrictBuild);

                        GUILayout.Label(strictBuildLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        bool result = EditorGUILayout.Toggle(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.StrictBuild = result);
                    }
                    GUILayout.EndHorizontal();

                    // IAP name
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        string value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.DLCIAPName);

                        GUILayout.Label(dlcIAPNameLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        string result = EditorGUILayout.TextField(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if(result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.DLCIAPName = result);
                    }
                    GUILayout.EndHorizontal();

                    // Preload shared assets
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        bool value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.PreloadSharedAssets);

                        GUILayout.Label(preloadSharedAssetsLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        bool result = EditorGUILayout.Toggle(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.PreloadSharedAssets = result);
                    }
                    GUILayout.EndHorizontal();

                    // Preload scene assets
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        bool value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.PreloadSceneAssets);

                        GUILayout.Label(preloadSceneAssetsLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        bool result = EditorGUILayout.Toggle(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.PreloadSceneAssets = result);
                    }
                    GUILayout.EndHorizontal();

                    // Ship with game
                    bool shipWithGame = false;
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        bool value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.ShipWithGame);

                        GUILayout.Label(shipWithGameLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        bool result = EditorGUILayout.Toggle(value);
                        EditorGUI.showMixedValue = false;
                        shipWithGame = value;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.ShipWithGame = result);
                    }
                    GUILayout.EndHorizontal();

                    // Ship with game directory
                    EditorGUI.BeginDisabledGroup(shipWithGame == false);
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        ShipWithGameDirectory value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.ShipWithGameDirectory);

                        GUILayout.Label(shipWithGameDirectoryLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        ShipWithGameDirectory result = (ShipWithGameDirectory)EditorGUILayout.EnumPopup(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.ShipWithGameDirectory = result);
                    }
                    GUILayout.EndHorizontal();

                    // Ship with game path
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        // Get option
                        bool mixed;
                        string value = GetPlatformPropertyMultiple(platforms, selectedPlatform, out mixed, p => p.ShipWithGamePath);

                        GUILayout.Label(shipWithGamePathLabel, GUIStyles.LabelWidth);
                        EditorGUI.showMixedValue = mixed;
                        string result = EditorGUILayout.TextField(value);
                        EditorGUI.showMixedValue = false;

                        // Check for changed
                        if (result != value)
                            SetPlatformPropertyMultiple(platforms, selectedPlatform, p => p.ShipWithGamePath = result);
                    }
                    GUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();


                    // Draw platforms specific options
                    PlatformSpecificGUI();

                    // Show hint
                    if (selectedPlatform == 0)
                        EditorGUILayout.HelpBox("Changes made to these options will apply to all platforms!", MessageType.Info);
                }
                GUILayout.EndVertical();

            }
        }

        private void PlatformSpecificGUI()
        {
            // Check for android
            if(selectedPlatform != 0 && platforms[selectedPlatform - 1] is DLCPlatformProfileAndroid)
            {
                // Get platform specific
                DLCPlatformProfileAndroid androidPlatform = platforms[selectedPlatform - 1] as DLCPlatformProfileAndroid;

                // Header
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Android Specific", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // Asset pack directory
                GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                {
                    GUILayout.Label(dlcAssetPackDirectoryLabel, GUIStyles.LabelWidth);
                    string result = EditorGUILayout.TextField(androidPlatform.DLCAssetPackDirectory);

                    // Check for changed
                    if (result != androidPlatform.DLCAssetPackDirectory)
                    {
                        androidPlatform.DLCAssetPackDirectory = result;
                        EditorUtility.SetDirty(target);
                    }
                }
                GUILayout.EndHorizontal();

                // Delivery type
                GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                {
                    GUILayout.Label(deliveryTypeLabel, GUIStyles.LabelWidth);
                    string result = EditorGUILayout.TextField(androidPlatform.DeliveryType);

                    // Check for changed
                    if (result != androidPlatform.DeliveryType)
                    {
                        androidPlatform.DeliveryType = result;
                        EditorUtility.SetDirty(target);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }


        

        private T GetPlatformPropertyMultiple<T>(DLCPlatformProfile[] platforms, int index, out bool mixed, Func<DLCPlatformProfile, T> selector)
        {            
            if (index > 0)
            {
                mixed = false;
                return Enumerable.Repeat(platforms[index - 1], 1)
                    .Select(selector)
                    .First();
            }
            else
            {
                T first = default;
                bool assigned = false;
                mixed = false;
                foreach (T val in platforms.Select(selector))
                {
                    if (assigned == false)
                    {
                        first = val;
                        assigned = true;
                    }
                    if (val != null && val.Equals(first) == false)
                    {
                        mixed = true;
                        return default;
                    }
                }
                return first;
            }            
        }

        private void SetPlatformPropertyMultiple(DLCPlatformProfile[] platforms, int index, Action<DLCPlatformProfile> assign)
        {
            if(index > 0)
            {
                assign(platforms[index - 1]);
            }
            else
            {
                foreach(DLCPlatformProfile platform in platforms)
                {
                    assign(platform);
                }
            }

            // Set dirty
            EditorUtility.SetDirty(target);
        }

        private void ShowCustomMetadataSelectionMenu()
        {
            // Select context menu
            GenericMenu menu = new GenericMenu();

            List<Type> availableMetadataTypes = new List<Type>();

            // Get assembly name for `DLCToolkit.dll`
            string dlcToolkitAsmName = typeof(DLCCustomMetadata).Assembly.GetName().Name;

            // Find all types to show
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Check for this
                if (asm == typeof(DLCCustomMetadata).Assembly)
                    continue;

                bool hasReference = false;

                foreach(AssemblyName nameInfo in asm.GetReferencedAssemblies())
                {
                    if(string.Compare(dlcToolkitAsmName, nameInfo.Name) == 0)
                    {
                        hasReference = true;
                        break;
                    }
                }

                // Check for reference - we can skip expensive reflection checks because a reference to DLCToolkit is required in order to derive from DLCCustomMetadata
                if (hasReference == false)
                    continue;

                try
                {
                    foreach(Type type in asm.GetTypes())
                    {
                        // Check for derived
                        if(type.IsClass == true && typeof(DLCCustomMetadata).IsAssignableFrom(type) == true)
                        {
                            // Check for abstract
                            if (type.IsAbstract == true)
                                continue;

                            // Register the type
                            availableMetadataTypes.Add(type);
                        }
                    }
                }
                catch(TypeLoadException e)
                {
                    Debug.LogWarning(string.Format("Could not load types for assembly `{0}`. Custom metadata selection may not be available", asm));
                    UnityEngine.Debug.LogException(e);
                }
            }

            // Add default menu item
            menu.AddItem(new GUIContent("None"), false, () =>
            {
                // Reset to null
                profile.CustomMetadata = null;

                // Reset drawer
                customMetadataEditor = null;
                customMetadataExpanded = false;
            });

            // Add separator
            if(availableMetadataTypes.Count > 0)
                menu.AddSeparator("");

            // Build menu
            foreach(Type type in availableMetadataTypes)
            {
                // Add the menu item
                menu.AddItem(new GUIContent(type.FullName), false, (object state) =>
                {
                    // Create instance
                    DLCCustomMetadata customMeta = CreateInstance(type) as DLCCustomMetadata;

                    // Check for error
                    if(customMeta == null)
                    {
                        Debug.LogError("Failed to create instance of custom metadata type: " + type);
                        return;
                    }

                    // Register custom metadata
                    profile.CustomMetadata = customMeta;
                    customMetadataExpanded = true;

                    // Create custom editor
                    Editor.CreateCachedEditor(profile.CustomMetadata, null, ref customMetadataEditor);
                }, type);
            }

            menu.ShowAsContext();
        }

        private void ShowAddPlatformSelectionMenu()
        {
            // Create context menu
            GenericMenu menu = new GenericMenu();

            // Add all missing platforms
            foreach(BuildTarget missingPlatform in missingPlatforms)
            {
                // Get the friendly display name
                string platformName = DLCPlatformProfile.GetFriendlyPlatformName(missingPlatform);

                // Create menu
                menu.AddItem(new GUIContent(platformName), false, () =>
                {
                    // Add new platform
                    profile.AddDLCPlatform(missingPlatform);

                    // Update platform contents
                    RebuildPlatforms();

                    // Repaint the window
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        private static Texture2D GetIconTextureForAssetPath(string assetPath)
        {
            // Get the asset type
            Type type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);

            // Try to get icon
            return AssetPreview.GetMiniTypeThumbnail(type);
        }

        private static string GetLastBuildTimeFriendlyString(DateTime lastBuildTime)
        {
            // Calculate amount of time that has passed
            TimeSpan span = DateTime.Now - lastBuildTime;

            // Check for over a year
            if (span.Days > 365)
            {
                int years = (span.Days / 365);
                if (span.Days % 365 != 0)
                    years += 1;
                return string.Format("About {0} {1} ago",
                years, years == 1 ? "year" : "years");
            }
            // Check for over a month
            if (span.Days > 30)
            {
                int months = (span.Days / 30);
                if (span.Days % 31 != 0)
                    months += 1;
                return string.Format("About {0} {1} ago",
                months, months == 1 ? "month" : "months");
            }
            // Check for over a day
            if (span.Days > 0)
            {
                return string.Format("About {0} {1} ago",
                    span.Days, span.Days == 1 ? "day" : "days");
            }
            // Check for over an hour
            if (span.Hours > 0)
            {
                return string.Format("About {0} {1} ago",
                    span.Hours, span.Hours == 1 ? "hour" : "hours");
            }
            // Check for over a minute
            if (span.Minutes > 0)
            {
                return string.Format("About {0} {1} ago",
                    span.Minutes, span.Minutes == 1 ? "minute" : "minutes");
            }
            // Check for over 5 seconds
            if (span.Seconds > 5)
            {
                return string.Format("About {0} seconds ago", span.Seconds);
            }
            // Check for now
            if (span.Seconds <= 5)
            {
                return "Just now";
            }
            return string.Empty;
        }
    }
}
