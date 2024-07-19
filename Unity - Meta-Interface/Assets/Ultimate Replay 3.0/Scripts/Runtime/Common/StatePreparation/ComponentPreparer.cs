using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace UltimateReplay.StatePreparation
{
    public abstract class ComponentPreparer
    {
        // Type
        private class ComponentPreparerSorter : IComparer<ComponentPreparer>
        {
            // Methods
            public int Compare(ComponentPreparer x, ComponentPreparer y)
            {
                return x.attribute.priority.CompareTo(y.attribute.priority);
            }
        }

        // Private
        private static bool initialized = false;
        private static readonly List<ComponentPreparer> preparers = new List<ComponentPreparer>();
        private static readonly Dictionary<Type, ComponentPreparer> preparersCache = new Dictionary<Type, ComponentPreparer>();
        private static readonly ComponentPreparerSorter preparerSorter = new ComponentPreparerSorter();

        private ReplayComponentPreparerAttribute attribute = null;

        // Properties
        public bool enabled = true;

        // Properties
        public static IReadOnlyList<ComponentPreparer> Preparers
        {
            get { return preparers; }
        }

        protected internal ReplayComponentPreparerAttribute Attribute
        {
            get { return attribute; }
            set { attribute = value; }
        }

        // Methods
        internal abstract void InvokePrepareForPlayback(Component component);

        internal abstract void InvokePrepareForGameplay(Component component);

        public static void InitializePreparers()
        {
            // Check for initialized
            if (initialized == true)
                return;

            // Get assembly name info
#if UNITY_WINRT && !UNITY_EDITOR
            string thisName = typeof(ReplayManager).GetTypeInfo().Assembly.FullName;
#else
            string thisName = typeof(ReplayManager).Assembly.GetName().FullName;
#endif

            // Check all asssemblies in domain
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                bool hasReference = false;

                // Check if the assembly has a reference to Ultimate Replay assembly - It is not possible to define a component preparer otherwise
                foreach(AssemblyName nameInfo in asm.GetReferencedAssemblies())
                {
                    if(string.Compare(thisName, nameInfo.FullName) == 0)
                    {
                        hasReference = true;
                        break;
                    }
                }

                // Check for reference - we can skip expensive reflection checks in this case as it is not possible to create a component preparer without referencing the UR 2.0 assmembly
                if (hasReference == false && asm.FullName != thisName)
                    continue;

                try
                {
                    foreach (Type type in asm.GetTypes())
                    {
                        // Check for attribute
#if UNITY_WINRT && !UNITY_EDITOR
                if(type.GetTypeInfo().GetCustomAttribute<ReplayComponentPreparerAttribute>() != null)
#else
                        if (type.IsDefined(typeof(ReplayComponentPreparerAttribute), false) == true)
#endif
                        {
#if UNITY_WINRT && !UNITY_EDITOR
                    ReplayComponentPreparerAttribute attribute = type.GetTypeInfo().GetCustomAttribute<ReplayComponentPreparerAttribute>();
#else
                            // Get the attributes
                            object[] data = type.GetCustomAttributes(typeof(ReplayComponentPreparerAttribute), false);

                            // Get the attribute
                            ReplayComponentPreparerAttribute attribute = data[0] as ReplayComponentPreparerAttribute;
#endif

                            // Make sure the type inheirts from ComponentPreparer
                            if (typeof(ComponentPreparer).IsAssignableFrom(type) == false)
                            {
                                Debug.LogWarning(string.Format("Custom replay component preparer '{0}' must inherit from ComponentPreparer<>", type));
                                continue;
                            }

                            // Create an instance
                            ComponentPreparer preparer = null;

                            try
                            {
                                // Try to create an instance
                                preparer = (ComponentPreparer)Activator.CreateInstance(type);
                            }
                            catch
                            {
                                Debug.LogWarning(string.Format("Failed to create an instance of custom replay component preparer '{0}'. Make sure the type has a default constructor", type));
                                continue;
                            }

                            // Cache the attribute
                            preparer.Attribute = attribute;

                            // Register the preparer
                            preparers.Add(preparer);
                        }
                    }
                }
                catch (TypeLoadException e)
                {
                    Debug.LogWarningFormat("Could not load types for assembly '{0}'. Types from this assembly using Ultimate Replay 3.0 attributes may not work correctly!", asm.FullName);
                    Debug.LogException(e);
                }
            }

            // Sort preparers
            preparers.Sort(preparerSorter);

            // Set initialized flag
            initialized = true;
        }

        public static ComponentPreparer FindPreparer(Type componentType)
        {
            InitializePreparers();

            // Check for cached
            ComponentPreparer result;
            if (preparersCache.TryGetValue(componentType, out result) == true)
                return result;

            foreach (ComponentPreparer preparer in preparers)
            {
                if (componentType == preparer.Attribute.componentType || preparer.Attribute.componentType.IsAssignableFrom(componentType) == true)
                {
                    // Update cache
                    if(componentType.IsGenericType == false && preparersCache.ContainsKey(componentType) == false)
                        preparersCache[componentType] = preparer;

                    return preparer;
                }
            }

            // No preparer found
            return null;
        }
    }

    public abstract class ComponentPreparer<T> : ComponentPreparer where T : Component
    {
        // Private
        private Dictionary<int, ReplayState> componentData = new Dictionary<int, ReplayState>();
        
        // Methods
        public abstract void PrepareForPlayback(T component, ReplayState additionalData);

        public abstract void PrepareForGameplay(T component, ReplayState additionalData);

        internal override void InvokePrepareForPlayback(Component component)
        {            
            // Check for correct type
            if ((component is T) == false)
                return;

            // Create a state to hold the data
            ReplayState state = ReplayState.pool.GetReusable();

            // Call the method
            PrepareForPlayback(component as T, state);

            // Check for any state information
            if(state.Size > 0)
            {
                // Get the component hash
                int hash = component.GetInstanceID();

                // Check if it already exists
                if(componentData.ContainsKey(hash) == true)
                {
                    // Update the initial state
                    componentData[hash] = state;
                    return;
                }

                // Create the initial state
                componentData.Add(hash, state);
            }
        }

        internal override void InvokePrepareForGameplay(Component component)
        {
            // Check for correct type
            if ((component is T) == false)
                return;

            // Create a state
            ReplayState state = null;

            // Get the hash
            int hash = component.GetInstanceID();

            // Check for data
            if(componentData.ContainsKey(hash) == true)
            {
                // Get the state data
                state = componentData[hash];
            }
            else
            {
                return;
            }

            // Make sure we have a state even if it is empty
            if (state == null)
                state = ReplayState.pool.GetReusable();

            // Reset the sate for reading
            state.PrepareForRead();

            // Invoke method
            PrepareForGameplay(component as T, state);
        }
    }
}
