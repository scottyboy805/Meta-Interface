using DLCToolkit.BuildTools;
using DLCToolkit.Profile;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    [InitializeOnLoad]
    internal static class EditorMenu
    {
        // Public
        public const string setScriptReleaseMode = "Tools/DLC Toolkit/Build Config/Compilation/Release";
        public const string setScriptDebugMode = "Tools/DLC Toolkit/Build Config/Compilation/Debug";
        public const string setForceRebuildEnabled = "Tools/DLC Toolkit/Build Config/Force Rebuild/Enabled";
        public const string setForceRebuildDisabled = "Tools/DLC Toolkit/Build Config/Force Rebuild/Disabled";

        // Constructor
        static EditorMenu()
        {
            // Update and load config
            DLCConfig config = DLCBuildPipeline.UpdateDLCConfigFromProject();

            // Set menu state
            Menu.SetChecked(setScriptReleaseMode, config.scriptingDebug == false);
            Menu.SetChecked(setScriptDebugMode, config.scriptingDebug == true);
            Menu.SetChecked(setForceRebuildEnabled, config.forceRebuild == true);
            Menu.SetChecked(setForceRebuildDisabled, config.forceRebuild == false);
        }

        // Methods
        [MenuItem("Assets/Create/Ultimate DLC Toolkit/Create DLC", false, 1)]
        public static void CreateNewDLCInFolder()
        {
            string path = "Assets";

            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }

            // Open window with path specified
            DLCCreateWizard.specifiedCreateFolder = path;
            EditorWindow.GetWindowWithRect<DLCCreateWizard>(new Rect(-1, -1, 500, 380), false, "Create DLC");
        }



        [MenuItem("Tools/DLC Toolkit/Create DLC", priority = 100)]
        public static void CreateNewDLC()
        {
            EditorWindow.GetWindowWithRect<DLCCreateWizard>(new Rect(-1, -1, 500, 380), false, "Create DLC");
        }

        [MenuItem("Tools/DLC Toolkit/Browse DLC", priority = 120)]
        public static void BrowseDLC()
        {
            EditorWindow.GetWindowWithRect<DLCBrowser>(new Rect(-1, -1, 500, 380), false, "Browse DLC");
        }


        [MenuItem("Tools/DLC Toolkit/Build DLC/All Platforms #&B", priority = 160)]
        public static void BuildDLCForAllPlatforms()
        {
            // Build all DLC
            DLCBuildPipeline.BuildAllDLCContent();
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/All Platforms %#&B", priority = 161)]
        public static void BuildSelectedDLCForAllPlatforms()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build all DLC
            DLCBuildPipeline.BuildDLCContent(profiles);
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Desktop #&D", priority = 180)]
        public static void BuildDLCForDesktopPlatform()
        {
            // Build Desktop DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.StandaloneWindows64,
                BuildTarget.StandaloneOSX,
                BuildTarget.StandaloneLinux64,
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Desktop %#&D", priority = 181)]
        public static void BuildSelectedDLCForDesktopPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Desktop DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.StandaloneWindows64,
                BuildTarget.StandaloneOSX,
                BuildTarget.StandaloneLinux64,
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Console #&C", priority = 180)]
        public static void BuildDLCForConsolePlatform()
        {
            // Build Desktop DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.PS4,
                BuildTarget.PS5,
                BuildTarget.XboxOne,
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Console %#&C", priority = 181)]
        public static void BuildSelectedDLCForConsolePlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Desktop DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.PS4,
                BuildTarget.PS5,
                BuildTarget.XboxOne,
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Mobile #&M", priority = 180)]
        public static void BuildDLCForMobilePlatform()
        {
            // Build Mobile DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.iOS,
                BuildTarget.Android,
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Mobile %#&M", priority = 181)]
        public static void BuildSelectedDLCForMobilePlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Mobile DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.iOS,
                BuildTarget.Android,
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Windows", priority = 200)]
        public static void BuildDLCForWindowsPlatform()
        {
            // Build Windows DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.StandaloneWindows64
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Windows", priority = 201)]
        public static void BuildSelectedDLCForWindowsPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Windows DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.StandaloneWindows64
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/OSX", priority = 200)]
        public static void BuildDLCForOSXPlatform()
        {
            // Build OSX DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.StandaloneOSX
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/OSX", priority = 201)]
        public static void BuildSelectedDLCForOSXPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build OSX DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.StandaloneOSX
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Linux", priority = 200)]
        public static void BuildDLCForLinuxPlatform()
        {
            // Build Linux DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.StandaloneLinux64
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Linux", priority = 201)]
        public static void BuildSelectedDLCForLinuxPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Linux DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.StandaloneLinux64
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Web GL", priority = 200)]
        public static void BuildDLCForWebGLPlatform()
        {
            // Build WebGL DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.WebGL
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Web GL", priority = 201)]
        public static void BuildSelectedDLCForWebGLPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build WebGL DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.WebGL
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/IOS", priority = 200)]
        public static void BuildDLCForIOSPlatform()
        {
            // Build IOS DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.iOS
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/IOS", priority = 201)]
        public static void BuildSelectedDLCForIOSPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build IOS DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.iOS
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/Android", priority = 200)]
        public static void BuildDLCForAndroidPlatform()
        {
            // Build Android DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.Android
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/Android", priority = 201)]
        public static void BuildSelectedDLCForAndroidPlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Android DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.Android
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/PS4", priority = 200)]
        public static void BuildDLCForPS4Platform()
        {
            // Build PS4 DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.PS4
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/PS4", priority = 201)]
        public static void BuildSelectedDLCForPS4Platform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build PS4 DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.PS4
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/PS5", priority = 200)]
        public static void BuildDLCForPS5Platform()
        {
            // Build PS5 DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.PS5
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/PS5", priority = 201)]
        public static void BuildSelectedDLCForPS5Platform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }


            // Build PS5 DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.PS5
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build DLC/XBox One", priority = 200)]
        public static void BuildDLCForXBoxOnePlatform()
        {
            // Build Xbox One DLC
            DLCBuildPipeline.BuildAllDLCContent(null, new BuildTarget[]
            {
                BuildTarget.XboxOne
            });
        }

        [MenuItem("Tools/DLC Toolkit/Build Selected DLC/XBox One", priority = 201)]
        public static void BuildSelectedDLCForXBoxOnePlatform()
        {
            // Get selected profiles
            DLCProfile[] profiles = Selection.GetFiltered<DLCProfile>(SelectionMode.Unfiltered);

            // Check for any
            if (profiles.Length == 0)
            {
                Debug.LogError("No selected DLC profiles to build!");
                return;
            }

            // Build Xbox One DLC
            DLCBuildPipeline.BuildDLCContent(profiles, null, new BuildTarget[]
            {
                BuildTarget.XboxOne
            });
        }

#if DLCTOOLKIT_DEBUG
        [MenuItem("Tools/DLC Toolkit/Update DLC Config", false, priority = 300)]
#endif
        public static void UpdateDLCConfig()
        {
            // Important to keep config in order when game or DLC is built
            DLCBuildPipeline.UpdateDLCConfigFromProject();
        }

        [MenuItem(setScriptReleaseMode, priority = 302)]
        public static void SetScriptingReleaseMode()
        {
            // Update and load config
            DLCConfig config = DLCBuildPipeline.UpdateDLCConfigFromProject();

            // Update valid
            config.scriptingDebug = false;
            EditorUtility.SetDirty(config);

            // Update menus
            Menu.SetChecked(setScriptReleaseMode, config.scriptingDebug == false);
            Menu.SetChecked(setScriptDebugMode, config.scriptingDebug == true);
        }

        [MenuItem(setScriptDebugMode, priority = 302)]
        public static void SetScriptingDebugMode()
        {
            // Update and load config
            DLCConfig config = DLCBuildPipeline.UpdateDLCConfigFromProject();

            // Update valid
            config.scriptingDebug = true;
            EditorUtility.SetDirty(config);

            // Update menus
            Menu.SetChecked(setScriptReleaseMode, config.scriptingDebug == false);
            Menu.SetChecked(setScriptDebugMode, config.scriptingDebug == true);
        }

        [MenuItem(setForceRebuildEnabled, priority = 302)]
        public static void SetForceRebuildEnabledMode()
        {
            // Update and load config
            DLCConfig config = DLCBuildPipeline.UpdateDLCConfigFromProject();

            // Update valid
            config.forceRebuild = true;
            EditorUtility.SetDirty(config);

            // Update menus
            Menu.SetChecked(setForceRebuildEnabled, config.forceRebuild == true);
            Menu.SetChecked(setForceRebuildDisabled, config.forceRebuild == false);
        }

        [MenuItem(setForceRebuildDisabled, priority = 302)]
        public static void SetForceRebuildDisabledMode()
        {
            // Update and load config
            DLCConfig config = DLCBuildPipeline.UpdateDLCConfigFromProject();

            // Update valid
            config.forceRebuild = false;
            EditorUtility.SetDirty(config);

            // Update menus
            Menu.SetChecked(setForceRebuildEnabled, config.forceRebuild == true);
            Menu.SetChecked(setForceRebuildDisabled, config.forceRebuild == false);
        }

        [MenuItem("Tools/DLC Toolkit/Settings", false, priority = 400)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/DLC Toolkit");
        }
    }
}
