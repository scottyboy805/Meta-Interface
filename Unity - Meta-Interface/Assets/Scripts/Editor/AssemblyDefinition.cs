#if UNITY_EDITOR
using MetaInterface.Syntax;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;

namespace MetaInterface
{
    public class AssemblyDefinition
    {
        // Private
        private Assembly asm = null;
        private MetaConfig config = MetaConfig.Default;
        private List<string> modifiedSourceFileGuids = new List<string>();

        // Properties
        public IReadOnlyList<string> ModifiedSourceFileGuids
        {
            get { return modifiedSourceFileGuids; }
        }

        // Constructor
        public AssemblyDefinition(Assembly asm, MetaConfig config = null)
        {
            if (asm == null)
                throw new ArgumentNullException(nameof(asm));

            this.asm = asm;

            if (config != null)
                this.config = config;
        }

        // Methods
        public bool GenerateSourceOverwrite()
        {
            try
            {
                // Process all sources files
                foreach (string source in asm.sourceFiles)
                {
                    // Create source
                    MetaSourceFile sourceFile = MetaSourceFile.FromFile(source);

                    // Rewrite and patch declarations
                    SyntaxNode patchedRoot = sourceFile.ParseAndGenerateMeta(config);
                    

                    // Insert a comment about the changes made to this source file
                    patchedRoot = SyntaxPatcher.InsertGeneratedComment(patchedRoot, asm.name + ".dll", source);

                    // Overwrite source
                    sourceFile.OverwriteSource(patchedRoot);

                    // Mark as modified
                    modifiedSourceFileGuids.Add(sourceFile.Guid);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool GenerateSource(string outputFolder)
        {
            try
            {
#if DEBUG
                UnityEngine.Debug.Log("Output meta source folder: " + outputFolder);
#endif

                // Process all sources files
                foreach (string source in asm.sourceFiles)
                {
                    // Get the full output path
                    string sourceOutputPath = Path.Combine(outputFolder, source);

                    // Make sure parent directory exists
                    string parentPath = Directory.GetParent(sourceOutputPath).FullName;

                    if(Directory.Exists(parentPath) == false)
                        Directory.CreateDirectory(parentPath);

                    // Create source
                    MetaSourceFile sourceFile = MetaSourceFile.FromFile(source);

                    // Rewrite and patch declarations
                    SyntaxNode patchedRoot = sourceFile.ParseAndGenerateMeta(config);

                    // Insert a comment about the changes made to this source file
                    patchedRoot = SyntaxPatcher.InsertGeneratedComment(patchedRoot, asm.name + ".dll", source);

#if DEBUG
                    UnityEngine.Debug.Log("Generate meta source: " +  sourceOutputPath);
#endif

                    // Write new source
                    MetaSourceFile.WriteSource(sourceOutputPath, patchedRoot);

                    // Mark as modified
                    modifiedSourceFileGuids.Add(sourceFile.Guid);
                }
            }
            catch(Exception e)
            {
#if DEBUG
                UnityEngine.Debug.LogException(e);
#endif
                return false;
            }

            return true;
        }
    }
}
#endif
