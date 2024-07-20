using MetaInterface.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;

#if UNITY
using UnityEditor;
#endif

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

#if UNITY
        public string Guid
        {
            get { return AssetDatabase.AssetPathToGUID(sourceFile); }
        }
#endif

        // Constructor
        private MetaSourceFile() { }

        // Methods
        public CSharpSyntaxTree Parse(MetaConfig config = null)
        {
            // Check for config
            if (config == null)
                config = MetaConfig.Default;

            // Get parsed tree
            if (syntaxTree != null)
                return syntaxTree;

            // Create parse options
            CSharpParseOptions options = new CSharpParseOptions(preprocessorSymbols: config.PreprocessorDefineSymbols);

            // Parse from source
            syntaxTree = CSharpSyntaxTree.ParseText(sourceText, options) as CSharpSyntaxTree;

            return syntaxTree;
        }

        public SyntaxNode ParseAndGenerateMeta(MetaConfig config = null)
        {
            // Check for config
            if (config == null)
                config = MetaConfig.Default;

            // Get syntax tree
            CSharpSyntaxTree syntaxTree = Parse(config);

            // Path source file
            SyntaxRewriter rewriter = new SyntaxRewriter(config);

            // Rewrite and patch declarations
            SyntaxNode patchedRoot = rewriter.Visit(syntaxTree.GetRoot());

            // Patch for comments
            SyntaxCommenter commenter = new SyntaxCommenter();
            patchedRoot = commenter.Visit(patchedRoot);

            return patchedRoot.NormalizeWhitespace();
        }

        public void OverwriteSource(SyntaxNode root)
        {
            // Save to file
            File.WriteAllText(sourceFile, root.ToFullString());
        }

        public static void WriteSource(string outputPath, SyntaxNode root)
        {
            // Save to file
            File.WriteAllText(outputPath, root.ToFullString());
        }

        public static MetaSourceFile FromFile(string cSharpFilePath)
        {
            // Check for null or empty
            if (string.IsNullOrEmpty(cSharpFilePath) == true)
                throw new ArgumentException("Source path cannot be null or empty");

            // Check for file not found
            if (File.Exists(cSharpFilePath) == false)
                throw new FileNotFoundException(cSharpFilePath);

            // Create the source file
            return new MetaSourceFile
            {
                sourceFile = cSharpFilePath,
                sourceText = SourceText.From(File.ReadAllText(cSharpFilePath)),
            };
        }

        public static MetaSourceFile FromSource(string cSharpSourceCode)
        {
            // Check for null or empty
            if (string.IsNullOrEmpty(cSharpSourceCode) == true)
                throw new ArgumentException("Source code cannot be null or empty");

            // Create the source file
            return new MetaSourceFile
            {
                sourceFile = "Unknown Source",
                sourceText = SourceText.From(cSharpSourceCode),
            };
        }
    }
}
