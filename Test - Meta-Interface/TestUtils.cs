
using MetaInterface;
using Microsoft.CodeAnalysis;

namespace Meta_Interface_UnitTest
{
    internal class TestUtils
    {
        // Methods
        public static string CSharpSourceToMeta(string cSharpSource, MetaConfig config = null)
        {
            // Create meta source file
            MetaSourceFile source = MetaSourceFile.FromSource(cSharpSource);

            // Get patched node
            SyntaxNode node = source.ParseAndGenerateMeta(config);

            return node.ToFullString();
        }

        public static SyntaxNode CSharpSourceToExcludedMeta(string cSharpSource, MetaConfig config = null)
        {
            // Create meta source file
            MetaSourceFile source = MetaSourceFile.FromSource(cSharpSource);

            // Get patched node
            return source.ParseAndGenerateMeta(config);
        }

        public static string GetCSharpSourceResource(string resourceFile)
        {
            string path = "../../../TestResources/" + resourceFile;
            return File.ReadAllText(path);
        }
    }
}
