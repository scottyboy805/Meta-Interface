using DLCToolkit.BuildTools.Scripting;
using DLCToolkit.Format;
using System.Collections.Generic;
using System.IO;

namespace DLCToolkit.BuildTools.Format
{
    internal sealed class DLCBuildScriptAssembly : DLCScriptAssembly, IDLCBuildBundleEntry
    {
        // Type
        private struct BuildAssemblyHeader
        {
            public AssemblyHeader data;
            public string assemblyName;
            public byte[] assemblyImage;
            public byte[] symbolsImage;

            // Constructor
            public BuildAssemblyHeader(string assemblyName, byte[] assemblyImage, byte[] symbolsImage = null)
            {
                this.data = new AssemblyHeader { flags = symbolsImage != null ? AssemblyFlags.DebugSymbols : 0 };
                this.assemblyName = assemblyName;
                this.assemblyImage = assemblyImage;
                this.symbolsImage = symbolsImage;
            }
        }

        // Private
        private List<BuildAssemblyHeader> buildAssemblyHeaders = new List<BuildAssemblyHeader>();

        // Constructor
        internal DLCBuildScriptAssembly(DLCBuildScriptCollection scriptAssemblies, bool debugMode)
        {
            // Register assemblies - IMPORTNT: We must add the assemblies in order of dependency so that we can just load in order at runtime
            foreach(ScriptAssemblyCompilation compilation in scriptAssemblies.IncludeOrderedCompilations)
            {
                // Check for debug mode
                bool isDebug = debugMode == true && compilation.HasDebugSymbols == true;

                // Register the compilation in order of dependencies
                buildAssemblyHeaders.Add(new BuildAssemblyHeader
                {
                    data = new AssemblyHeader { flags = (isDebug == true) ? AssemblyFlags.DebugSymbols : 0 },
                    assemblyName = compilation.AssemblyName,
                    assemblyImage = compilation.ReadAssemblyImage(),
                    symbolsImage = (isDebug == true) 
                        ? compilation.ReadSymbolsImage() 
                        : null,
                });
            }
        }

        // Methods
        public void WriteToStream(Stream stream)
        {
            // Store assembly start position
            long assemblyStart = stream.Position;

            // Create writer
            BinaryWriter writer = new BinaryWriter(stream);

            // Write size
            writer.Write((ushort)buildAssemblyHeaders.Count);

            // Remember header stream position
            long headerStart = stream.Position;

            // Write all headers
            for(int i = 0; i < buildAssemblyHeaders.Count; i++)
            {
                WriteAssemblyEntry(writer, buildAssemblyHeaders[i]);
            }

            // Write all raw assembly data
            for(int i = 0; i < buildAssemblyHeaders.Count; i++)
            {
                // Write the raw assembly data
                BuildAssemblyHeader header = buildAssemblyHeaders[i];
                WriteAssemblyRawData(writer, assemblyStart, buildAssemblyHeaders[i].assemblyName, 
                    buildAssemblyHeaders[i].assemblyImage, buildAssemblyHeaders[i].symbolsImage, ref header.data);

                // Update the header info
                buildAssemblyHeaders[i] = header;
            }

            // Get current position
            long assemblyEnd = stream.Position;

            // Return to start
            writer.Flush();
            stream.Seek(headerStart, SeekOrigin.Begin);

            // Overwrite all header data now with correct values
            for(int i = 0; i < buildAssemblyHeaders.Count; i++)
            {
                WriteAssemblyEntry(writer, buildAssemblyHeaders[i]);
            }

            // Return to final position
            stream.Seek(assemblyEnd, SeekOrigin.Begin);
        }

        private void WriteAssemblyEntry(BinaryWriter writer, BuildAssemblyHeader header)
        {
            // Write flags
            writer.Write((ushort)header.data.flags);

            // Write start and size
            writer.Write(header.data.streamStart);
            writer.Write(header.data.streamSize);
        }

        private void WriteAssemblyRawData(BinaryWriter writer, long relativeOffset, string asmName, byte[] assemblyImage, byte[] symbolsImage, ref AssemblyHeader header)
        {
            // Check if debug symbols are included
            bool hasSymbols = symbolsImage != null && (header.flags & AssemblyFlags.DebugSymbols) != 0;

            // Get current position
            long offset = writer.BaseStream.Position;

            // Write name
            DLCFormatUtils.WriteString(writer, asmName);

            // Write sizes
            writer.Write((uint)assemblyImage.Length);

            if (hasSymbols == true)
                writer.Write((uint)symbolsImage.Length);

            // Write data to stream
            writer.Write(assemblyImage, 0, assemblyImage.Length);

            // Check for symbols
            if (hasSymbols == true)
                writer.Write(symbolsImage, 0, symbolsImage.Length);

            // Calculate size
            long size = writer.BaseStream.Position - offset;

            // Update header
            header.streamStart = (int)(offset - relativeOffset);
            header.streamSize = (int)size;
        }
    }
}
