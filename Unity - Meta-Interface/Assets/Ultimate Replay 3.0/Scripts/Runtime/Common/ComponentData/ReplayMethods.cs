using System;
using System.Collections.Generic;
using System.Reflection;
using UltimateReplay.Storage;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateReplay
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal sealed class ReplayMethods
    {
        // Private
        private static readonly byte serializeMethodID = 56;
        private static Dictionary<int, MethodInfo> replayMethods = new Dictionary<int, MethodInfo>();
        private static Dictionary<MethodInfo, int> replayMethodHashes = new Dictionary<MethodInfo, int>();

        // Constructor
        static ReplayMethods()
        {
            // Get assembly name info
#if UNITY_WINRT && !UNITY_EDITOR
            string thisName = typeof(ReplayManager).GetTypeInfo().Assembly.FullName;
#else
            string thisName = typeof(ReplayManager).Assembly.GetName().FullName;
#endif

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                bool hasReference = false;

                // Check if the assembly has a reference to Ultimate Replay assembly - It is not possible to define a component preparer otherwise
                foreach (AssemblyName nameInfo in asm.GetReferencedAssemblies())
                {
                    if (string.Compare(thisName, nameInfo.FullName) == 0)
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
                        if (type.IsClass == true && type.IsAbstract == false && typeof(ReplayBehaviour).IsAssignableFrom(type) == true)
                        {
                            foreach (MethodInfo method in type.GetMethods())
                            {
                                // Register the replay method
                                if (method.IsDefined(typeof(ReplayMethodAttribute), false) == true)
                                {
                                    int hash = CalculateReplayMethodSignatureHash(method);
                                    replayMethods.Add(hash, method);
                                    replayMethodHashes.Add(method, hash);
                                }
                            }
                        }
                    }
                }
                catch(TypeLoadException e)
                {
                    Debug.LogWarningFormat("Could not load types for assembly '{0}'. Types from this assembly using Ultimate Replay 3.0 attributes may not work correctly!", asm.FullName);
                    Debug.LogException(e);
                }
            }
        }

        // Methods
        public static bool SerializeMethodInfo(MethodInfo method, ReplayState state, object[] args)
        {
            // Check for replay method
            if(replayMethodHashes.ContainsKey(method) == false)
            {
                Debug.LogWarningFormat("The method '{0}' cannot be serialized because it is not decorated with the 'ReplayMethod' attribute", method);
                return false;
            }

            // Get method hash
            int hash = GetReplayMethodHash(method);

            // Write method identifier
            state.Write(serializeMethodID);

            // Write method hash
            state.Write(hash);

            // Get param list
            ParameterInfo[] parameters = method.GetParameters();

            // Write the parameter types
            for (int i = 0; i < parameters.Length; i++)
            {
                // Get the parameter type
                Type paramType = parameters[i].ParameterType;

                // Check if parameter is serializable
                if(ReplayState.IsTypeSerializable(paramType) == false)
                {
                    Debug.LogWarningFormat("The replay method '{0}' cannot be recorded because the parameter type '{1}' is not serializable", method, paramType);
                    return false;
                }
            }

            // Write all arguments
            return SerializeMethodArguments(method, state, args);
        }

        private static bool SerializeMethodArguments(MethodInfo method, ReplayState state, params object[] args)
        {
            // Get method params
            ParameterInfo[] parameters = method.GetParameters();

            // Write the argument length in case of optional arguments
            state.Write((ushort)parameters.Length);

            if(args.Length <= parameters.Length)
            {
                for(int i = 0; i < args.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;

                    // Check for enum
                    if(parameterType.IsEnum == true)
                        parameterType = parameterType.GetEnumUnderlyingType();

                    // Get the method used to serialize the parameters
                    MethodInfo serializeMethod = ReplayState.GetSerializeMethod(parameterType);

                    // Failed to get write method
                    if (serializeMethod == null)
                        return false;

                    // Check if type needs conversion
                    if (args[i] != null && args[i].GetType() != parameterType)
                        args[i] = Convert.ChangeType(args[i], parameterType);

                    // Write the parameter
                    serializeMethod.Invoke(state, new object[] { args[i] });
                }
            }
            return true;
        }

        public static bool DeserializeMethodInfo(ReplayState state, out MethodInfo method, out object[] args)
        {
            method = null;
            args = null;

            // Check for identifier
            if (state.ReadByte() != serializeMethodID)
                return false;

            // Check for version 110
            if (ReplayStorage.DeserializeVersionContext >= 110)
            {
                int hash = state.ReadInt32();

                // Try to get method
                replayMethods.TryGetValue(hash, out method);
            }
            else
            {
                // Get declaring type name
                string assemblyQualifiedName = state.ReadString();

                // Try to resolve type
                Type resolvedType = Type.GetType(assemblyQualifiedName);

                // Check for failure
                if (resolvedType == null)
                    return false;

                // Get method name
                string methodName = state.ReadString();

                // Get parameter count
                byte paramsLength = state.ReadByte();

                Type[] parameterTypes = new Type[paramsLength];

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    // Get the full name for the parameter type
                    string assemblyQualifiedParemterName = state.ReadString();

                    // Try to resolve
                    parameterTypes[i] = Type.GetType(assemblyQualifiedParemterName);

                    if (parameterTypes[i] == null)
                        return false;
                }

                // Check for no parameters
                if (paramsLength == 0)
                    parameterTypes = Type.EmptyTypes;

                // Try to resolve the method
                method = resolvedType.GetMethod(methodName, parameterTypes);
            }

            // Get arg values
            if(method != null)
                args = DeserializeMethodArguments(method, state);

            return method != null;
        }

        private static object[] DeserializeMethodArguments(MethodInfo method, ReplayState state)
        {
            // Get method params
            ParameterInfo[] parameters = method.GetParameters();

            // Read arg length
            ushort length = state.ReadUInt16();

            object[] arguments = new object[length];

            for (int i = 0; i < length; i++)
            {
                Type parameterType = parameters[i].ParameterType;

                // Get enum type
                if (parameters[i].ParameterType.IsEnum == true)
                    parameterType = parameterType.GetEnumUnderlyingType();

                // Get the method used to serialize the parameters
                MethodInfo deserializeMethod = ReplayState.GetDeserializeMethod(parameterType);

                // Failed to get write method
                if (deserializeMethod == null)
                    return null;

                // Write the parameter
                arguments[i] = deserializeMethod.Invoke(state, null);

                // Check if type needs conversion
                if (arguments[i] != null && arguments[i].GetType() != parameterType)
                    arguments[i] = Convert.ChangeType(arguments[i], parameterType);
            }

            return arguments;
        }

        public static MethodInfo GetReplayMethod(int hash)
        {
            MethodInfo method;
            replayMethods.TryGetValue(hash, out method);

            return method;
        }

        public static int GetReplayMethodHash(MethodInfo method)
        {
            int hash = 0;
            replayMethodHashes.TryGetValue(method, out hash);

            return hash;
        }

        /// <summary>
        /// Calculate a fixed hash that will be stable between sessions for a given method signature.
        /// Value will remain the same unless the method signature changes in any way.
        /// Signature includes assembly name and version, type namespace and name, method name, method return type and method parameter types.
        /// </summary>
        /// <param name="replayMethod">The method to generate the hash for</param>
        /// <returns>An almost certainly unique hash that will be deterministic between sessions</returns>
        public static int CalculateReplayMethodSignatureHash(MethodInfo replayMethod)
        {
            // Get parameters
            ParameterInfo[] parameters = replayMethod.GetParameters();

            // Create signature hash
            uint hash = 87992108;

            unchecked
            {
                // Assembly string
                hash += hash * CalculateFixedStringHash(replayMethod.DeclaringType.AssemblyQualifiedName);

                // Method name
                hash += hash + CalculateFixedStringHash(replayMethod.Name);

                // Method return type
                hash += hash & CalculateFixedStringHash(replayMethod.ReturnType.AssemblyQualifiedName);

                // Parameter types
                for(int i = 0; i < parameters.Length; i++)
                {
                    hash += hash - CalculateFixedStringHash(parameters[i].ParameterType.AssemblyQualifiedName) << i;
                }
            }
            return (int)hash;
        }

        /// <summary>
        /// Calculate a fixed hash for the given string that will be stable between sessions, platforms and architectures.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static uint CalculateFixedStringHash(string input)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                // Check all characters
                for (int i = 0; i < input.Length && input[i] != '\0'; i += 2)
                {
                    // Create hash value
                    hash1 = ((hash1 << 5) + hash1) ^ input[i];

                    // Check for end of string
                    if (i == input.Length - 1 || input[i + 1] == '\0')
                        break;

                    // Combine hash value
                    hash2 = ((hash2 << 5) + hash2) ^ input[i + 1];
                }

                return (uint)(hash1 + (hash2 * 1566083941));
            }
        }
    }
}
