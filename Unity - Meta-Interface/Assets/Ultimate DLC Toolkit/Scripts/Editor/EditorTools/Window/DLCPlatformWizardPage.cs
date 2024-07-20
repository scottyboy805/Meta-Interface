using DLCToolkit.Profile;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    internal class DLCPlatformWizardPage : DLCWizardPage
    {
        // Private
        private static readonly GUIContent enabledLabel = new GUIContent("Enabled", "Should this platform be enabled for the DLC");

        private bool tableStyle = true;
        private GUIContent[] platformIcons = null;

        // Properties
        public override string PageName => "Platforms";

        // Constructor
        public DLCPlatformWizardPage(DLCProfile profile)
            : base(profile)
        {
            platformIcons = new GUIContent[profile.Platforms.Length];

            for(int i = 0; i < platformIcons.Length; i++)
            {
                platformIcons[i] = GUIIcons.GetIconForPlatform(profile.Platforms[i].Platform);
            }
        }

        // Methods
        public override void OnGUI()
        {
            EditorGUILayout.HelpBox("Select the platforms that the DLC will be built for. Can be changed later", MessageType.Info);

            // Draw all platforms
            for(int i = 0; i < Profile.Platforms.Length; i++)
            {
                OnPlatformGUI(Profile.Platforms[i], i);
            }
        }

        private void OnPlatformGUI(DLCPlatformProfile platform, int i)
        {
            // Platform group
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                // Platform icon
                GUILayout.Label(platformIcons[i], GUILayout.Width(30), GUILayout.Height(16));
                GUILayout.FlexibleSpace();

                // Platform name
                GUILayout.Label(DLCPlatformProfile.GetFriendlyPlatformName(platform.Platform));
                GUILayout.FlexibleSpace();

                // Enabled
                GUILayout.Label(enabledLabel);
                bool result = EditorGUILayout.Toggle(GUIContent.none, platform.Enabled, GUILayout.Width(16));

                // Check for changed
                if(result != platform.Enabled)
                    platform.Enabled = result;

                // Platform warning
                if (DLCPlatformProfile.IsDLCBuildTargetAvailable(platform.Platform) == true)
                {
                    GUILayout.Space(30);
                }
                else
                {
                    GUIContent content = EditorGUIUtility.IconContent("console.warnicon.sml");
                    content.tooltip = "Build support is not currently installed for this platform! You can still enable DLC for this platform, but you will not be able to build until you install the platform build tools from the Unity Hub (" 
                        + "Player build tools: " + platform.Platform + ").";
                    GUILayout.Label(content, GUILayout.Width(24));
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
