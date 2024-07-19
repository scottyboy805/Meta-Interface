using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay.StatePreparation
{
    /// <summary>
    /// The default <see cref="IReplayPreparer"/> used by Ultimate Replay to prepare game objects for gameplay and playback.
    /// </summary>
    [Serializable]
    public class DefaultReplayPreparer : IReplayPreparer, ISerializationCallbackReceiver
    {
        // Types
        [Serializable]
        public class ComponentPreparerSettings
        {
            // Public
            public SerializableType componentPreparerType;
            public bool enabled = true;
        }

        // Private
        private static readonly List<Component> sharedComponents = new List<Component>();

        [SerializeField, HideInInspector]
        private List<SerializableType> skipTypes = new List<SerializableType>
        {
            typeof(Camera),
            typeof(AudioSource),
            typeof(ParticleSystem),
            typeof(Canvas),
        };

        [SerializeField, HideInInspector]
        private List<ComponentPreparerSettings> preparerSettings = new List<ComponentPreparerSettings>();

        // Properties
        public IList<SerializableType> SkipTypes
        {
            get { return skipTypes; }
        }

        public IList<ComponentPreparerSettings> PreparerSettings
        {
            get { return preparerSettings; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        static DefaultReplayPreparer()
        {
            // Load all preparers
            ComponentPreparer.InitializePreparers();
        }

        // Methods
        /// <summary>
        /// Prepare the specified replay object for playback mode.
        /// </summary>
        /// <param name="replayObject">The replay object to prepare</param>
        public virtual void PrepareForPlayback(ReplayObject replayObject)
        {
            // Clear collection and fetch all components
            sharedComponents.Clear();
            replayObject.GetComponentsInChildren(sharedComponents);

            // Find all components on the object
            for (int i = 0; i < sharedComponents.Count; i++)
            {
                // Get the element
                Component component = sharedComponents[i];

                // Make sure the component is still alive and check for trivial case to avoid expensive type checks
                if (component == null || component == replayObject || component is Transform || component is ReplayBehaviour)
                    continue;

                bool skip = false;
                Type componentType = component.GetType();

                // Check if the component should be prepared or skipped
                for (int j = 0; j < skipTypes.Count; j++)
                {
                    SerializableType skipType = skipTypes[j];

                    // Check if the component is a skip type or child of
                    if (skipType.SystemType.IsAssignableFrom(componentType) == true)
                    {
                        // Set the skip flag
                        skip = true;
                        break;
                    }
                }

                // Check if we should skip the component
                if (skip == true)
                    continue;

                // Check for ignore attribute
                if (componentType.IsDefined(typeof(ReplayPreparerIgnoreAttribute), false) == true)
                    continue;

                // Try to find a preparer
                ComponentPreparer preparer = ComponentPreparer.FindPreparer(componentType);

                // Check for error
                if (preparer == null || preparer.enabled == false)
                    continue;

                // Prepare the component
                preparer.InvokePrepareForPlayback(component);
            }
        }

        /// <summary>
        /// Prepare the specified replay object for gameplay mode.
        /// </summary>
        /// <param name="replayObject">The replay object to prepare</param>
        public virtual void PrepareForGameplay(ReplayObject replayObject)
        {
            // Clear collection and fetch all components
            sharedComponents.Clear();
            replayObject.GetComponentsInChildren(sharedComponents);

            // Find all components on the object
            for(int i = 0; i <  sharedComponents.Count; i++)
            {
                // Get the element
                Component component = sharedComponents[i];

                // Make sure the component is still alive
                if (component == null || component == replayObject || component is Transform || component is ReplayBehaviour)
                    continue;

                bool skip = false;
                Type componentType = component.GetType();

                // Check if the component should be prepared or skipped
                for(int j = 0; j < skipTypes.Count; j++) 
                {
                    SerializableType skipType = skipTypes[j];

                    // Check if the component is a skip type or child of
                    if (skipType.SystemType.IsAssignableFrom(componentType) == true)
                    {
                        // Set the skip flag
                        skip = true;
                        break;
                    }
                }

                // Check if we should skip the component
                if (skip == true)
                    continue;

                // Check for ignore attribute
                if (componentType.IsDefined(typeof(ReplayPreparerIgnoreAttribute), false) == true)
                    continue;

                // Try to find a preparer
                ComponentPreparer preparer = ComponentPreparer.FindPreparer(componentType);

                // Check for error
                if (preparer == null || preparer.enabled == false)
                    continue;

                // Prepare the component
                preparer.InvokePrepareForGameplay(component);
            }
        }

        public bool HasSkipType(Type systemType)
        {
            foreach(SerializableType type in skipTypes)
            {
                if (type.SystemType == systemType)
                    return true;
            }
            return false;
        }

        public void OnBeforeSerialize()
        {
            // Create options
            foreach (ComponentPreparer preparer in ComponentPreparer.Preparers)// preparers)
            {
                // Check for component settings added
                bool exists = preparerSettings.Exists(p => p.componentPreparerType.SystemType == preparer.Attribute.componentType);

                // Check for exists
                if (exists == false)
                {
                    preparerSettings.Add(new ComponentPreparerSettings
                    {
                        componentPreparerType = preparer.Attribute.componentType,
                        enabled = true,
                    });
                }
            }
        }

        public void OnAfterDeserialize()
        {
            foreach(ComponentPreparer preparer in ComponentPreparer.Preparers)//preparers)
            {
                // Check for matching component
                ComponentPreparerSettings setting = preparerSettings.Find(p => p.componentPreparerType.SystemType == preparer.Attribute.componentType);

                if (setting != null)
                {
                    // Apply settings
                    preparer.enabled = setting.enabled;
                }
            }
        }

        public DefaultReplayPreparer CreateInstance()
        {
            DefaultReplayPreparer instance = new DefaultReplayPreparer();

            instance.skipTypes.Clear();
            instance.skipTypes.AddRange(skipTypes);

            instance.preparerSettings.Clear();
            instance.preparerSettings.AddRange(PreparerSettings);

            return instance;
        }
    }
}
