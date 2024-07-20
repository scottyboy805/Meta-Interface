using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace DLCToolkit.BuildTools.Scripting
{
    internal enum ScriptAssemblyCompilationState
    {
        NotCompiled = 0,
        CompiledSuccessfully,
        FailedToCompile,
    }

    internal sealed class ScriptAssemblyCompilation
    {
        // Private
        private ScriptAssemblyBatch batch = null;
        private Assembly assembly = null;
        private string assemblyName = null;
        private string assemblyOutputPath = null;
        private List<ScriptAssemblyCompilation> compilationReferences = new List<ScriptAssemblyCompilation>();

        private ScriptAssemblyCompilationState compilationState = ScriptAssemblyCompilationState.NotCompiled;
        private string compilationOutputDirectory = null;

        // Public
        public const string compilationDirectory = "Temp/ScriptAssemblies";
        public const string symbolsExtension = ".mdb";

        // Properties
        public Assembly Assembly
        {
            get { return assembly; }
        }

        public string AssemblyName
        {
            get { return assemblyName; }
        }

        public string AssemblyOutputPath
        {
            get { return assemblyOutputPath; }
        }

        public string CompilationOutputPath
        {
            get
            {
                if (compilationOutputDirectory == null)
                    return assemblyOutputPath;

                return compilationOutputDirectory + "/" + assemblyName + ".dll";
            }
        }

        public IList<ScriptAssemblyCompilation> CompilationReferences
        {
            get { return compilationReferences; }
        }

        public ScriptAssemblyCompilationState CompilationState
        {
            get { return compilationState; }
        }

        public bool HasDebugSymbols
        {
            get { return File.Exists(Path.ChangeExtension(assemblyOutputPath, symbolsExtension)) == true; }
        }

        // Constructor
        internal ScriptAssemblyCompilation(ScriptAssemblyBatch batch, Assembly assembly)
        {
            this.batch = batch;
            this.assembly = assembly;
            this.assemblyName = assembly.name;
            this.assemblyOutputPath = Path.Combine(compilationDirectory, assembly.name + ".dll");// assembly.outputPath;

            // Add all uncompiled references
            this.compilationReferences.AddRange(assembly.assemblyReferences.Select(r => batch.GetOrCreateCompilation(r)));
        }

        // Methods
        public bool CheckCompilationRequest(string compilationOutputDirectory)
        {
            this.compilationOutputDirectory = compilationOutputDirectory;

            // Update status
            compilationState = File.Exists(CompilationOutputPath) == true
                ? ScriptAssemblyCompilationState.CompiledSuccessfully
                : ScriptAssemblyCompilationState.FailedToCompile;

            // Check for success
            return compilationState == ScriptAssemblyCompilationState.CompiledSuccessfully;
        }

        public bool RequestGenerateSymbols()
        {
            // Load tool
            string pdb2mdbTool = batch.PDB2MDBDebugSymbolsToolPath;

            // Check for available
            if (File.Exists(pdb2mdbTool) == false)
                return false;

            // Get pdb file path
            string pdbPath = Path.ChangeExtension(assemblyOutputPath, ".pdb");
            string mdbTargetPath = Path.ChangeExtension(assemblyOutputPath, ".mdb");

            ProcessStartInfo info = new ProcessStartInfo(pdb2mdbTool, Path.GetFileName(assemblyOutputPath));
            info.WorkingDirectory = Directory.GetParent(assemblyOutputPath).FullName;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;

            // Launch the process
            Process process = Process.Start(info);

            // Wait for the process to finish
            process.WaitForExit();
            Debug.Log(process.StandardOutput.ReadToEnd());

            // Check for success
            return File.Exists(mdbTargetPath) == true;
        }

        public byte[] ReadAssemblyImage()
        {
            return File.ReadAllBytes(CompilationOutputPath);
        }

        public byte[] ReadSymbolsImage()
        {
            return File.ReadAllBytes(Path.ChangeExtension(CompilationOutputPath, symbolsExtension));
        }

        //private string[] CollectCompilationsReferences()
        //{
        //    List<string> references = new List<string>();

        //    // Add all compiled
        //    references.AddRange(compiledReferences);

        //    // Add all compilations
        //    foreach(ScriptAssemblyCompilation dependencyCompilation in compilationReferences)
        //    {
        //        // Check for compiled
        //        if (dependencyCompilation.compilationState == ScriptAssemblyCompilationState.NotCompiled)
        //            throw new InvalidOperationException("One or more dependant script compilations have not yet been compiled: " + dependencyCompilation.assemblyName);

        //        // Check for failed to compile
        //        if (dependencyCompilation.compilationState == ScriptAssemblyCompilationState.FailedToCompile)
        //            throw new InvalidOperationException("One or more dependant script compilations failed to compile: " + dependencyCompilation.assemblyName);

        //        // Add the reference
        //        references.Add(dependencyCompilation.AssemblyOutputPath);
        //    }

        //    return references.ToArray();
        //}
    }
}
