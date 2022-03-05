using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;

namespace MetaInterface
{
    public class MetaSourceFile
    {
        // Private
        private string sourceFile = null;
        private SourceText sourceText = null;
        private CSharpSyntaxTree syntaxTree = null;

        // Properties
        public string SourceFile
        {
            get { return sourceFile; }
        }

        public SourceText SourceText
        {
            get { return sourceText; }
        }

        // Constructor
        public MetaSourceFile(string cSharpSourcePath)
        {
            // Check for null or empty
            if (string.IsNullOrEmpty(cSharpSourcePath) == true)
                throw new ArgumentException("Source path cannot be null or empty");

            // Check for file not found
            if (File.Exists(cSharpSourcePath) == false)
                throw new FileNotFoundException(cSharpSourcePath);

            this.sourceFile = cSharpSourcePath;
            this.sourceText = SourceText.From(File.ReadAllText(cSharpSourcePath));
        }

        // Methods
        public CSharpSyntaxTree Parse()
        {
            if (syntaxTree != null)
                return syntaxTree;

            // Parse from source
            syntaxTree = CSharpSyntaxTree.ParseText(sourceText) as CSharpSyntaxTree;

            return syntaxTree;
        }

        public void OverwriteSource(SyntaxNode root)
        {
            // Fix whitespace
            root = root.NormalizeWhitespace(); 

            // Save to file
            File.WriteAllText(Path.ChangeExtension(sourceFile, null) + ".Generated.cs", root.ToFullString());
        }
    }
}
