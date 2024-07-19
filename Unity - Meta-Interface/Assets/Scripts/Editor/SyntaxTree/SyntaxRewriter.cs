using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node != null)
            {
                node = RemoveRegionDirectives(node);
            }

            return base.Visit(node);

            //// Check for root unit
            //if (node is CompilationUnitSyntax comp)
            //{
            //    // Remove region nodes
            //    node = node.RemoveNodes(node.DescendantNodesAndSelf()
            //        .Where(n => n is RegionDirectiveTriviaSyntax || n is EndRegionDirectiveTriviaSyntax)
            //        , SyntaxRemoveOptions.KeepNoTrivia);

            //    // Remove trivia
            //    node = node.ReplaceTrivia(node.DescendantTrivia(descendIntoTrivia: true)
            //        .Where(t => t.IsKind(SyntaxKind.RegionDirectiveTrivia) || t.IsKind(SyntaxKind.RegionKeyword) || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia) || t.IsKind(SyntaxKind.EndRegionKeyword))
            //        , default);
            //}
            //return base.Visit(node);

            //if (node != null)
            //{
            //    // Get trivial lists
            //    SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();
            //    SyntaxTriviaList trailingTrivial = node.GetTrailingTrivia();

            //    // Replace regions
            //    node = RemoveTrivia(node, leadingTrivia, SyntaxKind.RegionDirectiveTrivia);
            //    node = RemoveTrivia(node, leadingTrivia, SyntaxKind.EndRegionDirectiveTrivia);
            //    node = RemoveTrivia(node, leadingTrivia, SyntaxKind.EndRegionKeyword);
            //    node = RemoveTrivia(node, trailingTrivial, SyntaxKind.RegionDirectiveTrivia);
            //    node = RemoveTrivia(node, trailingTrivial, SyntaxKind.EndRegionDirectiveTrivia);
            //    node = RemoveTrivia(node, trailingTrivial, SyntaxKind.EndRegionKeyword);
            //}

            //return base.Visit(node);
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia) || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
            {
                // Return an empty trivia to remove the directive
                return default;
            }

            //if (trivia.HasStructure)
            //{
            //    var newStructure = this.Visit(trivia.GetStructure());
            //    return default;// return SyntaxFactory.Trivia(newStructure);
            //}

            return base.VisitTrivia(trivia);
        }

        public override SyntaxNode VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
        {
            return null;
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {


            // Namespace should remain in the syntax tree
            return base.VisitNamespaceDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Check if class is exposed
            if (SyntaxPatcher.IsClassDeclarationExposed(node, config) == false)
                return null;

            // Class should remain in the syntax tree
            SyntaxNode result = base.VisitClassDeclaration(node);

            // Remove comments
            //if(config.DiscardTypeComments == true)
            //    result = result.kin

            return result;
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
            return node;
            //return base.VisitInterfaceDeclaration(node);
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
            return SyntaxPatcher.PatchFieldInitializer(node); //base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            // Check if property is exposed
            if (SyntaxPatcher.IsPropertyDeclarationExposed(node) == false)
                return null;

            // Property should remain in the syntax tree
            return base.VisitPropertyDeclaration(SyntaxPatcher.PatchPropertyAccessorsLambda(node)); //base.VisitPropertyDeclaration(node);
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            // Check if accessor is exposed
            if (SyntaxPatcher.IsAccessorDeclarationHidden(node) == true)
                return null;

            // Accessor should remain in the syntax tree
            return base.VisitAccessorDeclaration(node);
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            // Check if constructor is exposed
            if (SyntaxPatcher.IsConstructorDeclarationExposed(node) == false)
                return null;

            // Constructor should remain in the syntax tree
            return SyntaxPatcher.PatchConstructorBodyLambda(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Check if method is exposed
            if (SyntaxPatcher.IsMethodDeclarationExposed(node) == false)
                return null;

            // Method should remain in the syntax tree
            return SyntaxPatcher.PatchMethodBodyLambda(node);
        }

        private T RemoveTrivia<T>(T node, SyntaxTriviaList trivialList, SyntaxKind kind) where T : SyntaxNode
        {
            SyntaxTriviaList.Enumerator enumerator = trivialList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                SyntaxTrivia current = enumerator.Current;
                if (current.IsKind(kind) == true)
                {
                    node = node.ReplaceTrivia(current, new SyntaxTrivia());
                }
            }
            return node;
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
    }
}
