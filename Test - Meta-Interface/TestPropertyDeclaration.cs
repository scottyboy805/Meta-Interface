
namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestPropertyDeclaration
    {
        // Auto Property
        [DataTestMethod]
        [DataRow(
@"public sealed class Test
{
    public int Property { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}", @"
public sealed class Test
{
    public int Property{get;set;}
}", DisplayName = "Auto Property")]

        // Auto property assignment
        [DataRow(
@"public sealed class Test
{
    public int Property { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}", @"
public sealed class Test
{
    public int Property{get;set;} = 5
}", DisplayName = "Auto Property Assignment")]

        // Lambda property
        [DataRow(
@"public sealed class Test
{
    public int Property => throw new System.NotImplementedException();
}", @"
public sealed class Test
{
    public int Property => 5;
}", DisplayName = "Lambda Property")]

        // Get property
        [DataRow(
@"public sealed class Test
{
    public int Property { get => throw new System.NotImplementedException(); }
}", @"
public sealed class Test
{
    public int Property { get{ return 5;}}
}", DisplayName = "Get Property")]

        // Set property
        [DataRow(
@"public sealed class Test
{
    public int Property { set => throw new System.NotImplementedException(); }
}", @"
public sealed class Test
{
    public int Property { set{ val = 5;}}
}", DisplayName = "Set Property")]

        // Get Set property
        [DataRow(
@"public sealed class Test
{
    public int Property { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}", @"
public sealed class Test
{
    public int Property { get => 5; set{ val = 5;}}
}", DisplayName = "Get Set Property")]
        public void TestPropertyMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}
