
namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestDirectives
    {
        // Explicit interface method
        [DataTestMethod]
        [DataRow(
@"#define Test
#define Other
public class Test
{
#if Test && Other
    private int field;
#endif
}", @"
#define Test
#define Other
public class Test
{         
#if Test && Other
    private int field;
#endif  
}", DisplayName = "Condition Directive")]
        public void TestDirectivesMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}
