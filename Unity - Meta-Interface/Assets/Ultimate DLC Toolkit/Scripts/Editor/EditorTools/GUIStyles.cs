using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    internal static class GUIStyles
    {
        // Private
        private static GUIStyle titleLabelStyle = null;
        private static GUIStyle iconStyle = null;
        private static GUIStyle iconButtonStyle = null;
        private static GUIStyle tableContentLightStyle = null;
        private static GUIStyle tableContentDarkStyle = null;
        private static GUIStyle tableHeaderStyle = null;

        private static GUIStyle breadcrumbStartStyle = null;
        private static GUIStyle breadcrumbMiddleStyle = null;
        private static GUIStyle breadcrumbEndStyle = null;

        // Properties
        public static GUIStyle TitleLabelStyle
        {
            get
            {
                if (titleLabelStyle == null)
                {
                    titleLabelStyle = new GUIStyle(EditorStyles.largeLabel);
                    titleLabelStyle.alignment = TextAnchor.MiddleCenter;
                }

                return titleLabelStyle;
            }
        }

        public static GUIStyle IconStyle
        {
            get
            {
                if (iconStyle == null)
                {
                    iconStyle = new GUIStyle(EditorStyles.label);
                    iconStyle.padding = new RectOffset();
                }
                return iconStyle;
            }
        }

        public static GUIStyle IconButtonStyle
        {
            get
            {
                if (iconButtonStyle == null)
                {
                    iconButtonStyle = new GUIStyle(GUI.skin.button);
                    iconButtonStyle.padding = new RectOffset(2, 3, 1, 1);
                }
                return iconButtonStyle;
            }
        }

        public static GUIStyle TableContentLightStyle
        {
            get
            {
                if (tableContentLightStyle == null)
                {
                    // Get help box color
                    Color helpBoxColor = new Color(0.82f, 0.82f, 0.82f);

                    if (EditorGUIUtility.isProSkin == true)
                        helpBoxColor = new Color(0.22f, 0.22f, 0.22f);

                    tableContentLightStyle = new GUIStyle(GUI.skin.label);
                    tableContentLightStyle.stretchWidth = true;
                    tableContentLightStyle.normal.background = new Texture2D(1, 1);
                    tableContentLightStyle.normal.background.SetPixel(0, 0, helpBoxColor);
                    tableContentLightStyle.normal.background.Apply();
                }
                return tableContentLightStyle;
            }
        }

        public static GUIStyle TableContentDarkStyle
        {
            get
            {
                if (tableContentDarkStyle == null)
                {
                    // Get help box color
                    Color helpBoxColor = new Color(0.79f, 0.79f, 0.79f);

                    if (EditorGUIUtility.isProSkin == true)
                        helpBoxColor = new Color(0.20f, 0.20f, 0.20f);

                    tableContentDarkStyle = new GUIStyle(GUI.skin.label);
                    tableContentDarkStyle.stretchWidth = true;
                    tableContentDarkStyle.normal.background = new Texture2D(1, 1);
                    tableContentDarkStyle.normal.background.SetPixel(0, 0, helpBoxColor);
                    tableContentDarkStyle.normal.background.Apply();
                }
                return tableContentDarkStyle;
            }
        }

        public static GUIStyle TableHeaderStyle
        {
            get
            {
                if(tableHeaderStyle == null)
                {
                    // Get help box color
                    Color headerColor = new Color(0.68f, 0.68f, 0.68f);

                    if (EditorGUIUtility.isProSkin == true)
                        headerColor = new Color(0.15f, 0.15f, 0.15f);

                    tableHeaderStyle = new GUIStyle(GUI.skin.box);
                    tableHeaderStyle.stretchWidth = true;
                    tableHeaderStyle.normal.background = new Texture2D(1, 1);
                    tableHeaderStyle.normal.background.SetPixel(0, 0, headerColor);
                    tableHeaderStyle.normal.background.Apply();
                }
                return tableHeaderStyle;
            }
        }

        public static GUIStyle BreadcrumbStartStyle
        {
            get
            {
                if (breadcrumbStartStyle == null)
                {
                    breadcrumbStartStyle = GUI.skin.FindStyle("GUIEditor.BreadcrumbLeftBackground");
                    bool useLegacy = false;

                    if (breadcrumbStartStyle == null)
                    {
                        breadcrumbStartStyle = "GUIEditor.BreadcrumbLeft";
                        useLegacy = true;
                    }

                    breadcrumbStartStyle = new GUIStyle(breadcrumbStartStyle);
                    breadcrumbStartStyle.fixedHeight = 0;
                    breadcrumbStartStyle.border = new RectOffset(3, 10, 3, 3);
                    breadcrumbStartStyle.padding = new RectOffset(3, 10, 6, 7);

                    if (useLegacy == true)
                        breadcrumbStartStyle.name = "Legacy";
                }
                return breadcrumbStartStyle;
            }
        }

        public static GUIStyle BreadcrumbMiddleStyle
        {
            get
            {
                if (breadcrumbMiddleStyle == null)
                {
                    breadcrumbMiddleStyle = GUI.skin.FindStyle("GUIEditor.BreadcrumbMidBackground");
                    bool useLegacy = false;

                    if (breadcrumbMiddleStyle == null)
                    {
                        breadcrumbMiddleStyle = "GUIEditor.BreadcrumbMid";
                        useLegacy = true;
                    }

                    breadcrumbMiddleStyle = new GUIStyle(breadcrumbMiddleStyle);
                    breadcrumbMiddleStyle.fixedHeight = 0;
                    breadcrumbMiddleStyle.border = new RectOffset(10, 10, 3, 3);
                    breadcrumbMiddleStyle.padding = new RectOffset(10, 8, 6, 7);

                    if (useLegacy == true)
                        breadcrumbMiddleStyle.name = "Legacy";
                }
                return breadcrumbMiddleStyle;
            }
        }

        public static GUIStyle BreadcrumbEndStyle
        {
            get
            {
                if (breadcrumbEndStyle == null)
                {
                    breadcrumbEndStyle = BreadcrumbMiddleStyle;
                }
                return breadcrumbEndStyle;
            }
        }

        public static GUILayoutOption LabelWidth
        {
            get { return GUILayout.Width(EditorGUIUtility.labelWidth); }
        }

        // Methods
        public static GUIStyle GetActiveTableContentStyle(ref bool light)
        {
            // Select style
            GUIStyle style = light == true 
                ? TableContentLightStyle 
                : TableContentDarkStyle;

            // Toggle style
            light = !light;

            return style;
        }
    }
}
