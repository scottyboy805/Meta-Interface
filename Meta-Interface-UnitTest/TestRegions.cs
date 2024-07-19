

namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestRegions
    {
        // Region methods
        [DataTestMethod]
        [DataRow(
@"public class Test
{
    public void MethodA() => throw new System.NotImplementedException();
    public void MethodB() => throw new System.NotImplementedException();
}", @"
public class Test
{
#region Test
    public void MethodA(){}
    public void MethodB(){}
#endregion
}", DisplayName = "Region Methods")]
        public void TestRegionMetadata(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}
