using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltimateReplay.Storage;

namespace UltimateReplay.Formatters
{
    public abstract class ReplayFormatter : IReplaySerialize, IReplayTokenSerialize
    {
        // Private        
        private readonly IEnumerable<ReplayToken> tokens = null;

        private static Dictionary<int, Type> formatterToType = new Dictionary<int, Type>();
        private static Dictionary<Type, int> typeToFormatter = new Dictionary<Type, int>();
        private static Dictionary<Type, ReplayFormatter> sharedFormatters = new Dictionary<Type, ReplayFormatter>();

        [ReplayTokenSerialize("Formatter ID")]
        private byte formatterId = 0;

        // Properties        
        public byte FormatterId
        {
            get { return formatterId; }
            internal set { formatterId = value; }
        }

        // Constructor
        protected ReplayFormatter()
        {
            Type thisType = GetType();

            // Get serialize tokens
            tokens = ReplayToken.Tokenize(thisType);

            // Get id for formatter
            formatterId = GetFormatterId(thisType);
        }

        // Constructor
        static ReplayFormatter()
        {
            // Register UR 3.0 assembly formatters first
            RegisterAssemblyFormatters(typeof(ReplayFormatter).Assembly);

            // Get this assembly
            Assembly thisAsm = Assembly.GetExecutingAssembly();
            AssemblyName thisAsmName = thisAsm.GetName();

            // Check all loaded assemblies
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip UR 3.0
                if (asm == typeof(ReplayFormatter).Assembly)
                    continue;

                // Only check assemblies that reference Ultimate Replay 3.0 - since formatters must derive from ReplayFormatter which requires a reference to UR 3.0 - Saves a lot of unnecessary checks
                bool checkAssembly = false;
                if (asm != thisAsm)
                {
                    // Check for assembly references this assembly - If so, the assembly may define types which use the ReplaySerializer attribute
                    foreach (AssemblyName nameInfo in asm.GetReferencedAssemblies())
                    {
                        if (nameInfo == thisAsmName)
                        {
                            checkAssembly = true;
                            break;
                        }
                    }
                }
                else
                {
                    // We are processing this assembly which does indeed have serializer types
                    checkAssembly = true;
                }

                // Should the assembly be processed for serializers
                if (checkAssembly == false)
                    continue;

                // Register the formatters
                RegisterAssemblyFormatters(asm);
            }
        }

        // Methods
        #region TokenSerialize
        IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
        {
            foreach (ReplayToken token in tokens)
            {
                if (token.IsOptional == false || includeOptional == true)
                    yield return token;
            }
        }
        #endregion

        private static void RegisterAssemblyFormatters(Assembly asm)
        {
            // Types are sorted alphabetically to remain consistent between startups - so long as new formatters are not introduced
            foreach (Type type in asm.GetTypes().OrderBy(t => t.Name))
            {
                // Check for derived from ReplayFormatter
                if (typeof(ReplayFormatter).IsAssignableFrom(type) == true)
                {
                    // Register formatter
                    RegisterFormatter(type);
                }
            }
        }

        public abstract void OnReplaySerialize(ReplayState state);

        public abstract void OnReplayDeserialize(ReplayState state);


        public static ReplayFormatter CreateFormatter(byte formatterId)
        {
            // Get formatter id
            Type type = GetFormatterType(formatterId);

            // Check for error
            if (type == null)
                return null;

            // Create new instance
            return Activator.CreateInstance(type) as ReplayFormatter;
        }

        public static ReplayFormatter GetFormatter(byte formatterId)
        {
            // Get formatter id
            Type type = GetFormatterType(formatterId);

            // Check for error
            if (type == null)
                return null;

            // Check for shared
            ReplayFormatter formatter;
            if (sharedFormatters.TryGetValue(type, out formatter) == true)
                return formatter;

            // Create new and cache
            formatter = (ReplayFormatter)Activator.CreateInstance(type);
            sharedFormatters[type] = formatter;

            // Get shared instance
            return formatter;
        }

        public static T CreateFormatter<T>(byte formatterId) where T : ReplayFormatter
        {
            // Create formatter
            return CreateFormatter(formatterId) as T;
        }

        public static T GetFormatter<T>(byte formatterId) where T : ReplayFormatter
        {
            // Get formatter
            return GetFormatter(formatterId) as T;
        }

        public static Type GetFormatterType(byte formatterId)
        {
            // Try to find type
            Type type;
            if (formatterToType.TryGetValue(formatterId, out type) == true)
                return type;

            return null;
        }

        public static T GetFormatterOfType<T>() where T : ReplayFormatter
        {
            // Check for shared
            ReplayFormatter formatter;
            if (sharedFormatters.TryGetValue(typeof(T), out formatter) == true)
                return formatter as T;

            // Create new and cache
            formatter = (ReplayFormatter)Activator.CreateInstance(typeof(T));
            sharedFormatters[typeof(T)] = formatter;

            return formatter as T;
        }

        internal static byte GetFormatterId(string formatterTypeString)
        {
            // Try to find type
            Type formatterType = Type.GetType(formatterTypeString, false);

            // Check for null
            if (formatterType != null)
                return GetFormatterId(formatterType);

            return 0;
        }

        internal static byte GetFormatterId(Type formatterType)
        {
            // Get id as byte
            int id;
            if (typeToFormatter.TryGetValue(formatterType, out id) == true)
                return (byte)id;

            return 0;
        }

        internal static bool RegisterFormatter(Type formatterType)
        {
            // Check for already added
            if (typeToFormatter.ContainsKey(formatterType) == false)
            {
                // Get formatter id
                int id = formatterToType.Count + 1;

                // Register type
                formatterToType.Add(id, formatterType);
                typeToFormatter.Add(formatterType, id);

                // Success
                return true;
            }

            // Failed to register
            return false;
        }
    }
}
