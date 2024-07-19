using MetaInterface;
using Microsoft.CodeAnalysis;

namespace Meta_Interface_UnitTest
{
    internal class TestUtils
    {
        // Methods
        public static string CSharpSourceToMeta(string cSharpSource)
        {
            // Create meta source file
            MetaSourceFile source = MetaSourceFile.FromSource(cSharpSource);

            // Get patched node
            SyntaxNode node = source.ParseAndGenerateMeta();

            return node.ToFullString();
        }

        public static SyntaxNode CSharpSourceToExcludedMeta(string cSharpSource)
        {
            // Create meta source file
            MetaSourceFile source = MetaSourceFile.FromSource(cSharpSource);

            // Get patched node
            return source.ParseAndGenerateMeta();
        }
    }
}
