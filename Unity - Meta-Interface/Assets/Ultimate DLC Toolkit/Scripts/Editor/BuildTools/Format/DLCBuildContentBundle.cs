using DLCToolkit.Format;
using System;
using System.IO;

namespace DLCToolkit.BuildTools.Format
{
    internal class DLCBuildContentBundle : DLCContentBundle, IDLCBuildBundleEntry
    {
        // Private
        private string bundlePath = null;
        private uint crc = 0;

        // Constructor
        internal DLCBuildContentBundle(string bundlePath, uint crc)
            : base(new MemoryStream())
        {
            this.bundlePath = bundlePath;
            this.crc = crc;
        }

        // Methods
        public void WriteToStream(Stream stream)
        {
            // Write the bundle crc
            //WriteCrc(stream, crc);

            // Copy bundle contents
            using(Stream bundleStream = File.OpenRead(bundlePath))
            {
                // Copy into our stream
                bundleStream.CopyTo(stream);
            }
        }

        private void WriteCrc(Stream stream, uint crc)
        {
            // Get bytes
            byte[] bytes = BitConverter.GetBytes(crc);

            // Write to stream
            stream.Write(bytes);
        }
    }
}
