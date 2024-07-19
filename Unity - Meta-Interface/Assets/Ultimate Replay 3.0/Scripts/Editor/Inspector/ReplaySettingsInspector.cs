using System;
using System.Collections.Generic;
using System.Reflection;
using UltimateReplay.Lifecycle;
using UltimateReplay.StatePreparation;
using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    [CustomEditor(typeof(ReplaySettings))]
    public class ReplaySettingsInspector : Editor
    {
        // Private
        private static readonly string[] options = { "General", "Prefabs", "State Preparation" };
        private int selection = 0;

        private Color light = new Color(0.9f, 0.9f, 0.9f);
        private Color dark = new Color(0.7f, 0.7f, 0.7f);

        private SerializedProperty sceneDiscovery = null;

        private SerializedProperty recordGroup = null;
        private SerializedProperty recordFps = null;
        private SerializedProperty recordUpdate = null;

        private SerializedProperty playbackGroup = null;
        private SerializedProperty playbackEndBehaviour = null;
        private SerializedProperty playbackFps = null;
        private SerializedProperty playbackUpdate = null;

        private Stack<SerializableType> removeTypes = new Stack<SerializableType>();

        // Methods
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            // Display toolbar
            selection = GUILayout.Toolbar(selection, options);
            GUILayout.Space(10);

            switch(selection)
            {
                case 0: OnGeneralGUI(); break;
                case 1: OnPrefabsGUI(); break;
                case 2: OnStatePreparationGUI(); break;
            }

            EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();
        }

        private void FindProperties()
        {
            sceneDiscovery = serializedObject.FindProperty(nameof(ReplaySettings.sceneDiscovery));

            // Find record properties
            if (recordGroup == null)
            {
                recordGroup = serializedObject.FindProperty(nameof(ReplaySettings.recordOptions));
                recordFps = recordGroup.FindPropertyRelative(nameof(ReplayRecordOptions.recordFPS));
                recordUpdate = recordGroup.FindPropertyRelative(nameof(ReplayRecordOptions.recordUpdateMode));
            }

            // Find playback properties
            if(playbackGroup == null)
            {
                playbackGroup = serializedObject.FindProperty(nameof(ReplaySettings.playbackOptions));
                playbackEndBehaviour = playbackGroup.FindPropertyRelative(nameof(ReplayPlaybackOptions.playbackEndBehaviour));
                playbackFps = playbackGroup.FindPropertyRelative(nameof(ReplayPlaybackOptions.playbackFPS));
                playbackUpdate = playbackGroup.FindPropertyRelative(nameof(ReplayPlaybackOptions.playbackUpdateMode));
            }
        }

        private void OnGeneralGUI()
        {
            ReplaySettings settings = (ReplaySettings)target;

            // Update properties
            FindProperties();

            // Draw property
            EditorGUILayout.PropertyField(sceneDiscovery);
            EditorGUILayout.Separator();
            GUILayout.Space(10);

            // Draw record
            GUILayout.Label("Replay Record Options", EditorStyles.largeLabel);

            // Record properties
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Display record options
                EditorGUILayout.PropertyField(recordFps);
                EditorGUILayout.PropertyField(recordUpdate);
            }
            GUILayout.EndVertical();
            GUILayout.Space(20);

            // Draw playback
            GUILayout.Label("Replay Playback Options", EditorStyles.largeLabel);

            // Playback properties
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Playback unlimited
                GUILayout.BeginHorizontal();
                {
                    // Label
                    GUILayout.Label("Playback FPS Unlimited", GUILayout.Width(EditorGUIUtility.labelWidth));

                    // Toggle
                    bool result = EditorGUILayout.Toggle(settings.PlaybackOptions.IsPlaybackFPSUnlimited);

                    // Check for changed
                    if(result != settings.playbackOptions.IsPlaybackFPSUnlimited)
                    {
                        // Update property
                        playbackFps.floatValue = result == true ? -1 : 60;                 
                    }
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(settings.playbackOptions.IsPlaybackFPSUnlimited);
                EditorGUILayout.PropertyField(playbackFps);
                EditorGUI.EndDisabledGroup();

                // Display playback options
                EditorGUILayout.PropertyField(playbackEndBehaviour);
                EditorGUILayout.PropertyField(playbackUpdate);
            }
            GUILayout.EndVertical();
            GUILayout.Space(20);

            EditorGUILayout.HelpBox("These settings will be used by default unless custom settings are provided when calling the Replay Managaer API", MessageType.Info);
        }

        private void OnPrefabsGUI()
        {
            ReplaySettings settings = (ReplaySettings)target;

            // Draw prefabs
            GUILayout.Label("Replay Prefab Providers", EditorStyles.largeLabel);

            // Draw all
            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Check for none
                if(settings.PrefabProviders.Count == 0)
                {
                    GUILayout.Label("No prefabs have been added yet. Select add\noption or drag & drop prefab providers here!", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    for(int i = 0; i < settings.PrefabProviders.Count; i++)
                    {
                        // Staggered item colours
                        GUI.backgroundColor = (i % 2 == 0) ? light : dark;

                        DrawPrefabItem(settings, settings.PrefabProviders[i]);

                        // Divider
                        if (i < settings.PrefabProviders.Count - 1)
                        {
                            EditorGUILayout.Separator();
                            GUILayout.Space(-10);
                        }
                    }
                }
            }
            GUILayout.EndVertical();

            // Buttons
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                // Add default
                if(GUILayout.Button("Add Prefab") == true)
                {
                    // Add default slot
                    settings.AddPrefabProvider(CreateInstance<ReplayObjectDefaultLifecycleProvider>());
                }

                // Add resources
                if(GUILayout.Button("Add Resources") == true)
                {
                    // Add resources slot
                    settings.AddPrefabProvider(CreateInstance<ReplayObjectResourcesLifecycleProvider>());
                }

                // Add custom
                if(GUILayout.Button("Add Custom") == true)
                {
                    settings.AddPrefabProvider(CreateInstance<ReplayObjectCustomLifecycleProvider>());
                }
            }
            GUILayout.EndHorizontal();
        }

        private void OnStatePreparationGUI()
        {
            ReplaySettings settings = (ReplaySettings)target;

            GUILayout.Label("Default Replay Preparer Configuration", EditorStyles.largeLabel);
            GUILayout.Space(10);

            // Useful hint
            EditorGUILayout.HelpBox("This configuration only applies when using the included 'Default Replay Preparer' for replay state preparation", MessageType.Info);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // Title
                GUILayout.Label("Ignore Component Types", EditorStyles.largeLabel);
                DrawUnderLine();


                // Display all serialized types
                foreach (SerializableType type in settings.DefaultReplayPreparer.SkipTypes)
                {
                    // Get the system type
                    string typeName = type.SystemType.FullName;

                    GUILayout.BeginHorizontal();
                    {
                        // Display the name info
                        GUILayout.Label(typeName);

                        // Clear button
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("X", GUILayout.Width(20)) == true)
                        {
                            removeTypes.Push(type);

                            // Mark as dirty
                            EditorUtility.SetDirty(target);
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                // Clear dead types
                while (removeTypes.Count > 0)
                    settings.DefaultReplayPreparer.SkipTypes.Remove(removeTypes.Pop());
            }
            GUILayout.EndVertical();

            // Add new ignore type
            GUILayout.Space(-4);
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Type", EditorStyles.toolbarButton) == true)
                {
                    // Show the menu
                    ShowAddTypeContextMenu();
                }
            }
            GUILayout.EndHorizontal();


            // Component preparer settings
            GUILayout.Space(10);
            GUILayout.Label("Component Processors", EditorStyles.largeLabel);

            // Display all
            foreach (DefaultReplayPreparer.ComponentPreparerSettings preparerSetting in settings.DefaultReplayPreparer.PreparerSettings)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Label
                    GUILayout.Label(preparerSetting.componentPreparerType.SystemType.FullName, EditorStyles.largeLabel);
                    DrawUnderLine();

                    // Enabled property
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Enabled", GUILayout.Width(EditorGUIUtility.labelWidth));
                        bool result = EditorGUILayout.Toggle(preparerSetting.enabled);

                        if (result != preparerSetting.enabled)
                        {
                            preparerSetting.enabled = result;

                            // Mark as dirty
                            EditorUtility.SetDirty(target);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        private void ShowAddTypeContextMenu()
        {
            GenericMenu menu = new GenericMenu();

            // Get all component types
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in asm.GetTypes())
                {
                    // Check for component
                    if (typeof(Component).IsAssignableFrom(type) == true)
                    {
                        // Check for already added
                        if (((ReplaySettings)target).DefaultReplayPreparer.HasSkipType(type) == false)
                        {
                            if (string.IsNullOrEmpty(type.Namespace) == true)
                            {
                                menu.AddItem(new GUIContent("namespace::global/" + type.Name), false, OnAddTypeContextMenuClicked, type);
                            }
                            else
                            {
                                menu.AddItem(new GUIContent(string.Concat(type.Namespace, "/", type.Name)), false, OnAddTypeContextMenuClicked, type);
                            }
                        }
                    }
                }
            }

            menu.ShowAsContext();
        }

        private void OnAddTypeContextMenuClicked(object item)
        {
            // Add the new type
            ((ReplaySettings)target).DefaultReplayPreparer.SkipTypes.Add((Type)item);

            // Mark as dirty
            EditorUtility.SetDirty(target);
        }

        private void DrawUnderLine()
        {
            Rect last = GUILayoutUtility.GetLastRect();
            last.y += last.height;
            last.height = 1;

            EditorGUI.DrawRect(last, Color.gray);
        }

        private void DrawPrefabItem(ReplaySettings settings, ReplayObjectLifecycleProvider provider)
        {
            // Start layout
            GUILayout.BeginVertical(GUI.skin.box);
            {
                // Check for default prefab
                if (provider is ReplayObjectDefaultLifecycleProvider)
                {
                    ReplayObjectDefaultLifecycleProvider defaultProvider = (ReplayObjectDefaultLifecycleProvider)provider;

                    // Prefab field
                    GUILayout.BeginHorizontal();
                    {
                        // Label
                        GUILayout.Label("Prefab:", GUILayout.Width(EditorGUIUtility.labelWidth));

                        // Field
                        ReplayObject result = EditorGUILayout.ObjectField(defaultProvider.replayPrefab, typeof(ReplayObject), false) as ReplayObject;

                        // Check for changed
                        if (result != defaultProvider.replayPrefab)
                        {
                            defaultProvider.replayPrefab = result;
                            EditorUtility.SetDirty(target);
                        }

                        // Remove button
                        if (GUILayout.Button("X", GUILayout.Width(20)) == true)
                        {
                            settings.RemovePrefabProvider(provider);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Pooling field
                    GUILayout.BeginHorizontal();
                    {
                        // Label
                        GUILayout.Label("Allow Pooling:", GUILayout.Width(EditorGUIUtility.labelWidth));

                        // Toggle
                        bool result = EditorGUILayout.Toggle(defaultProvider.allowPooling);

                        // Check for changed
                        if (result != defaultProvider.allowPooling)
                        {
                            defaultProvider.allowPooling = result;
                            EditorUtility.SetDirty(target);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                // Check for resources prefab
                else if (provider is ReplayObjectResourcesLifecycleProvider)
                {
                    ReplayObjectResourcesLifecycleProvider resourceProvider = (ReplayObjectResourcesLifecycleProvider)provider;

                    // Prefab field
                    GUILayout.BeginHorizontal();
                    {
                        // Label
                        GUILayout.Label("Resources Path:", GUILayout.Width(EditorGUIUtility.labelWidth));

                        // Field
                        string result = EditorGUILayout.TextField(resourceProvider.resourcesPath);

                        // Check for changed
                        if (result != resourceProvider.resourcesPath)
                        {
                            resourceProvider.resourcesPath = result;
                            EditorUtility.SetDirty(target);
                        }

                        // Remove button
                        if (GUILayout.Button("X", GUILayout.Width(20)) == true)
                        {
                            settings.RemovePrefabProvider(provider);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Pooling field
                    GUILayout.BeginHorizontal();
                    {
                        // Label
                        GUILayout.Label("Allow Pooling:", GUILayout.Width(EditorGUIUtility.labelWidth));

                        // Toggle
                        bool result = EditorGUILayout.Toggle(resourceProvider.allowPooling);

                        // Check for changed
                        if (result != resourceProvider.allowPooling)
                        {
                            resourceProvider.allowPooling = result;
                            EditorUtility.SetDirty(target);
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Startup load field
                    GUILayout.BeginHorizontal();
                    {
                        // Label
                        GUILayout.Label("Async Load On Startup:", GUILayout.Width(EditorGUIUtility.labelWidth));

                        // Toggle
                        bool result = EditorGUILayout.Toggle(resourceProvider.asyncLoadOnStartup);

                        // Check for changed
                        if(result != resourceProvider.asyncLoadOnStartup)
                        {
                            resourceProvider.asyncLoadOnStartup = result;
                            EditorUtility.SetDirty(target);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                // Must be custom
                else if(provider is ReplayObjectCustomLifecycleProvider)
                {
                    ReplayObjectCustomLifecycleProvider custom = (ReplayObjectCustomLifecycleProvider)provider;

                    // Prefab field
                    GUILayout.BeginHorizontal();
                    {
                        // Label
                        GUILayout.Label("Lifecycle Provider:", GUILayout.Width(EditorGUIUtility.labelWidth));

                        // Field
                        ReplayObjectLifecycleProvider result = EditorGUILayout.ObjectField(custom.customProvider, typeof(ReplayObjectLifecycleProvider), false) as ReplayObjectLifecycleProvider;

                        // Check for changed
                        if (result != custom.customProvider)
                        {
                            custom.customProvider = result;
                            EditorUtility.SetDirty(target);
                        }

                        // Remove button
                        if (GUILayout.Button("X", GUILayout.Width(20)) == true)
                        {
                            settings.RemovePrefabProvider(provider);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }
    }
}
