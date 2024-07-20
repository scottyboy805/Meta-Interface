using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace MetaInterface.Syntax
{
    public class SyntaxCommenter : CSharpSyntaxRewriter
    {
        // Methods
        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            // Check for any declarations
            if(node.Members
                .OfType<MemberDeclarationSyntax>()
                .Any(declaration =>
                    declaration is ClassDeclarationSyntax ||
                    declaration is StructDeclarationSyntax ||
                    declaration is InterfaceDeclarationSyntax ||
                    declaration is EnumDeclarationSyntax) == false)
            {
                // Insert a comment
                return node.WithLeadingTrivia(
                        SyntaxFactory.Comment(
@"/// <summary>
/// Declarations have been stripped because they were part of the internal implementation
/// </summary>"));
            }

            // No need to visit base as we are only modifying the namespace
            return node;
        }
    }
}
