﻿
namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestFieldDeclaration
    {
        // Methods
        // Empty field
        [DataTestMethod]
        [DataRow(
@"public sealed class Test
{
    public int field;
}", @"
public sealed class Test
{
    public int field;
}", DisplayName = "Empty Field")]

        // Assigned field
        [DataRow(
@"public sealed class Test
{
    public int field = 123;
}", @"
public sealed class Test
{
    public int field = 123;
}", DisplayName = "Assigned Field")]

        // Const referenced field
        [DataRow(
@"public sealed class Test
{
    public int field;
}", @"
public sealed class Test
{
    const int number = 5;
    public int field = number;
}", DisplayName = "Const Referenced Field")]

        // Method referenced field
        [DataRow(
@"public sealed class Test
{
    public int field;
}", @"
public sealed class Test
{
    static int Method() => 5;
    public int field = Method();
}", DisplayName = "Method Referenced Field")]
        public void TestFieldMetadata(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }


        // Private field
        [DataTestMethod]
        [DataRow(
@"public sealed class Test
{
}",
@"public sealed class Test
{
    private int field;
}", DisplayName = "Private Field")]

        // Internal field
        [DataRow(
@"public sealed class Test
{
}",
@"public sealed class Test
{
    internal string field = 123;
}", DisplayName = "Internal Field")]

        // Protected field
        [DataRow(
@"public sealed class Test
{
    protected bool field = true;
}",
@"public sealed class Test
{
    protected bool field = true;
}", DisplayName = "Protected Field")]

        // Protected internal field
        [DataRow(
@"public sealed class Test
{
    protected internal bool field = true;
}",
@"public sealed class Test
{
    protected internal bool field = true;
}", DisplayName = "Protected Internal Field")]
        public void TestExcludeFieldMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}
