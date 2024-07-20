using UnityEditor;
using UnityEditor.Compilation;
using System.Linq;
using MetaInterface;

public class GenerateSource
{
    //[MenuItem("Tools/Generate Source")]
    //public static void Generate()
    //{
    //    Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

    //    Assembly asm = assemblies.Where(a => a.outputPath.Contains("TestAPI")).FirstOrDefault();

    //    AssemblyDefinition def = new AssemblyDefinition(asm);

    //    def.GenerateSourceOverwrite();
    //}

    [MenuItem("Tools/Generate Meta Source/From UR3.0 Code Base")]
    public static void GenerateUR3_0()
    {
        Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

        Assembly asm = assemblies.Where(a => a.name == "UltimateReplay").FirstOrDefault();

        MetaConfig config = new MetaConfig
        {
            DiscardTypeComments = true,
            DiscardMemberComments = true,
        };

        AssemblyDefinition def = new AssemblyDefinition(asm);

        def.GenerateSource("GeneratedMeta/Ultimate Replay/Source");
    }

    [MenuItem("Tools/Generate Meta Source/From Ultimate DLC Code Base")]
    public static void GenerateUDLC()
    {
        Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Editor);

        Assembly asm = assemblies.Where(a => a.name == "DLCToolkit").FirstOrDefault();

        MetaConfig config = new MetaConfig
        {
            DiscardTypeComments = true,
            DiscardMemberComments = true,
        };

        AssemblyDefinition def = new AssemblyDefinition(asm);

        def.GenerateSource("GeneratedMeta/DLC Toolkit/Source");
    }
}
