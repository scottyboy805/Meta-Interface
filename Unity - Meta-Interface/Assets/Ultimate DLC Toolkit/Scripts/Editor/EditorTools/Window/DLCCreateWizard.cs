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

                // Draw the create node
                EditorGUI.BeginDisabledGroup(metadataPage.ValidateDLCProfile() != InvalidReason.None);
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
            // Validate once more
            if (metadataPage.ValidateDLCProfile() != InvalidReason.None)
                Debug.LogError("Cannot create new DLC. One or more required parameters are missing or invalid");


            // Update platform unique keys
            string uniqueKey = profile.DLCName.Replace(" ", "")
                .ToLower();

            // Remove disabled platforms
            profile.RemoveDisabledPlatforms();

            // Update platform unique key
            foreach (DLCPlatformProfile platform in profile.Platforms)
                platform.DlcUniqueKey = uniqueKey;


            // Create directory
            if(Directory.Exists(metadataPage.CreatePath) == false)
                Directory.CreateDirectory(metadataPage.CreatePath);

            // Import
            AssetDatabase.Refresh();

            // Save the asset
            AssetDatabase.CreateAsset(profile, metadataPage.CreatePath + "/" + profile.DLCName + ".asset");

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
