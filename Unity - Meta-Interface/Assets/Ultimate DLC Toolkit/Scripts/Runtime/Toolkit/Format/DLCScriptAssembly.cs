using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DLCToolkit.Format
{
    internal class DLCScriptAssembly : IDLCBundleEntry
    {
        // Type
        [Flags]
        protected internal enum AssemblyFlags : ushort
        {
            DebugSymbols = 1,
        }

        protected internal struct AssemblyHeader
        {
            // Public
            public AssemblyFlags flags;
            public int streamStart;
            public int streamSize;
        }

        protected internal struct AssemblyImage
        {
            // Public
            public AssemblyFlags flags;
            public string assemblyName;
            public byte[] assemblyImage;
            public byte[] symbolsImage;
        }

        // Private
        private Stream sourceStream = null;
        private List<AssemblyHeader> assemblyHeaders = new List<AssemblyHeader>();
        private List<Assembly> assembliesLoaded = new List<Assembly>();

        // Properties
        internal IReadOnlyList<Assembly> AssembliesLoaded
        {
            get { return assembliesLoaded; }
        }

        // Methods
        public void LoadScriptAssemblyImages()
        {
            // Load from stream
            AssemblyImage[] images = ExtractAssemblyImages();

            // Load script assemblies
            LoadScriptAssemblies(images);
        }

        public DLCAsync LoadScriptAssemblyImagesAsync(IDLCAsyncProvider asyncProvider)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(LoadScriptAssemblyImagesAsyncRoutine(async));

            return async;
        }

        private IEnumerator LoadScriptAssemblyImagesAsyncRoutine(DLCAsync async)
        {
            // Load from stream
            async.UpdateStatus("Extracting assembly images");
            Task<AssemblyImage[]> task = ExtractAssemblyImagesAsync();

            // Wait for task to be completed
            while (task.IsCompleted == false)
            {
                // Wait a frame
                yield return null;
            }

            // Check for successful
            if(task.IsCompletedSuccessfully == false)
            {
                async.Error("Failed to extract assembly images from stream");
                yield break;
            }

            // Load assemblies
            async.UpdateStatus("Loading script assemblies");
            bool success = LoadScriptAssemblies(task.Result);

            // Complete operation
            async.Complete(success);
        }

        private bool LoadScriptAssemblies(AssemblyImage[] images)
        {
            // Load all assemblies
            for (int i = 0; i < images.Length; i++)
            {
                try
                {
                    // Check for debug
                    if ((images[i].flags & AssemblyFlags.DebugSymbols) != 0)
                    {
                        // Load with symbols
                        Assembly loaded = Assembly.Load(images[i].assemblyImage, images[i].symbolsImage);
                        Debug.Log("Loaded scripting assembly with symbols: " + images[i].assemblyName + ", Loaded = " + loaded.FullName);

                        // Add to loaded
                        assembliesLoaded.Add(loaded);
                    }
                    else
                    {
                        // Load without symbols
                        Assembly loaded = Assembly.Load(images[i].assemblyImage);
                        Debug.Log("Loaded scripting assembly: " + images[i].assemblyName + ", Loaded = " + loaded.FullName);

                        // Add to loaded
                        assembliesLoaded.Add(loaded);
                    }
                }
                catch (Exception)
                {
                    // Log error and we must break because subsequent assemblies may have a dependency upon this assembly
                    Debug.LogError("Failed to load scripting assembly: " + images[i].assemblyName);
                    Debug.LogError("Scripting may not be available due to load errors!");
                    return false;
                }
            }
            return true;
        }

        internal AssemblyImage[] ExtractAssemblyImages()
        {
            AssemblyImage[] result = new AssemblyImage[assemblyHeaders.Count];

            // Load all images
            for(int i = 0; i < result.Length; i++)
                result[i] = ExtractAssemblyImage(assemblyHeaders[i]);

            return result;
        }

        internal async Task<AssemblyImage[]> ExtractAssemblyImagesAsync()
        {
            AssemblyImage[] result = new AssemblyImage[assemblyHeaders.Count];

            // Load all images async
            for (int i = 0; i < result.Length; i++)
                result[i] = await ExtractAssemblyImageAsync(assemblyHeaders[i]);

            return result;
        }

        internal AssemblyImage ExtractAssemblyImage(AssemblyHeader header)
        {
            // Seek to offset
            sourceStream.Seek(header.streamStart, SeekOrigin.Begin);

            // Create reader
            BinaryReader reader = new BinaryReader(sourceStream);

            // Read name
            string asmName = DLCFormatUtils.ReadString(reader);

            // Read size
            uint imageSize = reader.ReadUInt32();
            uint symbolsSize = ((header.flags & AssemblyFlags.DebugSymbols) != 0) ? reader.ReadUInt32() : 0;

            // Allocate image
            byte[] image = new byte[imageSize];
            byte[] symbols = ((header.flags & AssemblyFlags.DebugSymbols) != 0) ? new byte[symbolsSize] : null;

            // Read all image bytes into buffer
            reader.Read(image, 0, (int)imageSize);
            
            // Read all symbols bytes into buffer
            if((header.flags & AssemblyFlags.DebugSymbols) != 0)
                reader.Read(symbols, 0, (int)symbolsSize);

            // Create result
            return new AssemblyImage
            {
                flags = header.flags,
                assemblyName = asmName,
                assemblyImage = image,
                symbolsImage = symbols,
            };
        }

        internal async Task<AssemblyImage> ExtractAssemblyImageAsync(AssemblyHeader header)
        {
            // Seek to offset
            sourceStream.Seek(header.streamStart, SeekOrigin.Begin);

            // Create reader
            BinaryReader reader = new BinaryReader(sourceStream);

            // Read name
            string asmName = DLCFormatUtils.ReadString(reader);

            // Read size
            uint imageSize = reader.ReadUInt32();
            uint symbolsSize = ((header.flags & AssemblyFlags.DebugSymbols) != 0) ? reader.ReadUInt32() : 0;

            // Allocate image
            byte[] image = new byte[imageSize];
            byte[] symbols = ((header.flags & AssemblyFlags.DebugSymbols) != 0) ? new byte[symbolsSize] : null;

            // Read all image bytes into buffer
            await sourceStream.ReadAsync(image, 0, (int)imageSize);

            // Read all symbols bytes into buffer
            if ((header.flags & AssemblyFlags.DebugSymbols) != 0)
                await sourceStream.ReadAsync(symbols, 0, (int)symbolsSize);

            // Create result
            return new AssemblyImage
            {
                flags = header.flags,
                assemblyName = asmName,
                assemblyImage = image,
                symbolsImage = symbols,
            };
        }

        public void ReadFromStream(Stream stream)
        {
            // Store source stream for later
            this.sourceStream = stream;

            // Create reader
            BinaryReader reader = new BinaryReader(stream);

            // Get size
            int assemblySize = reader.ReadUInt16();

            // Read all assemblies
            for(int i = 0; i < assemblySize; i++)
            {
                // Load the assembly header
                ReadAssemblyHeader(reader);
            }
        }

        public DLCAsync ReadFromStreamAsync(IDLCAsyncProvider asyncProvider, Stream stream)
        {
            ReadFromStream(stream);
            return DLCAsync.Completed(true);
        }

        private void ReadAssemblyHeader(BinaryReader reader)
        {
            assemblyHeaders.Add(new AssemblyHeader
            {
                flags = (AssemblyFlags)reader.ReadUInt16(),
                streamStart = reader.ReadInt32(),
                streamSize = reader.ReadInt32(),
            });
        }
    }
}
