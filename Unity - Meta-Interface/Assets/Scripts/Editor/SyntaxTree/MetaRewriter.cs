using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MetaInterface.Tests")]

namespace MetaInterface.Syntax
{
    internal sealed class MetaRewriter : CSharpSyntaxRewriter
    {
        // Methods
        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            // Don't modify abstract properties
            if (node.Modifiers.Any(SyntaxKind.AbstractKeyword)
                || node.Parent.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                return node;
            }

            // Replace the property body with a lambda expression that throws a NotSupportedException
            var lambda = SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ThrowExpression(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.ParseTypeName("System.NotSupportedException"))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList())));

            // Place the lambda expression on the same line as the property declaration
            var newNode = node
                .WithAccessorList(null)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithExpressionBody(lambda.WithLeadingTrivia(node.GetLeadingTrivia()))
                .WithTrailingTrivia(node.GetTrailingTrivia());

            return newNode;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Don't modify abstract methods
            if (node.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                node.Parent.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                return node;
            }

            // Replace the method body with a lambda expression that throws a NotSupportedException
            var lambda = SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.ThrowExpression(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.ParseTypeName("System.NotSupportedException"))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList())));

            var newNode = node
                .WithBody(null)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithExpressionBody(lambda.WithLeadingTrivia(node.GetLeadingTrivia()))
                .WithTrailingTrivia(node.GetTrailingTrivia());

            return newNode;
        }
    }
}
