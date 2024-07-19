using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using MetaInterface.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MetaInterface.UnitTest
{
    [TestFixture]
    internal class TestProperties
    {
        [Test]
        public void TestProperty()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                public abstract class MyClass
                {
                    public int MyProperty { get; set; }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the property bodies have been replaced with lambda expression
            var property = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(property);
            Assert.IsNotNull(property.ExpressionBody);
            Assert.IsNotNull(property.ExpressionBody.Expression);
            Assert.IsTrue(property.ExpressionBody.Expression is ThrowExpressionSyntax);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestPropertyGetterOnly()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                public abstract class MyClass
                {
                    public int MyProperty { get; }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the property bodies have been replaced with lambda expression
            var property = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(property);
            Assert.IsNotNull(property.ExpressionBody);
            Assert.IsNotNull(property.ExpressionBody.Expression);
            Assert.IsTrue(property.ExpressionBody.Expression is ThrowExpressionSyntax);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestPropertySetterOnly()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                public abstract class MyClass
                {
                    public int MyProperty { set; }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the property bodies have been replaced with lambda expression
            var property = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(property);
            Assert.IsNotNull(property.ExpressionBody);
            Assert.IsNotNull(property.ExpressionBody.Expression);
            Assert.IsTrue(property.ExpressionBody.Expression is ThrowExpressionSyntax);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestAbstractProperty()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                public abstract class MyClass
                {
                    public abstract int MyProperty { get; set; }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the property bodies have been replaced with lambda expression
            var property = newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(property);
            Assert.IsNull(property.ExpressionBody);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }
    }
}
