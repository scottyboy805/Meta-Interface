using System.Xml;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    internal static class GUIIcons
    {
        // Private
        private static GUIContent windowsIcon = null;
        private static GUIContent osxIcon = null;
        private static GUIContent linuxIcon = null;

        // Methods
        public static GUIContent GetIconForPlatform(BuildTarget platform)
        {
            switch (platform)
            {
                case BuildTarget.StandaloneWindows64:
                    {
                        if (windowsIcon == null)
                        {
                            windowsIcon = EditorGUIUtility.IconContent("BuildSettings.Standalone.Small");
                            windowsIcon.image = Resources.Load<Texture2D>("Editor/Icon/WindowsPlatform");
                        }
                        return windowsIcon;
                    }
                case BuildTarget.StandaloneOSX:
                    {
                        if(osxIcon == null)
                        {
                            osxIcon = new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Standalone.Small"));
                            osxIcon.image = Resources.Load<Texture2D>("Editor/Icon/OSXPlatform");
                        }
                        return osxIcon;
                    }
                case BuildTarget.StandaloneLinux64:
                    {
                        if(linuxIcon == null)
                        {
                            linuxIcon = new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Standalone.Small"));
                            linuxIcon.image = Resources.Load<Texture2D>("Editor/Icon/LinuxPlatform");
                        }
                        return linuxIcon;
                    }
                case BuildTarget.iOS: return EditorGUIUtility.IconContent("BuildSettings.iPhone.Small");
                case BuildTarget.Android: return EditorGUIUtility.IconContent("BuildSettings.Android.Small");
                case BuildTarget.WebGL: return EditorGUIUtility.IconContent("BuildSettings.WebGL.Small");
                case BuildTarget.PS4: return EditorGUIUtility.IconContent("BuildSettings.PS4");
                case BuildTarget.PS5: return EditorGUIUtility.IconContent("BuildSettings.PS5");

                case BuildTarget.XboxOne: return EditorGUIUtility.IconContent("BuildSettings.XboxOne.Small");
            }

            return GUIContent.none;
        }
    }
}
