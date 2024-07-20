using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DLCToolkit.Format
{
    internal class DLCBundle : IDisposable
    {
        // Type
        internal enum ContentType : ushort
        {
            Metadata = 1,
            IconSet,
            SharedAssetMetadata,
            SceneAssetMetadata,
            ScriptAssembly,
            SharedAssetBundle,
            SceneAssetBundle,
        }

        [Flags]
        internal enum ContentFlags : ushort
        {
            SharedAssets = 1,
            SceneAssets = 2,
            ScriptAssets = 4,

            Signed = 16,
            SignedWithVersion = 32,

            PreloadSharedBundle = 64,
            PreloadSceneBundle = 128,
        }

        protected struct FileHeader
        {
            // Public
            public static readonly int headerSize = Marshal.SizeOf(typeof(FileHeader));

            public int version;
            public RuntimePlatform platform;
            public ContentFlags flags;
            public int contentSize;
            public int reserved1;
            public int reserved2;
        }

        protected struct ContentHeader
        {
            // Public
            public ContentType type;
            public long streamStart;
            public long streamSize;
        }

        // Private
        private DLCStreamProvider bundleStreamProvider = null;

        // Protected
        protected FileHeader header = default;
        protected Dictionary<ContentType, ContentHeader> contentHeaders = new Dictionary<ContentType, ContentHeader>();

        // Public
        public const int DLCFileVersion = 100;
        public const int DLCFileIdentifier = 'U' |
                                            ('D' << 8) |
                                            ('L' << 16) |
                                            ('C' << 24);
             
        // Properties
        public ContentFlags Flags
        {
            get { return header.flags; }
        }

        public virtual string GuidHashString
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.productGUID.ToString();
#else
                return Application.buildGUID;
#endif
            }
        }

        // Constructor
        public DLCBundle(DLCStreamProvider bundleStreamProvider)
        {
            this.bundleStreamProvider = bundleStreamProvider;
        }

        // Methods
        public void ReadBundleContents(bool readContentHeaders)
        {
            // Create reader
            using (BinaryReader reader = new BinaryReader(bundleStreamProvider.OpenReadStream()))
            {
                Debug.Log("Checking DLC file contents...");

                // Check for DLC bundle format
                int identifier = -1;
                try
                {
                    // Try to read the 4 byte file identifier to confirm we are working with a valid format
                    identifier = reader.ReadInt32();
                }
                catch { }

                // Check for incorrect file format
                if (identifier != DLCFileIdentifier)
                    throw new FormatException("The stream does not contain a valid DLC Toolkit format");

                // Read the header
                header.version = reader.ReadInt32();
                header.platform = (RuntimePlatform)reader.ReadInt32();

                // Check platform
                Debug.Log("Checking DLC platform...");
                if (header.platform != Application.platform)
                {
                    // Check for editor
                    if ((Application.platform == RuntimePlatform.WindowsEditor && header.platform != RuntimePlatform.WindowsPlayer)
                        || (Application.platform == RuntimePlatform.OSXEditor && header.platform != RuntimePlatform.OSXEditor)
                        || (Application.platform == RuntimePlatform.LinuxEditor && header.platform != RuntimePlatform.LinuxPlayer))
                    {
                        Debug.LogWarning("DLC bundle was built for a different platform and may have errors: " + (RuntimePlatform)header.platform);
                    }
                }

                header.flags = (ContentFlags)reader.ReadUInt16();
                header.contentSize = reader.ReadInt32();
                header.reserved1 = reader.ReadInt32();      // unused
                header.reserved2 = reader.ReadInt32();      // unused

                // Check for signed
                if ((header.flags & ContentFlags.Signed) != 0)
                {
                    Debug.Log("DLC content has been signed, checking hashes...");

                    // Read the product and version hashes - SHA256 uses 32 bytes
                    byte[] productBytes = reader.ReadBytes(32);
                    byte[] versionBytes = (header.flags & ContentFlags.SignedWithVersion) != 0 ? reader.ReadBytes(32) : null;

                    // Check if DLC is signed to this game
                    DLC.CheckSignedDLC(productBytes, versionBytes);
                }

                // Check if we should fetch content headers
                if (readContentHeaders == true)
                {
                    // Read the content headers
                    Debug.Log("Fetching DLC content headers...");
                    for (int i = 0; i < header.contentSize; i++)
                    {
                        // Get the content type
                        ContentType contentType = (ContentType)reader.ReadUInt16();

                        contentHeaders[contentType] = new ContentHeader
                        {
                            type = contentType,
                            streamStart = reader.ReadInt64(),
                            streamSize = reader.ReadInt64(),
                        };
                    }
                }
            }
        }

        public bool HasContent(ContentType type)
        {
            return contentHeaders.ContainsKey(type);
        }

        public void RequestLoad(ContentType type, IDLCBundleEntry entry)
        {
            // Try to get the content stream
            Stream stream = GetContentStream(type);

            // Check for no stream
            if (stream == null)
                throw new InvalidOperationException("The bundle does not contain the requested content: " + type);

            // Read data from stream
            entry.ReadFromStream(stream);
        }

        public DLCAsync RequestLoadAsync(IDLCAsyncProvider asyncProvider, ContentType type, IDLCBundleEntry entry)
        {
            // Try to get the content stream
            Stream stream = GetContentStream(type);

            // Check for no stream
            if (stream == null)
                throw new InvalidOperationException("The bundle does not contain the requested content: " + type);

            // Read data from stream
            return entry.ReadFromStreamAsync(asyncProvider, stream);
        }

        public Stream GetContentStream(ContentType type)
        {
            // Try to get the content header
            ContentHeader header;
            if (contentHeaders.TryGetValue(type, out header) == false)
                return null;

            // Get the sub stream
            // Note - the sub stream will seek to the base stream to the starting data offset
            return bundleStreamProvider.OpenReadStream(header.streamStart, header.streamSize);
        }

        public void Dispose()
        {
            bundleStreamProvider.Dispose();
        }
    }
}
