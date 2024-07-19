using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using MetaInterface.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MetaInterface.UnitTest
{
    [TestFixture]
    public class TestMethods
    {
        [Test]  
        public void TestParameterlessMethod()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                class MyClass
                {
                    public void MyMethod()
                    {
                        Console.WriteLine(""Hello, world!"");
                    }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the method body has been replaced with a lambda expression
            var method = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(method);
            Assert.IsNotNull(method.ExpressionBody);
            Assert.IsNotNull(method.ExpressionBody.Expression);
            Assert.IsTrue(method.ExpressionBody.Expression is ThrowExpressionSyntax);
            Assert.IsTrue(method.Body is null);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestParameterlessGenericMethod()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                class MyClass
                {
                    public void MyMethod<T>()
                    {
                        Console.WriteLine(""Hello, world!"");
                    }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the method body has been replaced with a lambda expression
            var method = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(method);
            Assert.IsNotNull(method.ExpressionBody);
            Assert.IsNotNull(method.ExpressionBody.Expression);
            Assert.IsTrue(method.ExpressionBody.Expression is ThrowExpressionSyntax);
            Assert.IsTrue(method.Body is null);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestMethodWithArguments()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                class MyClass
                {
                    public void MyMethod(int a, string b, ref float d, out double e)
                    {
                        Console.WriteLine(""Hello, world!"");
                    }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the method body has been replaced with a lambda expression
            var method = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(method);
            Assert.IsNotNull(method.ExpressionBody);
            Assert.IsNotNull(method.ExpressionBody.Expression);
            Assert.IsTrue(method.ExpressionBody.Expression is ThrowExpressionSyntax);
            Assert.IsTrue(method.Body is null);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestGenericMethodWithArguments()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                class MyClass
                {
                    public void MyMethod<T>(int a, string b, ref float d, out double e)
                    {
                        Console.WriteLine(""Hello, world!"");
                    }
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the method body has been replaced with a lambda expression
            var method = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(method);
            Assert.IsNotNull(method.ExpressionBody);
            Assert.IsNotNull(method.ExpressionBody.Expression);
            Assert.IsTrue(method.ExpressionBody.Expression is ThrowExpressionSyntax);
            Assert.IsTrue(method.Body is null);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestAbstractParameterlessMethod()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                class MyClass
                {
                    public abstract void MyMethod();
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the method body has been replaced with a lambda expression
            var method = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(method);
            Assert.IsNull(method.Body);
            Assert.IsNull(method.ExpressionBody);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }

        [Test]
        public void TestAbstractMethodWithArguments()
        {
            // Parse the code into a SyntaxTree
            var code = @"
                class MyClass
                {
                    public abstract void MyMethod(int a, string b, ref float d, out double e);
                }
            ";
            var tree = CSharpSyntaxTree.ParseText(code);

            // Use the MethodBodyReplacer to visit the syntax tree and replace the method bodies
            var root = tree.GetRoot();
            var replacer = new MetaRewriter();
            var newRoot = replacer.Visit(root).NormalizeWhitespace();

            // Verify that the method body has been replaced with a lambda expression
            var method = newRoot.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            Assert.IsNotNull(method);
            Assert.IsNull(method.Body);
            Assert.IsNull(method.ExpressionBody);

            Assert.IsTrue(TestUtils.IsValidCSharpSyntax(newRoot.SyntaxTree), "Rewritten code is not valid C#: \n" + newRoot.SyntaxTree.ToString());
        }
    }
}
