//using UnityEditor;
//using UnityEditor.Compilation;
//using System.Linq;
//using MetaInterface;

//public class GenerateSource
//{
//    [MenuItem("Tools/Generate Source")]
//    public static void Generate()
//    {
//        Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

//        Assembly asm = assemblies.Where(a => a.outputPath.Contains("TestAPI")).FirstOrDefault();

//        AssemblyDefinition def = new AssemblyDefinition(asm);

//        def.GenerateSourceOverwrite();
//    }
//}
