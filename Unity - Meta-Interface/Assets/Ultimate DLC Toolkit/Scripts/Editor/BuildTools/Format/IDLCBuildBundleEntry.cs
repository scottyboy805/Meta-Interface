using System.IO;

namespace DLCToolkit.BuildTools.Format
{
    internal interface IDLCBuildBundleEntry
    {
        // Methods
        void WriteToStream(Stream stream);
    }
}
