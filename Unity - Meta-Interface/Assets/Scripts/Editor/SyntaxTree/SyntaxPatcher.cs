﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MetaInterface.Syntax
{
    public class SyntaxPatcher
    {
        // Public
        public static string methodImplementationString = @"throw new System.NotImplementedException()";

        // Methods
        public static SyntaxNode InsertGeneratedComment(SyntaxNode syntax, string assemblyDefinition, string cSharpSource)
        {
            return syntax.WithLeadingTrivia(
                SyntaxFactory.Comment(string.Format(
@"/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: {0}
/// From source file: {1}
/// </summary>
", assemblyDefinition, cSharpSource)));
        }

        public static CompilationUnitSyntax InsertSuppressWarnings(CompilationUnitSyntax syntax, IEnumerable<string> suppressWarnings)
        {
            // Create warning directive
            PragmaWarningDirectiveTriviaSyntax warningDirective = SyntaxFactory.PragmaWarningDirectiveTrivia(
                SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                SyntaxFactory.SeparatedList<ExpressionSyntax>(nodes:
                    suppressWarnings.Select(w => SyntaxFactory.IdentifierName(w))),
                    true);

            // Add as leading trivia
            return syntax.WithLeadingTrivia(SyntaxFactory.Trivia(warningDirective));
        }

        public static bool IsClassDeclarationExposed(ClassDeclarationSyntax syntax, MetaConfig config)
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
            // Check for modifier list
            if (IsModifierListExposed(syntax.Modifiers) == true)
                return true;

            // Check for explicit interface
            if (syntax.ExplicitInterfaceSpecifier != null)
                return true;

            return false;
        }

        public static bool IsConstructorDeclarationExposed(ConstructorDeclarationSyntax syntax)
        {
            // Check modifier list
            if (IsModifierListExposed(syntax.Modifiers) == true)
                return true;

            //// Check for internally available ctor
            //if (syntax.Modifiers.Any(SyntaxKind.InternalKeyword) == true)
            //    return true;

            return false;
        }

        public static bool IsMethodDeclarationExposed(MethodDeclarationSyntax syntax)
        {
            // Check for modifier list
            if (IsModifierListExposed(syntax.Modifiers) == true)
                return true;

            // Check for explicit interface
            if (syntax.ExplicitInterfaceSpecifier != null)
                return true;

            return false;
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

        private static bool HasCustomAttributeWithName(SeparatedSyntaxList<AttributeListSyntax> attributes, string attributeName)
        {
            foreach (AttributeListSyntax list in attributes)
            {
                foreach (AttributeSyntax attrib in list.Attributes)
                {
                    if (attrib.Name.ToString() == attributeName)
                        return true;
                }
            }
            return false;
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

            // Check for constant
            bool isConst = syntax.Modifiers.Any(SyntaxKind.ConstKeyword);

            // Check for allowed declaration
            if (declaration != null && IsFieldInitializerAllowed(declaration) == false)
            {
                // Get the new declaration
                VariableDeclarationSyntax newDeclaration = PatchFieldInitializer(declaration, isConst);

                // Perform replacement
                return syntax.ReplaceNode(declaration, newDeclaration);
            }

            // Field declaration is fine and does not need to be patched
            return syntax;
        }

        public static VariableDeclarationSyntax PatchFieldInitializer(VariableDeclarationSyntax syntax, bool isConst)
        {
            SeparatedSyntaxList<VariableDeclaratorSyntax> variables = syntax.Variables;

            // Variable will be replaced as needed
            VariableDeclarationSyntax newSyntax = syntax;

            // Remove all assignments
            for (int i = 0; i < variables.Count; i++)
            {
                // Get the new declarator
                VariableDeclaratorSyntax newDeclarator = PatchFieldInitializer(variables[i], isConst);

                // Perform replacement
                newSyntax = newSyntax.ReplaceNode(variables[i], newDeclarator);
            }

            return newSyntax;
        }

        public static VariableDeclaratorSyntax PatchFieldInitializer(VariableDeclaratorSyntax syntax, bool isConst)
        {
            // Get the initializer
            EqualsValueClauseSyntax init = syntax.Initializer;

            // Remove the assignment expression
            if (init != null && isConst == false)
                return syntax.RemoveNode(init, SyntaxRemoveOptions.KeepEndOfLine);

            return syntax;
        }

        public static bool IsFieldInitializerAllowed(VariableDeclarationSyntax syntax)
        {
            // Check for no assignment which is a valid case
            if (syntax == null || syntax.Variables == null)
                return true;

            // Check all declarations
            foreach (VariableDeclaratorSyntax variable in syntax.Variables)
            {
                if (IsVariableInitializerAllowed(variable) == false)
                    return false;
            }
            return true;
        }

        public static bool IsVariableInitializerAllowed(VariableDeclaratorSyntax syntax)
        {
            // No assignment is a valid case
            if (syntax == null || syntax.Initializer == null)
                return true;

            // Check for literal or null
            return syntax.Initializer.Value is LiteralExpressionSyntax;
        }

        public static PropertyDeclarationSyntax PatchPropertyAccessorsLambda(PropertyDeclarationSyntax syntax)
        {
            // Check for abstract
            if (syntax.Modifiers.Any(SyntaxKind.AbstractKeyword) == true)
                return syntax;

            // Check for expression body
            if (syntax.ExpressionBody != null)
                return PatchPropertyAccessorBodyLambda(syntax);

            // Get all accessors
            SyntaxList<AccessorDeclarationSyntax> accessors = syntax.AccessorList.Accessors;

            for (int i = 0; i < accessors.Count; i++)
            {
                // Patch accessor body
                syntax = syntax.ReplaceNode(accessors[i], PatchPropertyAccessorBodyLambda(accessors[i]));
                accessors = syntax.AccessorList.Accessors;
            }

            // Check for initializer
            if (syntax.Initializer != null)
                syntax = syntax.RemoveNode(syntax.Initializer, SyntaxRemoveOptions.KeepNoTrivia);

            return syntax;
        }

        public static AccessorDeclarationSyntax PatchPropertyAccessorBodyLambda(AccessorDeclarationSyntax syntax)
        {
            // Remove body
            if (syntax.Body != null)
                syntax = syntax.RemoveNode(syntax.Body, SyntaxRemoveOptions.KeepNoTrivia);

            // Remove expression body
            if (syntax.ExpressionBody != null)
                syntax = syntax.RemoveNode(syntax.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia);

            // Build patched body
            return syntax.WithExpressionBody(GetMethodBodyLambdaReplacementSyntax())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        public static PropertyDeclarationSyntax PatchPropertyAccessorBodyLambda(PropertyDeclarationSyntax syntax)
        {
            // Strip accessor body - check for no body provided too
            if (StripPropertyExpressionBody(ref syntax) == false)
                return syntax;

            // Replace the accessor body
            return syntax.WithExpressionBody(GetMethodBodyLambdaReplacementSyntax())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }


        public static ConstructorDeclarationSyntax PatchConstructorBodyLambda(ConstructorDeclarationSyntax syntax)
        {
            // Check for no body - No need to replace constructor body
            if (syntax.Body == null && syntax.ExpressionBody == null)
                return syntax;

            SyntaxTriviaList trailingTrivia;

            // Strip method body - check for no body provided too
            if (StripConstructorBody(ref syntax, out trailingTrivia) == false)
                return syntax;

            // Check for initializer
            if (syntax.Initializer != null)
                syntax = syntax.ReplaceNode(syntax.Initializer.ArgumentList, PatchArgumentListDefault(syntax.Initializer.ArgumentList));

            // Create with expression
            return syntax.WithExpressionBody(GetMethodBodyLambdaReplacementSyntax())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        public static ArgumentListSyntax PatchArgumentListDefault(ArgumentListSyntax syntax)
        {
            // Check for empty arguments
            if (syntax.Arguments == null || syntax.Arguments.Count == 0)
                return syntax;

            // Patch the syntax
            for (int i = 0; i < syntax.Arguments.Count; i++)
            {
                syntax = syntax.ReplaceNode(syntax.Arguments[i], SyntaxFactory.Argument(
                    SyntaxFactory.ParseExpression("default")));
            }

            return syntax;
        }

        public static MethodDeclarationSyntax PatchMethodBodyLambda(MethodDeclarationSyntax syntax)
        {
            // Check for no method body - No need to replace method body
            if (syntax.Body == null && syntax.ExpressionBody == null)
                return syntax;

            SyntaxTriviaList trailingTrivia;

            // Strip method body - check for no body provided too
            if (StripMethodBody(ref syntax, out trailingTrivia) == false)
                return syntax;

            // Create with expression
            return syntax.WithExpressionBody(GetMethodBodyLambdaReplacementSyntax())
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private static bool StripPropertyExpressionBody(ref PropertyDeclarationSyntax syntax)
        {
            // Check for trailing semicolon
            if (syntax.SemicolonToken != null)
                syntax = syntax.ReplaceToken(syntax.SemicolonToken, SyntaxFactory.Token(SyntaxKind.None));

            // Remove current expression body
            if (syntax.ExpressionBody != null)
            {
                syntax = syntax.RemoveNode(syntax.ExpressionBody, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                return true;
            }
            else
                return false;
        }

        private static bool StripConstructorBody(ref ConstructorDeclarationSyntax syntax, out SyntaxTriviaList trailingTrivia)
        {
            // Get trailing trivia
            trailingTrivia = syntax.GetTrailingTrivia();

            // Check for trailing semicolon
            if (syntax.SemicolonToken != null)
                syntax = syntax.ReplaceToken(syntax.SemicolonToken, SyntaxFactory.Token(SyntaxKind.None));

            bool hasBody = false;

            // Remove current method body
            if (syntax.Body != null)
            {
                syntax = syntax.RemoveNode(syntax.Body, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                hasBody = true;
            }

            // Remove current expression
            if (syntax.ExpressionBody != null)
            {
                syntax = syntax.RemoveNode(syntax.ExpressionBody, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                hasBody = true;
            }

            // Check for no body (abstract)
            if (hasBody == false)
                return false;

            // Strip was successful
            return true;
        }

        private static bool StripMethodBody(ref MethodDeclarationSyntax syntax, out SyntaxTriviaList trailingTrivia)
        {
            // Get trailing trivia
            trailingTrivia = syntax.GetTrailingTrivia();

            // Check for trailing semicolon
            if (syntax.SemicolonToken != null)
                syntax = syntax.ReplaceToken(syntax.SemicolonToken, SyntaxFactory.Token(SyntaxKind.None));

            bool hasBody = false;

            // Remove current method body
            if (syntax.Body != null)
            {
                syntax = syntax.RemoveNode(syntax.Body, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                hasBody = true;
            }

            // Remove current expression
            if (syntax.ExpressionBody != null)
            {
                syntax = syntax.RemoveNode(syntax.ExpressionBody, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                hasBody = true;
            }

            // Check for no body (abstract)
            if (hasBody == false)
                return false;

            // Strip was successful
            return true;
        }

        public static T StripDisabledTrivia<T>(T node) where T : SyntaxNode
        {
            // Strip leading trivia
            foreach (SyntaxTrivia leadingTrivia in node.GetLeadingTrivia())
            {
                if (leadingTrivia.IsKind(SyntaxKind.DisabledTextTrivia) == true)
                    node = node.ReplaceTrivia(leadingTrivia, (SyntaxTrivia)default);
            }

            // Strip trailing trivia
            foreach (SyntaxTrivia trailingTrivia in node.GetTrailingTrivia())
            {
                if (trailingTrivia.IsKind(SyntaxKind.DisabledTextTrivia) == true)
                    node = node.ReplaceTrivia(trailingTrivia, (SyntaxTrivia)default);
            }
            return node;
        }

        public static ArrowExpressionClauseSyntax GetMethodBodyLambdaReplacementSyntax()
        {
            return SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ParseExpression(
                    methodImplementationString)
                .WithLeadingTrivia(SyntaxFactory.Space))
                .WithLeadingTrivia(SyntaxFactory.Space);
        }
    }
}
