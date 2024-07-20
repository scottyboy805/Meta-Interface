using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DLCToolkit.BuildTools.Events
{
    internal static class DLCBuildEventHooks
    {
        // Methods
        public static void SafeInvokeBuildEventHookImplementations<TAttribute, TBase>(Action<TBase> methodInvoke) where TAttribute : Attribute
        {
            // Get implementations
            IEnumerable<TBase> implementations = LoadEventHookImplementations<TAttribute, TBase>();

            // Invoke all implementations
            foreach(TBase implementation in implementations)
            {
                try
                {
                    // Call use invoke
                    methodInvoke(implementation);
                }
                catch(Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        private static IEnumerable<TBase> LoadEventHookImplementations<TAttribute, TBase>() where TAttribute : Attribute
        {
            // Create result
            List<TBase> implementations = new List<TBase>();

            // Get this assembly name
            AssemblyName thisName = typeof(DLCBuildEventHooks).Assembly.GetName();

            // Check all loaded assemblies
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Get all references
                AssemblyName[] references = asm.GetReferencedAssemblies();

                // Check if this assembly references build tools (Required to implement build hooks)
                if (references.FirstOrDefault(a => a.FullName ==  thisName.FullName) == null)
                    continue;

                // Check all types
                foreach(Type type in asm.GetTypes())
                {
                    // Check for attribute
                    if(type.IsDefined(typeof(TAttribute), false) == true)
                    {
                        // Check for base
                        if(typeof(TBase).IsAssignableFrom(type) == false)
                        {
                            Debug.LogWarning("DLC build tools event hook does not implement the required base type: " + typeof(TBase).FullName);
                            continue;
                        }

                        // Try to create instance
                        try
                        {
                            // Create instance of the type
                            TBase instance = (TBase)Activator.CreateInstance(type);

                            // Check for error
                            if (instance != null)
                                implementations.Add(instance);
                        }
                        catch { }
                    }
                }
            }

            return implementations;
        }
    }
}
