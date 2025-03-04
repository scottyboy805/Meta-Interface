
namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestConstructor
    {
        [DataTestMethod]
        [DataRow(
@"public class Test
{
    protected Test() => throw new System.NotImplementedException();
}", @"public class Test
{             
}", DisplayName = "Condition false Directive")]
        public void TestGenerateConstructorMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}
