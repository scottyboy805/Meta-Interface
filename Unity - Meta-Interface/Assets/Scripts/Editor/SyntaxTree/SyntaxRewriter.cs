using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace MetaInterface.Syntax
{
    public class SyntaxRewriter : CSharpSyntaxRewriter
    {
        // Private
        private MetaConfig config = null;

        // Constructor
        public SyntaxRewriter(MetaConfig config)
        {
            this.config = config;
        }

        // Methods
        public SyntaxNode VisitTree(SyntaxTree tree)
        {
            // Perform visit
            SyntaxNode result = Visit(tree.GetRoot());

            
            return result;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node != null)
            {
                node = RemoveRegionDirectives(node);
            }

            return base.Visit(node);
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            // Check for region
            if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
            {
                // Return an empty trivia to remove the directive
                return default;
            }

            // Check for disabled text - pre-processor directive that is disabled
            if(trivia.IsKind(SyntaxKind.DisabledTextTrivia) == true)
            {
                // Need to parse the trivia manually
                SyntaxTree disabledTree = CSharpSyntaxTree.ParseText(trivia.ToFullString());

                // Manually patch the syntax tree
                SyntaxNode patchedDisabledRoot = VisitTree(disabledTree);

                // Get the full string
                return SyntaxFactory.DisabledText(patchedDisabledRoot.ToFullString());
            }
                       

            return base.VisitTrivia(trivia);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Check if class is exposed
            if (SyntaxPatcher.IsClassDeclarationExposed(node, config) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Class should remain in the syntax tree
            SyntaxNode result = base.VisitClassDeclaration(node);

            return result;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            // Check if struct is exposed
            if (SyntaxPatcher.IsStructDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Struct should remain in the syntax tree
            return base.VisitStructDeclaration(node);
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            // Check if interface is exposed
            if (SyntaxPatcher.IsInterfaceDeclarationExposed(node) == false 
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Interface should remain in the syntax tree
            return node;
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            // Check if enum is exposed
            if (SyntaxPatcher.IsEnumDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Enum should remain in the syntax tree
            return base.VisitEnumDeclaration(node);
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            // Check if event is exposed
            if (SyntaxPatcher.IsEventDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Event should remain in the syntax tree
            return base.VisitEventDeclaration(node);
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // Check if field is exposed
            if (SyntaxPatcher.IsFieldDeclarationExposed(node) == false 
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Field should remain in the syntax tree
            return SyntaxPatcher.PatchFieldInitializer(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            // Check if property is exposed
            if (SyntaxPatcher.IsPropertyDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Property should remain in the syntax tree
            return base.VisitPropertyDeclaration(SyntaxPatcher.PatchPropertyAccessorsLambda(node));
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            // Check if accessor is exposed
            if (SyntaxPatcher.IsAccessorDeclarationHidden(node) == true
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Accessor should remain in the syntax tree
            return base.VisitAccessorDeclaration(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            // Check if constructor is exposed
            if (SyntaxPatcher.IsConstructorDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Constructor should remain in the syntax tree
            return SyntaxPatcher.PatchConstructorBodyLambda(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Check if method is exposed
            if (SyntaxPatcher.IsMethodDeclarationExposed(node) == false)
            {
                if (IsExplicitInterfaceDeclaration(node) == true ||
                    HasLeadingPreprocessorDirectives(node) == false)
                {
                    return null;
                }
            }

            // Method should remain in the syntax tree
            return SyntaxPatcher.PatchMethodBodyLambda(node);
        }

        private SyntaxNode RemoveRegionDirectives(SyntaxNode node)
        {
            var newLeadingTrivia = RemoveRegionDirectives(node.GetLeadingTrivia());
            var newTrailingTrivia = RemoveRegionDirectives(node.GetTrailingTrivia());

            return node.WithLeadingTrivia(newLeadingTrivia).WithTrailingTrivia(newTrailingTrivia);
        }

        private SyntaxTriviaList RemoveRegionDirectives(SyntaxTriviaList triviaList)
        {
            return new SyntaxTriviaList(triviaList.Where(trivia =>
                !trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) &&
                !trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)));
        }

        private bool HasLeadingPreprocessorDirectives(SyntaxNode node)
        {
            var leadingTrivia = node.GetLeadingTrivia();
            return leadingTrivia.Any(trivia =>
                trivia.IsKind(SyntaxKind.IfDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.ElseDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.ElifDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.EndIfDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.DefineDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.UndefDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.ErrorDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.WarningDirectiveTrivia) ||
                trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia));
        }

        private bool IsExplicitInterfaceDeclaration(MethodDeclarationSyntax methodNode)
        {
            return methodNode.Identifier.Text.Contains('.');
        }
    }
}
