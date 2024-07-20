using DLCToolkit.BuildTools;
using UnityEditor;

namespace DLCToolkit.EditorTools
{    
    internal static class DLCEditorSettings
    {
        // Methods
        [SettingsProvider]
        public static SettingsProvider CreateDLCEditorSettingsProvider()
        {
            // Load the config
            DLCConfig config = DLC.Config;

            // Create settings
            return new SettingsProvider("Project/DLC Toolkit", SettingsScope.Project)
            {
                label = "DLC Toolkit",
                guiHandler = (searchContext) =>
                {
                    // Check for change
                    EditorGUI.BeginChangeCheck();

                    // Display UI
                    config.runtimeLogLevel = (DLCLogLevel)EditorGUILayout.EnumPopup("Runtime Log Level",  config.runtimeLogLevel);
                    config.buildLogLevel = (DLCLogLevel)EditorGUILayout.EnumPopup("Build Log Level", config.buildLogLevel);
                    config.clearConsoleOnBuild = EditorGUILayout.Toggle("Clear Console On Build", config.clearConsoleOnBuild);
                    

                    // End change
                    if(EditorGUI.EndChangeCheck() == true)
                    {
                        // Apply settings here
                        EditorUtility.SetDirty(config);
                    }
                },
            };
        }
    }
}
