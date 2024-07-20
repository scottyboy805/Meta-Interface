using DLCToolkit.Profile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEngine;

namespace DLCToolkit.BuildTools.Scripting
{
    internal enum CompilationResult
    {
        Failed,
        Compiled,
        CompiledWithoutSymbols,
    }

    internal sealed class ScriptAssemblyBatch
    {
        // Private
        private Queue<ScriptAssemblyCompilation> orderedCompilations = new Queue<ScriptAssemblyCompilation>();
        private string scriptCompilationDirectory = null;
        private string pdb2mdbPath = null;

        // Properties
        public Queue<ScriptAssemblyCompilation> OrderedCompilations
        {
            get { return orderedCompilations; }
        }

        public int OrderedCompilationsCount
        {
            get { return orderedCompilations.Count; }
        }

        public string PDB2MDBDebugSymbolsToolPath
        {
            get
            {
                // Check for cached
                if(pdb2mdbPath != null)
                    return pdb2mdbPath;

                // Get PDB2MDB tool path
                string editorDirectory = Directory.GetParent(EditorApplication.applicationPath).FullName;

                // Search in folder
                string pdb2mdbDirectory = Path.Combine(editorDirectory, "Data/MonoBleedingEdge/lib/mono/4.5");

                // Search for pdb2mdb
                string[] files = Directory.GetFiles(pdb2mdbDirectory, "pdb2mdb.exe", SearchOption.TopDirectoryOnly);

                // Check for found
                if (files.Length <= 0)
                {
                    Debug.LogWarning("Failed to locate PDB2MDB tool! Debug symbols will not be available!");
                    return pdb2mdbPath = "";
                }

                // Select path
                pdb2mdbPath = files[0];
                return pdb2mdbPath;
            }
        }


        // Methods
        public CompilationResult RequestPlayerCompilation(BuildTarget buildTarget, IEnumerable<ScriptAssemblyCompilation> includeCompilations, string[] defines, bool debugMode)
        {
            Debug.Log("Compiling DLC scripts for player runtime...");

            // Create compilation directory
            this.scriptCompilationDirectory = ScriptAssemblyCompilation.compilationDirectory + "/" + DLCPlatformProfile.GetFriendlyPlatformName(buildTarget);

            if (Directory.Exists(scriptCompilationDirectory) == false)
                Directory.CreateDirectory(scriptCompilationDirectory);

            // Request compilation
            PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
            {
                target = buildTarget,
                extraScriptingDefines = defines,
                options = debugMode == true 
                    ? ScriptCompilationOptions.DevelopmentBuild 
                    : ScriptCompilationOptions.None,
            }, scriptCompilationDirectory);

            // Check if all assemblies were compiled
            foreach(ScriptAssemblyCompilation compilation in includeCompilations)
            {
                Debug.Log("Compiling script assembly: " + compilation.AssemblyName);

                // Check for success
                if (compilation.CheckCompilationRequest(scriptCompilationDirectory) == false)
                {
                    Debug.LogError("Script compilation failed: " + compilation.AssemblyName);
                    return CompilationResult.Failed;
                }
            }

            // Check for debug - only supported on windows platforms
            if(debugMode == true && (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64))
            {
                // Try to generate the debug symbols
                if (RequestGenerateSymbols() == false)
                    return CompilationResult.CompiledWithoutSymbols;
            }

            return CompilationResult.Compiled;
        }

        public bool RequestGenerateSymbols()
        {
            // Check for allowed
            if(Application.platform != RuntimePlatform.WindowsEditor)
            {
                Debug.LogWarning("Generating debug symbols is only supported on windows platforms!");
                return false;
            }

            // Process all
            foreach(ScriptAssemblyCompilation compilation in orderedCompilations)
            {
                // Try to generate symbols
                if (compilation.RequestGenerateSymbols() == false)
                    return false;
            }
            return true;
        }

        public void CleanupCompilationDirectory()
        {
            if (scriptCompilationDirectory != null && Directory.Exists(scriptCompilationDirectory) == true)
                Directory.Delete(scriptCompilationDirectory, true);
        }

        public ScriptAssemblyCompilation GetOrCreateCompilation(Assembly assembly)
        {
            // Check for already registered
            ScriptAssemblyCompilation result = orderedCompilations.FirstOrDefault(c => c.Assembly == assembly);

            // Check for found
            if(result != null)
                return result;

            // Create and register new compilation
            result = new ScriptAssemblyCompilation(this, assembly);
            orderedCompilations.Enqueue(result);

            // Order results
            OrderByDependency();

            return result;
        }

        public void OrderByDependency()
        {
            // Check for simple case
            if (orderedCompilations.Count <= 1)
                return;

            // Create structures
            IList<ScriptAssemblyCompilation> input = orderedCompilations.ToArray();
            List<ScriptAssemblyCompilation> visited = new List<ScriptAssemblyCompilation>();
            List<ScriptAssemblyCompilation> pending = new List<ScriptAssemblyCompilation>();

            // Clear queue and it will be refilled in dependency order
            orderedCompilations.Clear();

            // Topological sort
            OrderByDependencyVisit(input, orderedCompilations, visited, pending);
        }

        private void OrderByDependencyVisit(IList<ScriptAssemblyCompilation> compilations, Queue<ScriptAssemblyCompilation> ordered, IList<ScriptAssemblyCompilation> visited, IList<ScriptAssemblyCompilation> pending) 
        {
            // Check all compilations
            foreach (ScriptAssemblyCompilation compilation in compilations)
            {
                // Check for visited
                if (visited.Contains(compilation) == false)
                {
                    // Check for pending
                    if (pending.Contains(compilation) == false)
                    {
                        pending.Add(compilation);
                    }
                    else
                        throw new InvalidOperationException("Cyclic dependency chain detected for script assembly compilation: " + compilation.AssemblyName);

                    // Visit recursively
                    OrderByDependencyVisit(compilation.CompilationReferences, ordered, visited, pending);

                    // Remove from pending
                    if (pending.Contains(compilation) == true)
                        pending.Remove(compilation);

                    // Add to visited
                    visited.Add(compilation);

                    // Add to results
                    ordered.Enqueue(compilation);
                }
            }
        }
    }
}
