using UnityEngine;
using UnityEditor;
using DLCToolkit.Profile;
using System.IO;

namespace DLCToolkit.EditorTools
{
    internal sealed class DLCCreateWizard : EditorWindow
    {
        // Internal
        internal static string specifiedCreateFolder = null;

        // Private
        private int selectedPage = 0;
        private DLCProfile profile = null;
        private DLCMetadataWizardPage metadataPage = null;
        private DLCWizardPage[] pages = null;        

        // Methods
        private void OnEnable()
        {
            profile = CreateInstance<DLCProfile>();
            pages = new DLCWizardPage[]
            {                
                metadataPage = new DLCMetadataWizardPage(profile, specifiedCreateFolder),
                new DLCPlatformWizardPage(profile),
                new DLCOptionsWizardPage(profile),
            };

            specifiedCreateFolder = null;
        }

        private void OnGUI()
        {
            // Draw header
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Create New DLC", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(40);
                GUILayout.BeginVertical();
                {
                    // Draw selected page
                    pages[selectedPage].OnGUI();
                }
                GUILayout.EndVertical();
                GUILayout.Space(40);
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            // Draw navigation
            OnNavigationGUI();
            GUILayout.Space(20);
        }

        private void OnNavigationGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                for (int i = 0; i < pages.Length; i++)
                {
                    // Select correct style
                    GUIStyle style = GUIStyles.BreadcrumbMiddleStyle;

                    // Check for start or end
                    if (i == 0)
                        style = GUIStyles.BreadcrumbStartStyle;

                    // Draw the node
                    OnNavigationNodeGUI(style, i, pages[i].PageName);
                }

                // Perform validation
                InvalidReason reason = metadataPage.ValidateDLCProfile();

                // Draw the create node
                EditorGUI.BeginDisabledGroup(reason != InvalidReason.None && reason != InvalidReason.PathAlreadyExists);
                if(OnNavigationNodeGUI(GUIStyles.BreadcrumbEndStyle, 3, "Create") == true)
                {
                    // Show create dialog
                    if(EditorUtility.DisplayDialog("Create New DLC", "Do you want to create the new DLC content at: " + metadataPage.CreatePath, "Confirm", "Cancel") == true)
                    {
                        // Create the DLC
                        CreateDLCProfile();
                    }
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        private bool OnNavigationNodeGUI(GUIStyle style, int index, string name)
        {
            // Get required size
            Vector2 requiredSize = style.CalcSize(new GUIContent(name));

            // Get selected
            bool selected = index == selectedPage;
            bool pressed = false;

            // Draw toggle
            bool selectedResult = GUILayout.Toggle(selected, "", style, GUILayout.Width(requiredSize.x), GUILayout.Height(26));

            // Change the page
            if (selectedResult == true && selected != selectedResult)
            {
                // Change page
                if(index < pages.Length)
                    selectedPage = index;

                pressed = true;
            }

            // Get the last rectangle
            Rect area = GUILayoutUtility.GetLastRect();

            area.x += (index > 0) ? 8 : 0;
            area.width -= (index > 0) ? 8 : 0;

            if (style.name == "legacy")
            {
                area.y += 5;
            }

            // Display title
            GUI.Label(area, name);
            return pressed;
        }

        private void CreateDLCProfile()
        {
            // Get validation
            InvalidReason reason = metadataPage.ValidateDLCProfile();

            // Validate once more
            if (reason != InvalidReason.None && reason != InvalidReason.PathAlreadyExists)
                Debug.LogError("Cannot create new DLC. One or more required parameters are missing or invalid");

            // Get the asset path to create
            string createAssetPathRelative = metadataPage.CreatePath + "/" + profile.DLCName + ".asset";

            // Check for path already exists
            if (reason == InvalidReason.PathAlreadyExists)
            {
                // Check for dlc already exists
                if(File.Exists(createAssetPathRelative) == true)
                {
                    EditorUtility.DisplayDialog("Could not create DLC!", "A DLC profile with the same name already exists in the specified folder: \n" + createAssetPathRelative + "\n\nPlease choose a different path or name for the DLC to continue", "Confirm");
                    return;
                }

                // Show a warning dialog
                if (EditorUtility.DisplayDialog("Path already exists!", "Are you sure you want to create a new DLC inside an existing folder? It is recommended to create the DLC in a dedicated folder", "Continue", "Cancel") == false)
                    return;
            }


            // Update platform unique keys
            string uniqueKey = profile.DLCName.Replace(" ", "")
                .ToLower();

            // Remove disabled platforms
            profile.RemoveDisabledPlatforms();

            // Update platform unique key
            foreach (DLCPlatformProfile platform in profile.Platforms)
                platform.DlcUniqueKey = uniqueKey;


            // Create directory
            if(reason != InvalidReason.PathAlreadyExists && Directory.Exists(metadataPage.CreatePath) == false)
                Directory.CreateDirectory(metadataPage.CreatePath);

            // Import
            AssetDatabase.Refresh();

            // Save the asset
            AssetDatabase.CreateAsset(profile, createAssetPathRelative);

            // Update content folder - must be performed after asset is created
            profile.UpdateDLCContentPath();
            AssetDatabase.SaveAssetIfDirty(profile);

            // Select the asset
            Selection.SetActiveObjectWithContext(profile, profile);

            // Ping folder
            EditorGUIUtility.PingObject(profile);

            // Close the window
            Close();
        }
    }
}
