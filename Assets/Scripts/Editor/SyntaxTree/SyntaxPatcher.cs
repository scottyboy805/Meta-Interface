using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace MetaInterface.SyntaxTree
{
    public class SyntaxPatcher
    {
        // Public
        public static string methodImplementationString = @"throw new System.NotImplementedException();";

        // Methods
        //public static SyntaxNode InsertGeneratedComment(SyntaxNode syntax)
        //{
        //    if(syntax is CompilationUnitSyntax)
        //    {
        //        ((CompilationUnitSyntax)syntax).
        //    }
        //}

        public static bool IsClassDeclarationExposed(ClassDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsStructDeclarationExposed(StructDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsInterfaceDeclarationExposed(InterfaceDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsEnumDeclarationExposed(EnumDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsEventDeclarationExposed(EventDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsFieldDeclarationExposed(FieldDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsPropertyDeclarationExposed(PropertyDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsMethodDeclarationExposed(MethodDeclarationSyntax syntax)
        {
            return IsModifierListExposed(syntax.Modifiers);
        }

        public static bool IsAccessorDeclarationHidden(AccessorDeclarationSyntax syntax)
        {
            return IsModifierListHidden(syntax.Modifiers);
        }

        private static bool IsModifierListExposed(SyntaxTokenList modifiers)
        {
            return modifiers
                .Where(m => m.Kind() == SyntaxKind.PublicKeyword
                || m.Kind() == SyntaxKind.ProtectedKeyword)
                .Any();
        }

        private static bool IsModifierListHidden(SyntaxTokenList modifiers)
        {
            return modifiers
                .Where(m => m.Kind() == SyntaxKind.PrivateKeyword
                || m.Kind() == SyntaxKind.InternalKeyword)
                .Where(m => m.Kind() != SyntaxKind.PublicKeyword 
                && m.Kind() != SyntaxKind.ProtectedKeyword)
                .Any();
        }

        public static FieldDeclarationSyntax PatchFieldInitializer(FieldDeclarationSyntax syntax)
        {
            VariableDeclarationSyntax declaration = syntax.Declaration;

            // Check for allowed declaration
            if(declaration != null && IsFieldInitializerAllowed(declaration) == false)
            {
                // Get the new declaration
                VariableDeclarationSyntax newDeclaration = PatchFieldInitializer(declaration);

                // Perform replacement
                return syntax.ReplaceNode(declaration, newDeclaration);
            }

            // Field declaration is fine and does not need to be patched
            return syntax;
        }

        public static VariableDeclarationSyntax PatchFieldInitializer(VariableDeclarationSyntax syntax)
        {
            SeparatedSyntaxList<VariableDeclaratorSyntax> variables = syntax.Variables;

            // Variable will be replaced as needed
            VariableDeclarationSyntax newSyntax = syntax;

            // Remove all assignments
            for(int i = 0; i < variables.Count; i++)
            {
                // Get the new declarator
                VariableDeclaratorSyntax newDeclarator = PatchFieldInitializer(variables[i]);

                // Perform replacement
                newSyntax = newSyntax.ReplaceNode(variables[i], newDeclarator);
            }

            return newSyntax;
        }

        public static VariableDeclaratorSyntax PatchFieldInitializer(VariableDeclaratorSyntax syntax)
        {
            // Get the initializer
            EqualsValueClauseSyntax init = syntax.Initializer;

            // Remove the assignment expression
            if (init != null)
                return syntax.RemoveNode(init, SyntaxRemoveOptions.KeepEndOfLine);

            return syntax;
        }

        public static bool IsFieldInitializerAllowed(VariableDeclarationSyntax syntax)
        {
            // Check for no assignment which is a valid case
            if (syntax == null || syntax.Variables == null)
                return true;

            // Check all declarations
            foreach(VariableDeclaratorSyntax variable in syntax.Variables)
            {
                if (IsVariableInitializerAllowed(variable) == false)
                    return false;
            }
            return true;
        }

        public static bool IsVariableInitializerAllowed(VariableDeclaratorSyntax syntax)
        {
            // No assigmnent is a valid case
            if (syntax == null || syntax.Initializer == null)
                return true;

            // Check for literal or null
            return syntax.Initializer.Value is LiteralExpressionSyntax;
        }

        public static MethodDeclarationSyntax PatchMethodBody(MethodDeclarationSyntax syntax)
        {
            // Strip method body - check for no body provided too
            if (StripMethodBody(ref syntax) == false)
                return syntax;

            // Replace the method body
            return syntax.WithBody(GetMethodBodyReplacementSyntax());
        }

        public static MethodDeclarationSyntax PatchMethodBodyLambda(MethodDeclarationSyntax syntax)
        {
            // Strip method body - check for no body provided too
            if (StripMethodBody(ref syntax) == false)
                return syntax;

            // Create with expression
            return syntax.WithExpressionBody(GetMethodBodyLambdaReplacementSyntax());
        }

        private static bool StripMethodBody(ref MethodDeclarationSyntax syntax)
        {
            // Check for trailing semicolon
            if (syntax.SemicolonToken != null)
                syntax = syntax.ReplaceToken(syntax.SemicolonToken, SyntaxFactory.Token(SyntaxKind.None));

            bool hasBody = false;

            // Remove current method body
            if (syntax.Body != null)
            {
                syntax = syntax.RemoveNode(syntax.Body, SyntaxRemoveOptions.KeepNoTrivia);
                hasBody = true;
            }

            // Remove current expression
            if (syntax.ExpressionBody != null)
            {
                syntax = syntax.RemoveNode(syntax.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia);
                hasBody = true;
            }

            // Check for no body (abstract)
            if (hasBody == false)
                return false;

            // Strip was successful
            return true;
        }

        public static BlockSyntax GetMethodBodyReplacementSyntax()
        {
            return SyntaxFactory.Block(
                SyntaxFactory.ParseStatement(
                    methodImplementationString));
        }

        public static ArrowExpressionClauseSyntax GetMethodBodyLambdaReplacementSyntax()
        {
            return SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ParseExpression(
                    methodImplementationString));
        }
    }
}
