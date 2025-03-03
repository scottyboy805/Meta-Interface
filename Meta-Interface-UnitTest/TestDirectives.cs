
namespace Meta_Interface_UnitTest
{
    [TestClass]
    public class TestDirectives
    {
        // Condition true directive
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
    private int field = expr;
#endif  
}", DisplayName = "Condition True Directive")]

        // Condition false directive
        [DataRow(
@"public class Test
{
#if Test && Other
#endif
}", @"public class Test
{         
#if Test && Other
    private int field = expr;
#endif  
}", DisplayName = "Condition false Directive")]
        public void TestDirectivesMeta(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }


        [DataTestMethod]
        [DataRow(
@"#define Test
public class Test
{
#if Test
    private void TestPrivateMethod() => throw new System.NotImplementedException();
#endif
}", @"#define Test
public class Test
{         
#if Test
    private void TestPrivateMethod()
    {
        int a = 5 + 4;
    }
#endif  
}", DisplayName = "Condition true method Directive")]
        [DataRow(
@"public class Test
{
#if Test
#endif
}", @"public class Test
{         
#if Test
    private void TestPrivateMethod()
    {
        int a = 5 + 4;
    }
#endif  
}", DisplayName = "Condition false method Directive")]
        [DataRow(
@"#define UNITY_EDITOR
public class Test
{
    // Methods
#if UNITY_EDITOR
    private void OnValidate() => throw new System.NotImplementedException();
#endif
}", @"#define UNITY_EDITOR
public class Test
{         
// Methods
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Update serialize flags
        UpdateSerializeFlags();
    }
#endif
}", DisplayName = "Condition UNITY_EDITOR method Directive")]
        public void TestDirectivesMethod(string expectedSource, string inputSource)
        {
            // Convert to meta
            string patchedSource = TestUtils.CSharpSourceToMeta(inputSource);

            // Check for equal
            Assert.AreEqual(expectedSource, patchedSource);
            Console.WriteLine(patchedSource);
        }
    }
}


