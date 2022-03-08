using MetaInterface.SyntaxTree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using UnityEditor.Compilation;

namespace MetaInterface
{
    public class AssemblyDefinition
    {
        // Private
        private Assembly asm = null;

        // Constructor
        public AssemblyDefinition(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException(nameof(asm));

            this.asm = asm;
        }

        // Methods
        public bool GenerateSourceOverwrite()
        {
            // Process all sources files
            foreach(string source in asm.sourceFiles)
            {
                // Create source
                MetaSourceFile sourceFile = new MetaSourceFile(source);

                // Get syntax tree
                CSharpSyntaxTree syntaxTree = sourceFile.Parse();

                // Path source file
                SyntaxRewriter rewriter = new SyntaxRewriter();

                // Rewrite and patch declarations
                SyntaxNode patchedRoot = rewriter.Visit(syntaxTree.GetRoot());

                patchedRoot = SyntaxPatcher.InsertGeneratedComment(patchedRoot, asm.name + ".dll", source);

                // Overwrite source
                sourceFile.OverwriteSource(patchedRoot);
            }

            return false;
        }
    }
}
