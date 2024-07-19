using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateReplay
{
    internal static class EditorMenu
    {
        // Methods
        [MenuItem("Tools/Ultimate Replay 3.0/Settings")]
        public static void ShowSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(ReplaySettings).FullName);

            if(guids.Length > 0)
            {
                // Get the asset path
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                // Load the settings asset
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<ReplaySettings>(path);
            }
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Replay Controls", priority = 100)]
        public static void AddReplayControls()
        {
            // Load the asset
            GameObject go = Resources.Load<GameObject>("UIReplayControls");

            // Check for valid
            if(go != null)
            {
                // Create instance of prefab
                GameObject instance =  PrefabUtility.InstantiatePrefab(go) as GameObject;

                // Record undo
                Undo.RegisterCreatedObjectUndo(instance, "Create Replay Controls");

                // Check for edit mode
                if (Application.isPlaying == false)
                {
                    // Check for event system in scene
                    if (Component.FindObjectOfType<EventSystem>(true) == null)
                    {
                        // Create event system
                        GameObject evt = new GameObject("EventSystem");
                        evt.AddComponent<EventSystem>();
                        evt.AddComponent<StandaloneInputModule>();

                        // Add new event system object
                        Undo.RegisterCreatedObjectUndo(evt, "Create Replay Controls");
                    }
                }
            }
            else
            {
                Debug.LogError("Could not create replay controls. 'UIReplayControls' asset is missing, moved or renamed! you may need to re-install from package manager!");
            }
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Object", priority = -20)]
        public static void AddReplayObject()
        {
            AddReplayObjectToSelection();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Transform", priority = 100)]
        public static void AddReplayTransform()
        {
            AddReplayComponentToSelection<ReplayTransform>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Animator", priority = 100)]
        public static void AddReplayAnimator()
        {
            AddReplayComponentToSelection<ReplayAnimator>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Audio", priority = 100)]
        public static void AddReplayAudio()
        {
            AddReplayComponentToSelection<ReplayAudio>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Rigged/Humanoid Rig (Experimental)", priority = 100)]
        public static void AddReplayRiggedHumanoid()
        {
            AddReplayComponentToSelection<ReplayRiggedHumanoid>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Rigged/Generic Rig", priority = 100)]
        public static void AddReplayRiggedGeneric()
        {
            AddReplayComponentToSelection<ReplayRiggedGeneric>();
        }


        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Parent Change", priority = 120)]
        public static void AddReplayParentChange()
        {
            AddReplayComponentToSelection<ReplayParentChange>();
        }


        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Enabled State", priority = 120)]
        public static void AddReplayEnabledState()
        {
            AddReplayComponentToSelection<ReplayEnabledState>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Component Enabled State", priority = 120)]
        public static void AddReplayComponentEnabledState()
        {
            AddReplayComponentToSelection<ReplayComponentEnabledState>();
        }

        

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Particle System", priority = 140)]
        public static void AddReplayParticleSystem()
        {
            AddReplayComponentToSelection<ReplayParticleSystem>();
        }

        

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Line Renderer", priority = 140)]
        public static void AddReplayLineRenderer()
        {
            AddReplayComponentToSelection<ReplayLineRenderer>();
        }

#if UNITY_2018_2_OR_NEWER
        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Trail Renderer", priority = 140)]
        public static void AddReplayTrailRenderer()
        {
            AddReplayComponentToSelection<ReplayTrailRenderer>();
        }
#endif

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Blend Shape", priority = 140)]
        public static void AddReplayBlendShape()
        {
            AddReplayComponentToSelection<ReplayBlendShape>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Material/Material Change", priority = 140)]
        public static void AddReplayMaterialChange()
        {
            AddReplayComponentToSelection<ReplayMaterialChange>();
        }

        [MenuItem("Tools/Ultimate Replay 3.0/Make Selection Replayable/Replay Material/Material Properties", priority = 140)]
        public static void AddReplayMaterialProperties()
        {
            AddReplayComponentToSelection<ReplayMaterial>();
        }

        //[MenuItem("Tools/Ultimate Replay 3.0/Setup/Replay Humanoid")]
        //public static void SetupReplayHumanoid()
        //{
        //    ReplayHumanoidConfiguratorWindow.ShowWindow();
        //}

        public static void AddReplayObjectToSelection()
        {
            // Check for no selection
            if (Selection.activeGameObject == null)
                return;

            // Get selected game objects
            GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable);

            if (selected.Length == 0)
                return;

            // Record the apply operation
            Undo.RecordObjects(selected, "Add Replay Object");

            // Process all selected
            foreach (GameObject obj in selected)
            {
                // Check for already existing component
                if (obj.GetComponent<ReplayObject>() == null)
                {
                    // Add the component
                    obj.AddComponent<ReplayObject>();

                    // Record modifications
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
            }
        }

        public static void AddReplayComponentToSelection<T>() where T : ReplayRecordableBehaviour
        {
            // Check for no selection
            if (Selection.activeGameObject == null)
                return;

            // Get selected game objects
            GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable);

            if (selected.Length == 0)
                return;

            // Record the apply operation
            Undo.RecordObjects(selected, "Add Replay Components");

            //Undo.RecordObject(null, "Add Replay Component");
            int undoID = Undo.GetCurrentGroup();

            // Process all selected
            foreach (GameObject obj in selected)
            {
                // Check for already existing component
                if (obj.GetComponent<T>() == null)
                {
                    // Add the component
                    //obj.AddComponent<T>();
                    Undo.AddComponent<T>(obj);
                    
                }
                // Record modifications
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            }
            Undo.CollapseUndoOperations(undoID);
        }
    }
}
