using System.IO;

namespace DLCToolkit.Format
{
    internal interface IDLCBundleEntry
    {
        // Methods
        void ReadFromStream(Stream stream);

        DLCAsync ReadFromStreamAsync(IDLCAsyncProvider asyncProvider, Stream stream);
    }
}
