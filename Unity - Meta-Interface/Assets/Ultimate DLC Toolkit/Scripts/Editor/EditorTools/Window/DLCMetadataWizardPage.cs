using DLCToolkit.Profile;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DLCToolkit.EditorTools
{
    [Flags]
    internal enum InvalidReason
    {
        None = 0,
        EmptyName = 1,
        EmptyVersion = 2,
        InvalidVersion = 4,
        EmptyPath = 8,
        PathAlreadyExists = 16,
        PathNotAssets = 32,
    }

    internal class DLCMetadataWizardPage : DLCWizardPage
    {
        // Private
        private static readonly GUIContent pathLabel = new GUIContent("DLC Path*", "The folder path where the DLC will be created. Required value");
        private static readonly GUIContent nameLabel = new GUIContent("DLC Name*", "The name of the DLC which can be changed at a later time. Required value");
        private static readonly GUIContent versionLabel = new GUIContent("DLC Version*", "The version for the DLC in the forma X.X.X which can be changed at a later time. Required Value");
        private static readonly GUIContent extensionLabel = new GUIContent("DLC Extension", "The file extension for the DLC which can be changed per platform at a later time");
        private static readonly GUIContent descriptionLabel = new GUIContent("DLC Description", "A short description for the DLC content which can be changed at a later time");
        private static readonly GUIContent developerLabel = new GUIContent("DLC Developer", "The name of the developer or studio that created the DLC which can be changed at a later time");
        private static readonly GUIContent publisherLabel = new GUIContent("DLC Publisher", "The name of the publisher that will distribute the DLC which can be changed at a later time");


        private const string defaultCreatePath = "Assets/DLC/";
        private string createPath = defaultCreatePath + "New DLC";
        private bool wasPathEdited = false;
        private bool tableStyle = true;
        private GUIContent error = null;
        private GUIContent warning = null;

        // Properties
        public override string PageName => "Metadata";

        public string CreatePath
        {
            get { return createPath; }
        }

        // Constructor
        public DLCMetadataWizardPage(DLCProfile profile, string specifiedCreateFolder = null)
            : base(profile)
        {
            createPath = string.IsNullOrEmpty(specifiedCreateFolder) == true
                ? defaultCreatePath + Profile.DLCName
                : Path.Combine(specifiedCreateFolder, Profile.DLCName).Replace('\\', '/');

            error = EditorGUIUtility.IconContent("console.erroricon.sml");
            warning = EditorGUIUtility.IconContent("console.warnicon.sml");
        }

        // Methods
        public override void OnGUI()
        {
            EditorGUILayout.HelpBox("Enter the required fields for the new DLC content. Can be changed later", MessageType.Info);

            // Validate profile
            InvalidReason reason = ValidateDLCProfile();

            // DLC path
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(pathLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = EditorGUILayout.TextField(createPath);

                // Show invalid
                if ((reason & InvalidReason.EmptyPath) != 0)
                {
                    error.tooltip = "Path cannot be empty";
                    GUILayout.Label(error, GUILayout.Width(24));
                }
                else if ((reason & InvalidReason.PathNotAssets) != 0)
                {
                    error.tooltip = "Path must specify a location in the current project";
                    GUILayout.Label(error, GUILayout.Width(24));
                }
                else if ((reason & InvalidReason.PathAlreadyExists) != 0)
                {
                    warning.tooltip = "Path already exists";
                    GUILayout.Label(warning, GUILayout.Width(24));
                }

                // Check for changed
                if (result != createPath)
                {
                    createPath = result;
                    wasPathEdited = true;
                }
            }
            GUILayout.EndHorizontal();

            // DLC name
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(nameLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = EditorGUILayout.TextField(Profile.DLCName);

                // Show invalid
                if ((reason & InvalidReason.EmptyName) != 0)
                {
                    error.tooltip = "Name cannot be empty";
                    GUILayout.Label(error, GUILayout.Width(24));
                }

                // Check for changed
                if (result != Profile.DLCName)
                {
                    Profile.DLCName = result;

                    if (wasPathEdited == false)
                        createPath = defaultCreatePath + result;
                }
            }
            GUILayout.EndHorizontal();

            // DLC version
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(versionLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = EditorGUILayout.TextField(Profile.DLCVersionString);

                // Show invalid
                if ((reason & InvalidReason.EmptyVersion) != 0)
                {
                    error.tooltip = "Version cannot be empty";
                    GUILayout.Label(error, GUILayout.Width(24));
                }
                else if ((reason & InvalidReason.InvalidVersion) != 0)
                {
                    error.tooltip = "Version is invalid. Use format X.X.X";
                    GUILayout.Label(error, GUILayout.Width(24));
                }

                // Check for changed
                if (result != Profile.DLCVersionString)
                    Profile.DLCVersionString = result;
            }
            GUILayout.EndHorizontal();

            // DLC extension
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(extensionLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = EditorGUILayout.TextField(Profile.Platforms[0].DLCExtension);

                // Check for changed
                if (result != Profile.Platforms[0].DLCExtension)
                {
                    foreach (DLCPlatformProfile platform in Profile.Platforms)
                        platform.DLCExtension = result;
                }
            }
            GUILayout.EndHorizontal();

            // DLC description
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(descriptionLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = GUILayout.TextArea(Profile.Description, GUILayout.Height(40));

                // Check for changed
                if (result != Profile.Description)
                    Profile.Description = result;
            }
            GUILayout.EndHorizontal();

            // DLC developer
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(developerLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = EditorGUILayout.TextField(Profile.Developer);

                // Check for changed
                if (result != Profile.Developer)
                    Profile.Developer = result;
            }
            GUILayout.EndHorizontal();

            // DLC publisher
            GUILayout.BeginHorizontal(GUIStyles.GetActiveTableContentStyle(ref tableStyle));
            {
                GUILayout.Label(publisherLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
                string result = EditorGUILayout.TextField(Profile.Publisher);

                // Check for changed
                if (result != Profile.Publisher)
                    Profile.Publisher = result;
            }
            GUILayout.EndHorizontal();
        }

        public InvalidReason ValidateDLCProfile()
        {
            InvalidReason flags = 0;

            // Check for path
            if (string.IsNullOrEmpty(CreatePath) == true)
                flags |= InvalidReason.EmptyPath;

            // Check for path exists
            if (Directory.Exists(CreatePath) == true)
                flags |= InvalidReason.PathAlreadyExists;

            // Check for assets
            if ((flags & InvalidReason.EmptyPath) == 0)
            {
                try
                {
                    string fullPath = Path.GetFullPath(CreatePath);
                    string relativePath = FileUtil.GetProjectRelativePath(fullPath.Replace('\\', '/'));

                    if (string.IsNullOrEmpty(relativePath) == true || relativePath.StartsWith("Assets/") == false)
                        flags |= InvalidReason.PathNotAssets;
                }
                catch
                {
                    flags |= InvalidReason.PathNotAssets;
                }
            }

            // Check for name
            if (string.IsNullOrEmpty(Profile.DLCName) == true)
                flags |= InvalidReason.EmptyName;

            // Check for version
            if (string.IsNullOrEmpty(Profile.DLCVersionString) == true)
                flags |= InvalidReason.EmptyVersion;

            // Check for invalid version
            if (Profile.DLCVersion == null)
                flags |= InvalidReason.InvalidVersion;

            return flags;
        }
    }
}
