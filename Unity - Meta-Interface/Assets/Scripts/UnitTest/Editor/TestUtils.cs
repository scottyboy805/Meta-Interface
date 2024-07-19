using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using UnityEngine;

namespace MetaInterface.UnitTest
{
    internal static class TestUtils
    {
        // Methods
        public static bool IsValidCSharpSyntax(SyntaxTree syntaxTree)
        {
            // Check for compilable
            CSharpSyntaxTree csharpSyntax = syntaxTree as CSharpSyntaxTree;

            // Get the diagnostics for the syntax tree that have a severity of Error
            var errors = syntaxTree.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (Diagnostic error in errors)
                Debug.LogError(error);

            // Check for any
            return errors.Any() == false;
        }
    }
}
