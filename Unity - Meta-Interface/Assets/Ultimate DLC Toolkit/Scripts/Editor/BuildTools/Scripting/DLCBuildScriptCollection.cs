using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace DLCToolkit.BuildTools.Scripting
{
    internal sealed class DLCBuildScriptCollection
    {
        // Private
        private ScriptAssemblyBatch compilationBatch = new ScriptAssemblyBatch();
        private List<ScriptAssemblyCompilation> includeCompilations = new List<ScriptAssemblyCompilation>();
        private List<string> includeSourceFiles = new List<string>();

        private Assembly[] cachedAssemblies = null;

        // Properties
        public ScriptAssemblyBatch CompilationBatch
        {
            get { return compilationBatch; }
        }

        public IReadOnlyList<ScriptAssemblyCompilation> IncludeCompilations
        {
            get { return includeCompilations; }
        }

        public IEnumerable<ScriptAssemblyCompilation> IncludeOrderedCompilations
        {
            get
            {
                foreach(ScriptAssemblyCompilation compilation in compilationBatch.OrderedCompilations)
                {
                    // Check for included
                    if(includeCompilations.Contains(compilation) == true)
                        yield return compilation;
                }
            }
        }

        public bool HasScriptAssemblies
        {
            get { return includeCompilations.Count > 0; }
        }

        // Methods
        public bool AddIncludeScriptSource(string scriptSourceFile)
        {
            // Try to get compilation
            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(scriptSourceFile);

            // Check for found
            if(string.IsNullOrEmpty(assemblyName) == true)
            {
                Debug.LogWarning("Script source file is not associated with any assembly definition and will not be included in the build: " + scriptSourceFile);
                return false;
            }

            // Remove extension
            assemblyName = Path.ChangeExtension(assemblyName, null);

            // Try to get the associated compilation
            if (cachedAssemblies == null)
                cachedAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);

            // Try to find matching assembly
            Assembly targetAssembly = cachedAssemblies.FirstOrDefault(a => a.name == assemblyName);

            // Check for null
            if(targetAssembly == null)
            {
                Debug.LogWarning("Script source file may be associated with an editor assembly definition and will not be included in the build: " + scriptSourceFile);
                return false;
            }

            // Register compilation
            ScriptAssemblyCompilation compilation = compilationBatch.GetOrCreateCompilation(targetAssembly);

            // Register as include compilation
            if(includeCompilations.Contains(compilation) == false)
                includeCompilations.Add(compilation);

            // Add source file
            Debug.Log("Add script source file to DLC: " + scriptSourceFile);
            includeSourceFiles.Add(scriptSourceFile);

            return true;
        }
    }
}
