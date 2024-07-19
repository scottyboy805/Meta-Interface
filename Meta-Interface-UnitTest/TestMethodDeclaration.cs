using Microsoft.CodeAnalysis;

namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestMethodDeclaration
    {
        // Empty method
        [DataTestMethod]
        [DataRow(
@"public class Test
{
    public void Method() => throw new System.NotImplementedException();
}", @"
public class Test
{
    public void Method(){}
}", DisplayName = "Empty Method")]

        // Lambda method
        [DataRow(
@"public class Test
{
    public int Method() => throw new System.NotImplementedException();
}", @"
public class Test
{
    public int Method() => 5;
}", DisplayName = "Lambda Method")]

        // Argument method
        [DataRow(
@"public class Test
{
    public void Method(int a, string b, ref float c) => throw new System.NotImplementedException();
}", @"
public class Test
{
    public void Method(int a, string b, ref float c){}
}", DisplayName = "Argument Method")]

        // Return method
        [DataRow(
@"public class Test
{
    public string Method() => throw new System.NotImplementedException();
}", 
@"
public class Test
{
    public string Method(){ return null; }
}", DisplayName = "Return Method")]

        // Random spaced method
        [DataRow(
@"public class Test
{
    public void Method() => throw new System.NotImplementedException();
}", @"
public    class         Test


          {         

    public
 void  Method() {   }   

}", DisplayName = "Random Spaced Method")]

        // Explicit interface method
        [DataRow(
@"public class Test : ITest
{
    void ITest.Method() => throw new System.NotImplementedException();
}", @"
public class Test : ITest
{         
    void ITest      . Method() {   }   
}", DisplayName = "Explicit Interface Method")]
        public void TestMethodMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }


        // Private method
        [DataTestMethod]
        [DataRow(
@"public class Test
{
}",
@"public class Test
{
    private void Method(){}
}", DisplayName = "Private Method")]

        // Internal method
        [DataRow(
@"public class Test
{
}",
@"public class Test
{
    internal void Method(){}
}", DisplayName = "Internal Method")]

        // Protected method
        [DataRow(
@"public class Test
{
    protected void Method() => throw new System.NotImplementedException();
}",
@"public class Test
{
    protected void Method(){}
}", DisplayName = "Protected Method")]

        // Protected internal method
        [DataRow(
@"public class Test
{
    protected internal void Method() => throw new System.NotImplementedException();
}",
@"public class Test
{
    protected internal void Method(){}
}", DisplayName = "Protected Internal Method")]
        public void TestExcludeMethodMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}