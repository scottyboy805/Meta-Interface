using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MetaInterface.SyntaxTree
{
    public class SyntaxRewriter : CSharpSyntaxRewriter
    {
        // Methods
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Check if class is exposed
            if (SyntaxPatcher.IsClassDeclarationExposed(node) == false)
                return null;

            // Class should remain in the syntax tree
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            // Check if struct is exposed
            if (SyntaxPatcher.IsStructDeclarationExposed(node) == false)
                return null;

            // Struct should remain in the syntax tree
            return base.VisitStructDeclaration(node);
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            // Check if interface is exposed
            if (SyntaxPatcher.IsInterfaceDeclarationExposed(node) == false)
                return null;

            // Interface should remain in the syntax tree
            return base.VisitInterfaceDeclaration(node);
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            // Check if enum is exposed
            if (SyntaxPatcher.IsEnumDeclarationExposed(node) == false)
                return null;

            // Enum should remain in the syntax tree
            return base.VisitEnumDeclaration(node);
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            // Check if event is exposed
            if (SyntaxPatcher.IsEventDeclarationExposed(node) == false)
                return null;

            // Event should remain in the syntax tree
            return base.VisitEventDeclaration(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // Check if field is exposed
            if (SyntaxPatcher.IsFieldDeclarationExposed(node) == false)
                return null;

            // Field should remain in the syntax tree
            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            // Check if property is exposed
            if (SyntaxPatcher.IsPropertyDeclarationExposed(node) == false)
                return null;

            // Property should remain in the syntax tree
            return base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Check if method is exposed
            if (SyntaxPatcher.IsMethodDeclarationExposed(node) == false)
                return null;

            // Method should remain in the syntax tree
            return SyntaxPatcher.PatchMethodBody(node);
        }
    }
}
