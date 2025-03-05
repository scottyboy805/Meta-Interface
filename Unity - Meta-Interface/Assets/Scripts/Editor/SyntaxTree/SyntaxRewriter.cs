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

        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            // Check for suppress warnings
            if(config.SuppressWarnings.Count > 0)
            {
                // Insert suppress warnings
                node = SyntaxPatcher.InsertSuppressWarnings(node, config.SuppressWarnings);
            }

            return base.VisitCompilationUnit(node);
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
            if (trivia.IsKind(SyntaxKind.DisabledTextTrivia) == true)
            {
                // Remove disabled text - gets compiled out
                return default;
            }

            return base.VisitTrivia(trivia);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // Check if class is exposed
            if (SyntaxPatcher.IsClassDeclarationExposed(node, config) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // For non-sealed classes we need to make sure there is either a public or protected parameterless constructor so that we can strip away `base` calls safely
            if(node.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)) == false && node.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)) == false)
            {
                // Get constructors
                bool hasCtor = node.DescendantNodes()
                    .OfType<ConstructorDeclarationSyntax>()
                    .Any(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword)) 
                        && (c.ParameterList.Parameters.Count == 0 || c.ParameterList.Parameters[0].Default != null));

                // We need to add a default ctor
                if(hasCtor == false)
                {
                    // Insert new constructor
                    node = node.AddMembers(SyntaxFactory.ConstructorDeclaration(node.Identifier)
                            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)))
                            .WithExpressionBody(SyntaxPatcher.GetMethodBodyLambdaReplacementSyntax())
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
                }
            }

            // Class should remain in the syntax tree
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            // Check if struct is exposed
            if (SyntaxPatcher.IsStructDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Struct should remain in the syntax tree
            return base.VisitStructDeclaration(node);
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            // Check if interface is exposed
            if (SyntaxPatcher.IsInterfaceDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Interface should remain in the syntax tree
            return node;
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            // Check if enum is exposed
            if (SyntaxPatcher.IsEnumDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Enum should remain in the syntax tree
            return node;
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            // Check if event is exposed
            if (SyntaxPatcher.IsEventDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Event should remain in the syntax tree
            return node;
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // Check if field is exposed
            if (SyntaxPatcher.IsFieldDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Field should remain in the syntax tree
            return SyntaxPatcher.PatchFieldInitializer(node);
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            // Check if property is exposed
            if (SyntaxPatcher.IsPropertyDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Property should remain in the syntax tree
            return SyntaxPatcher.PatchPropertyAccessorsLambda(node);
        }

        public override SyntaxNode VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            // Check if accessor is exposed
            if (SyntaxPatcher.IsAccessorDeclarationHidden(node) == true
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Accessor should remain in the syntax tree
            return node;
        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            // Check if constructor is exposed
            if (SyntaxPatcher.IsConstructorDeclarationExposed(node) == false
                && HasLeadingPreprocessorDirectives(node) == false)
                return null;

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

            // Constructor should remain in the syntax tree
            return SyntaxPatcher.PatchConstructorBodyLambda(node);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Check if method is exposed
            if (SyntaxPatcher.IsMethodDeclarationExposed(node) == false)
            {
                if (HasLeadingPreprocessorDirectives(node) == false)
                {
                    return null;
                }
            }

            // Remove any disabled trivia that might remain
            node = SyntaxPatcher.StripDisabledTrivia(node);

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
    }
}
