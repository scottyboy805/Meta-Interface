using Codice.Client.BaseCommands.Differences;
using DLCToolkit.Assets;
using DLCToolkit.BuildTools;
using DLCToolkit.BuildTools.Events;
using DLCToolkit.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    internal sealed class DLCBrowser : EditorWindow
    {
        // Type
        [DLCPreBuild]
        private sealed class DLCBrowserStartBuildNotification : DLCBuildEvent
        {
            // Methods
            public override void OnBuildEvent()
            {
                if (DLCBrowser.instance != null)
                    DLCBrowser.instance.OnDLCBuildStarted();
            }
        }

        [DLCPostBuild]
        private sealed class DLCBrowserEndBuildNotification : DLCBuildEvent
        {
            // Methods
            public override void OnBuildEvent()
            {
                if (DLCBrowser.instance != null)
                    DLCBrowser.instance.OnDLCBuildFinished();
            }
        }

        // Private
        private static readonly GUIContent[] toolbarLabels = new GUIContent[]
        {
            new GUIContent("DLC File", "Select DLC content from a file path"),
            new GUIContent("DLC Installed", "Select DLC content that has been built for the game and is installed"),
            new GUIContent("DLC Loaded", "Select DLC content that is currently loaded in memory. Only available while in play mode!"),
        };

        private static readonly GUIContent guidLabel = new GUIContent("DLC GUID", "The unique Guid assigned to this DLC");
        private static readonly GUIContent uniqueKeyLabel = new GUIContent("DLC Unique Key", "The DLC unique key for the DLC");
        private static readonly GUIContent nameLabel = new GUIContent("DLC Name", "The name of this DLC content");
        private static readonly GUIContent versionLabel = new GUIContent("DLC Version", "The current version of this DLC in the format X.X.X");
        private static readonly GUIContent signingLabel = new GUIContent("DLC Signing", "Has the DLC content be signed during build so that it will only be loadable by this game project");
        private static readonly GUIContent signingWithVersionLabel = new GUIContent("DLC Version Signing", "Has the DLC content be version signed, meaning that it will only be loadable by the version of the game project it was built for");
        private static readonly GUIContent toolkitVersionLabel = new GUIContent("DLC Toolkit Version", "The version of Ultimate DLC Toolkit that was used to create this DLC, in the format X.X.X");
        private static readonly GUIContent unityVersionLabel = new GUIContent("Unity Version", "The version of Unity that was used to create this DLC, in the format X.X.X(fX)");
        private static readonly GUIContent descriptionLabel = new GUIContent("Description", "A short description for this DLC");
        private static readonly GUIContent developerLabel = new GUIContent("Developer", "The name of the developer or studio that created this DLC content");
        private static readonly GUIContent publisherLabel = new GUIContent("Publisher", "The name of the publisher who distributed this DLC content");
        private static readonly GUIContent buildTimeLabel = new GUIContent("Build Time", "The build timestamp for when this DLC content was produced");
        private static readonly GUIContent contentLabel = new GUIContent("Included Content", "The types of content that are included in this DLC");
        private static readonly GUIContent networkUniqueIDLabel = new GUIContent("Network Unique ID", "A unique id string (Formulated from unique key, version, build time stamp) that can be used to determine if DLC content loaded on 2 different devices are considered identical. Useful in network multiplayer games to ensure that all clients have the same DLC available. This value will change on each DLC build!");
        private static readonly GUIContent networkUniqueIDHashLabel = new GUIContent("Network Unique ID Hash", "Similar to Network Unique ID however this value is a derived hash of fixed size (64 characters) calculated from the DLC unique build fingerprint. This value will change on each DLC build!");
        private static readonly GUIContent smallIconLabel = new GUIContent("Small Icon", "The small icon for this DLC. Recommended to use a square image of approximately 32x32px");
        private static readonly GUIContent mediumIconLabel = new GUIContent("Medium Icon", "The medium icon for this DLC. Recommended to use a square image of approximately 64x64px");
        private static readonly GUIContent largeIconLabel = new GUIContent("Large Icon", "The large icon for this DLC. Recommended to use a square image of approximately 256x256px");
        private static readonly GUIContent extraLargeIconLabel = new GUIContent("Extra Large Icon", "The extra large icon for this DLC. Recommended to use a square image or approximately 512x512px");

        private string browseContentLocation = null;
        private DLCContent browseContent = null;
        private int includedIconCount = -1;
        private DLCSharedAsset[] includedAssets = null;
        private DLCSceneAsset[] includedScenes = null;
        private Dictionary<Assembly, Type[]> includedAssemblies = null;
        private Dictionary<Assembly, bool> expandedAssemblies = null;
        private GUIContent closeIcon = null;
        private GUIContent viewFolderIcon = null;

        private Vector2 scroll = default;
        private bool tableStyle = true;
        private int labelWidth = 0;

        private int selectedOption = 0;
        private bool metadataExpanded = true;
        private bool iconsExpanded = false;
        private bool assetsExpanded = false;
        private bool scenesExpanded = false;
        private bool scriptsExpanded = false;

        // Internal
        internal static DLCBrowser instance = null;

        // Properties
        private int IncludedIconCount
        {
            get
            {
                if(includedIconCount == -1)
                {
                    includedIconCount = 0;
                    
                    // Preset icons
                    if (browseContent.IconProvider.HasIcon(DLCIconType.Small) == true) includedIconCount++;
                    if (browseContent.IconProvider.HasIcon(DLCIconType.Medium) == true) includedIconCount++;
                    if (browseContent.IconProvider.HasIcon(DLCIconType.Large) == true) includedIconCount++;
                    if (browseContent.IconProvider.HasIcon(DLCIconType.ExtraLarge) == true) includedIconCount++;
                }
                return includedIconCount;
            }
        }

        private DLCSharedAsset[] IncludedAssets
        {
            get
            {
                if (includedAssets == null)
                    includedAssets = browseContent.SharedAssets.FindAll();

                return includedAssets;
            }
        }

        private DLCSceneAsset[] IncludedScenes
        {
            get
            {
                if(includedScenes == null)
                    includedScenes = browseContent.SceneAssets.FindAll();

                return includedScenes;
            }
        }

        private Dictionary<Assembly, Type[]> IncludedAssemblies
        {
            get
            {
                if(includedAssemblies == null)
                {
                    includedAssemblies = new Dictionary<Assembly, Type[]>();
                    expandedAssemblies = new Dictionary<Assembly, bool>();

                    foreach (Assembly asm in browseContent.scriptAssembly.AssembliesLoaded)
                    {
                        includedAssemblies[asm] = asm.GetTypes()
                            .Where(t => t.IsPublic == true || typeof(UnityEngine.Object).IsAssignableFrom(t) == true)
                            .ToArray();
                        expandedAssemblies[asm] = false;
                    }
                }
                return includedAssemblies;
            }
        }

        // Methods
        private void OnEnable()
        {
            instance = this;

            closeIcon = EditorGUIUtility.IconContent("d_winbtn_mac_close_a");
            closeIcon.tooltip = "Return to the main DLC browser window";
            viewFolderIcon = EditorGUIUtility.IconContent("d_Project");
            viewFolderIcon.tooltip = "Open folder location";
        }

        private void OnDisable()
        {
            instance = null;            

            CloseDLC();
        }

        private void OnGUI()
        {
            // Update label width
            labelWidth = (int)(Screen.width / 2.5);

            // Check for content selected
            if (browseContent == null)
            {
                OnSelectDLCGUI();
            }
            else
            {
                OnBrowseDLCGUI();
            }
        }

        internal void OnDLCBuildStarted()
        {
            browseContent.Dispose();
            browseContent = null;
        }

        internal void OnDLCBuildFinished()
        {
            // Recreate from path
            if(string.IsNullOrEmpty(browseContentLocation) == false)
            {
                // Load the DLC once again
                browseContent = DLC.LoadDLCFrom(browseContentLocation);
            }
        }

        private void CloseDLC()
        {
            if (browseContent != null)
            {
                includedIconCount = -1;

                // Clear collections
                includedAssets = null;
                includedScenes = null;
                includedAssemblies = null;
                expandedAssemblies = null;

                browseContentLocation = null;
                browseContent.Dispose();
                browseContent = null;
            }
        }

        private void OnSelectDLCGUI()
        {
            // Header
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select DLC To Browse", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);


            // Toolbar
            selectedOption = GUILayout.Toolbar(selectedOption, toolbarLabels);
            
            switch (selectedOption)
            {
                case 0: OnSelectDLCFileGUI(); break;
                case 1: OnSelectDLCInstalledGUI(); break;
                case 2: OnSelectDLCLoadedGUI(); break;
            }            
        }

        private void OnSelectDLCFileGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            {
                EditorGUILayout.HelpBox("Select the target DLC file from disk to begin exploring the content included. Only valid Ultimate DLC Toolkit format files are supported!", MessageType.Info);

                //// Last DLC path
                //GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                //{
                //    GUILayout.FlexibleSpace();
                //    GUILayout.Label("Last DLC Path", GUILayout.Width(labelWidth));
                //    GUILayout.Label("None");
                //    GUILayout.FlexibleSpace();
                //}
                //GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    //GUILayout.FlexibleSpace();
                    // Select button
                    if (GUILayout.Button("Select DLC File") == true)
                    {
                        // Open file browser
                        string dlcPath = EditorUtility.OpenFilePanel("Select DLC Content", "", "dlc,*");

                        // Check for DLC file
                        if (DLC.IsDLCFile(dlcPath) == true)
                        {
                            // Try to open
                            browseContentLocation = dlcPath;
                            browseContent = DLC.LoadDLCFrom(dlcPath);

                            // Try to shorten location
                            string locationRelative = FileUtil.GetProjectRelativePath(browseContentLocation);

                            if (string.IsNullOrEmpty(locationRelative) == false)
                                browseContentLocation = locationRelative;
                        }
                        else if (string.IsNullOrEmpty(dlcPath) == false)
                        {
                            EditorUtility.DisplayDialog("Invalid DLC Format", "The selected file is not a valid Ultimate DLC Toolkit format. Make sure you select a valid DLC file produced by Ultimate DLC Toolkit!", "Ok");
                        }
                    }

                    //// Running button
                    //EditorGUI.BeginDisabledGroup(Application.isPlaying == false);
                    //if (GUILayout.Button("Select Loaded DLC") == true)
                    //{

                    //}
                    //EditorGUI.EndDisabledGroup();
                    //GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void OnSelectDLCInstalledGUI()
        {

        }

        private void OnSelectDLCLoadedGUI()
        {
            if(Application.isPlaying == false)
            {
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                {
                    EditorGUILayout.HelpBox("Loaded DLC content can only be discovered while in play mode. Please enter play mode to continue and all loaded DLC content will be displayed here!", MessageType.Warning);

                    // Select button
                    if (GUILayout.Button("Enter Play Mode") == true)
                    {
                        EditorApplication.EnterPlaymode();
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
        }

        private void OnBrowseDLCGUI()
        {
            // Display header
            GUILayout.BeginVertical(GUILayout.Height(30));
            {
                // DLC main toolbar
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(30);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(browseContent.NameInfo.ToString(), EditorStyles.largeLabel);
                    GUILayout.FlexibleSpace();

                    // Close button
                    if (GUILayout.Button(closeIcon, EditorStyles.label, GUILayout.Width(24)) == true)
                    {
                        CloseDLC();
                    }
                }
                GUILayout.EndHorizontal();

                // Location toolbar
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Space(24);
                    GUILayout.Label(browseContentLocation, EditorStyles.centeredGreyMiniLabel);

                    // Open button
                    if(GUILayout.Button(viewFolderIcon, EditorStyles.label, GUILayout.Width(24), GUILayout.Height(13)) == true)
                    {
                        EditorUtility.RevealInFinder(browseContentLocation);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            // Check for null - DLC content was closed during this update
            if (browseContent == null)
                return;


            scroll = GUILayout.BeginScrollView(scroll);
            {
                MetadataInfoGUI();
                IconsGUI();
                AssetsGUI();
                ScenesGUI();
                ScriptsGUI();
            }
            GUILayout.EndScrollView();
        }

        private void MetadataInfoGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                metadataExpanded = EditorGUILayout.Foldout(metadataExpanded, GUIContent.none);
                GUILayout.Label("Metadata", EditorStyles.largeLabel);
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            if (metadataExpanded == true)
            {
                GUILayout.Space(-4);
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Unique Key
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(uniqueKeyLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextArea(browseContent.NameInfo.UniqueKey);
                    }
                    GUILayout.EndHorizontal();

                    // Guid
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(guidLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextArea(browseContent.Metadata.Guid);
                    }
                    GUILayout.EndHorizontal();

                    // Name
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(nameLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextArea(browseContent.NameInfo.Name);
                    }
                    GUILayout.EndHorizontal();

                    // Version
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(versionLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextArea(browseContent.NameInfo.Version.ToString());
                    }
                    GUILayout.EndHorizontal();

                    // Signed
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(signingLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.Toggle((browseContent.bundle.Flags & DLCBundle.ContentFlags.Signed) != 0);

                        // With version
                        GUILayout.Label(signingWithVersionLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.Toggle((browseContent.bundle.Flags & DLCBundle.ContentFlags.SignedWithVersion) != 0);
                    }
                    GUILayout.EndHorizontal();

                    // Toolkit version
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(toolkitVersionLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextArea(browseContent.Metadata.ToolkitVersion.ToString());
                    }
                    GUILayout.EndHorizontal();

                    // Unity version
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(unityVersionLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextArea(browseContent.Metadata.UnityVersion);
                    }
                    GUILayout.EndHorizontal();

                    // Description
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(descriptionLabel, GUILayout.Width(labelWidth));
                        GUILayout.TextArea(browseContent.Metadata.Description);
                    }
                    GUILayout.EndHorizontal();

                    // Developer
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(developerLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextField(browseContent.Metadata.Developer);
                    }
                    GUILayout.EndHorizontal();

                    // Publisher
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(publisherLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextField(browseContent.Metadata.Publisher);
                    }
                    GUILayout.EndHorizontal();

                    // Build Time
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(buildTimeLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextField(browseContent.Metadata.BuildTime.ToString());
                    }
                    GUILayout.EndHorizontal();

                    // Content
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(contentLabel, GUILayout.Width(labelWidth));

                        GUILayout.Label("Assets");
                        EditorGUILayout.Toggle((browseContent.Metadata.ContentFlags & DLCContentFlags.Assets) != 0);

                        GUILayout.Label("Scenes");
                        EditorGUILayout.Toggle((browseContent.Metadata.ContentFlags & DLCContentFlags.Scenes) != 0);

                        GUILayout.Label("Scripts");
                        EditorGUILayout.Toggle((browseContent.Metadata.ContentFlags & DLCContentFlags.Scripts) != 0);
                    }
                    GUILayout.EndHorizontal();

                    // Network Unique ID
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(networkUniqueIDLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextField(browseContent.Metadata.GetNetworkUniqueIdentifier(true));
                    }
                    GUILayout.EndHorizontal();

                    // Network Unique Hash
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(networkUniqueIDHashLabel, GUILayout.Width(labelWidth));
                        EditorGUILayout.TextField(browseContent.Metadata.GetNetworkUniqueIdentifierHash());
                    }
                    GUILayout.EndHorizontal();
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
                GUILayout.Label("Icons" + (iconsExpanded == true
                    ? string.Format(" ({0})", IncludedIconCount)
                    : string.Empty), EditorStyles.largeLabel);
                GUILayout.Space(60);
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
                        GUILayout.Label(smallIconLabel, GUILayout.Width(labelWidth));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.ObjectField(browseContent.IconProvider.LoadIcon(DLCIconType.Small), typeof(Texture2D), false, GUILayout.Width(50), GUILayout.Height(50));
                    }
                    GUILayout.EndHorizontal();

                    // Medium Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(mediumIconLabel, GUILayout.Width(labelWidth));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.ObjectField(browseContent.IconProvider.LoadIcon(DLCIconType.Medium), typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60));
                    }
                    GUILayout.EndHorizontal();

                    // Large Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(largeIconLabel, GUILayout.Width(labelWidth));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.ObjectField(browseContent.IconProvider.LoadIcon(DLCIconType.Large), typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
                    }
                    GUILayout.EndHorizontal();

                    // Extra Large Icon
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        GUILayout.Label(extraLargeIconLabel, GUILayout.Width(labelWidth));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.ObjectField(browseContent.IconProvider.LoadIcon(DLCIconType.ExtraLarge), typeof(Texture2D), false, GUILayout.Width(80), GUILayout.Height(80));
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
                GUILayout.Label("Assets" + (assetsExpanded == true
                    ? string.Format(" ({0})", IncludedAssets.Length)
                    : string.Empty)
                    , EditorStyles.largeLabel);
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            if (assetsExpanded == true)
            {
                // Check for any
                if (IncludedAssets.Length > 0)
                {
                    foreach (DLCSharedAsset asset in IncludedAssets)
                    {
                        // Asset path
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            // Draw icon
                            GUILayout.Space(24);
                            GUILayout.Label(GUIContent.none, GUILayout.Width(16), GUILayout.Height(16));

                            Rect iconRect = GUILayoutUtility.GetLastRect();
                            GUI.DrawTexture(iconRect, GetIconTextureForAsset(asset));

                            // Draw path
                            GUILayout.Label(asset.RelativeName, GUILayout.MaxWidth(Screen.width - 50));
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        EditorGUILayout.HelpBox("There are no shared assets included in this DLC!", MessageType.Info);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void ScenesGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                scenesExpanded = EditorGUILayout.Foldout(scenesExpanded, GUIContent.none);
                GUILayout.Label("Scenes" + (scenesExpanded == true
                    ? string.Format(" ({0})", IncludedScenes.Length)
                    : string.Empty)
                    , EditorStyles.largeLabel);
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();
            
            if (scenesExpanded == true)
            {
                // Check for any
                if (IncludedScenes.Length > 0)
                {
                    foreach (DLCSceneAsset scene in IncludedScenes)
                    {
                        // Asset path
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            // Draw icon
                            GUILayout.Space(24);
                            GUILayout.Label(GUIContent.none, GUILayout.Width(16), GUILayout.Height(16));

                            Rect iconRect = GUILayoutUtility.GetLastRect();
                            GUI.DrawTexture(iconRect, GetIconTextureForAsset(scene));

                            // Draw path
                            GUILayout.Label(scene.RelativeName, GUILayout.MaxWidth(Screen.width - 50));
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        EditorGUILayout.HelpBox("There are no scenes included in this DLC!", MessageType.Info);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void ScriptsGUI()
        {
            GUILayout.BeginHorizontal(GUIStyles.TableHeaderStyle);
            {
                GUILayout.Space(14);
                scriptsExpanded = EditorGUILayout.Foldout(scriptsExpanded, GUIContent.none);
                GUILayout.Label("Scripts" + (scriptsExpanded == true 
                    ? string.Format(" ({0})", IncludedAssemblies.Count) 
                    : string.Empty)
                    , EditorStyles.largeLabel);
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            if (scriptsExpanded == true)
            {
                // Check for any
                if ((browseContent.Metadata.ContentFlags & DLCContentFlags.Scripts) != 0)
                {
                    foreach (KeyValuePair<Assembly, Type[]> asmPair in IncludedAssemblies)
                    {
                        // Assembly name
                        GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                        {
                            // Update expanded
                            expandedAssemblies[asmPair.Key] = EditorGUILayout.Foldout(expandedAssemblies[asmPair.Key], GUIContent.none);
                            GUILayout.Space(-30);

                            // Draw icon
                            GUILayout.Label(GUIContent.none, GUILayout.Width(16), GUILayout.Height(16));

                            Rect iconRect = GUILayoutUtility.GetLastRect();
                            GUI.DrawTexture(iconRect, AssetPreview.GetMiniTypeThumbnail(typeof(AssemblyDefinitionAsset))); //GetIconTextureForAsset(scene))////

                            // Draw path
                            GUILayout.Label(asmPair.Key.FullName, GUILayout.MaxWidth(Screen.width - 50));
                        }
                        GUILayout.EndHorizontal();

                        // Check for expanded
                        if (expandedAssemblies[asmPair.Key] == true)
                        {
                            foreach(Type type in IncludedAssemblies[asmPair.Key])
                            {
                                GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                                {
                                    GUILayout.Space(40);

                                    // Draw icon
                                    GUILayout.Label(GUIContent.none, GUILayout.Width(16), GUILayout.Height(16));

                                    Rect iconRect = GUILayoutUtility.GetLastRect();
                                    GUI.DrawTexture(iconRect, AssetPreview.GetMiniTypeThumbnail(typeof(MonoScript))); //GetIconTextureForAsset(scene))////

                                    // Draw path
                                    GUILayout.Label(type.FullName, GUILayout.MaxWidth(Screen.width - 50));
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
                    {
                        EditorGUILayout.HelpBox("There are no scripts included in this DLC!", MessageType.Info);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }


        private static Texture2D GetIconTextureForAsset(DLCAsset asset)
        {
            // Get the type
            Type type = asset.AssetMainType;

            // Check for scene
            if (asset is DLCSceneAsset)
                type = typeof(SceneAsset);

            // Try to get icon
            return AssetPreview.GetMiniTypeThumbnail(type);
        }
    }
}
