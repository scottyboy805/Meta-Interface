using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DLCToolkit.Format
{
    internal sealed class DLCNameInfo : IDLCNameInfo
    {
        // Private
        private string name = "";
        private string uniqueKey = "";
        private Version version = new Version(0, 0, 0, 0);        

        // Properties
        public string Name
        {
            get { return name; }
        }

        public string UniqueKey
        {
            get { return uniqueKey; }
        }

        public Version Version
        {
            get { return version; }
        }

        // Constructor
        internal DLCNameInfo() { }

        internal DLCNameInfo(string name, string uniqueKey, Version version)
        {
            this.name = name;
            this.uniqueKey = uniqueKey;
            this.version = version;
        }

        // Methods
        public override string ToString()
        {
            return string.Concat(name, ", ", version.ToString());
        }

        internal void WriteBinary(BinaryWriter writer)
        {
            writer.Write(name);
            DLCFormatUtils.WriteString(writer, uniqueKey);
            DLCFormatUtils.WriteVersion(writer, version);
        }

        internal void ReadBinary(BinaryReader reader)
        {
            name = reader.ReadString();
            uniqueKey = DLCFormatUtils.ReadString(reader);
            version = DLCFormatUtils.ReadVersion(reader);
        }
    }

    internal class DLCMetadata : IDLCMetadata, IDLCBundleEntry
    {
        // Private
        private string networkUniqueIdentifierHash = null;

        // Protected
        protected DLCNameInfo nameInfo = new DLCNameInfo();
        protected string guid = "";
        protected string description = "";
        protected string developer = "";
        protected string publisher = "";
        protected Version toolkitVersion = new Version(0, 0);
        protected string unityVersion = "";
        protected DLCContentFlags contentFlags = 0;
        protected DateTime buildTime = DateTime.MinValue;
        protected bool shippedWithGame = false;
        protected DLCCustomMetadata customMetadata = null;

        // Properties
        public IDLCNameInfo NameInfo
        {
            get { return nameInfo; }
        }

        public string Guid
        {
            get { return guid; }
        }

        public string Description
        {
            get { return description; }
        }

        public string Developer
        {
            get { return developer; }
        }

        public string Publisher
        {
            get { return publisher; }
        }

        public Version ToolkitVersion
        {
            get { return toolkitVersion; }
        }

        public string UnityVersion
        {
            get { return unityVersion; }
        }

        public DLCContentFlags ContentFlags
        {
            get { return contentFlags; }
        }

        public DateTime BuildTime
        {
            get { return buildTime; }
        }

        public bool ShippedWithGame
        {
            get { return shippedWithGame; }
        }

        public bool HasCustomMetadata
        {
            get { return customMetadata != null; }
        }

        // Constructor
        internal DLCMetadata() { }

        internal DLCMetadata(DLCNameInfo nameInfo, string guid, string description, string developer, string publisher, Version toolkitVersion, string unityVersion, DLCContentFlags contentFlags, bool shippedWithGame)
        {
            this.nameInfo = nameInfo;
            this.guid = guid;
            this.description = description;
            this.developer = developer;
            this.publisher = publisher;
            this.toolkitVersion = toolkitVersion;
            this.unityVersion = unityVersion;
            this.contentFlags = contentFlags;
            this.buildTime = DateTime.Now;
            this.shippedWithGame = shippedWithGame;
        }

        // Methods
        DLCCustomMetadata IDLCMetadata.GetCustomMetadata()
        {
            return customMetadata;
        }

        T IDLCMetadata.GetCustomMetadata<T>()
        {
            return customMetadata as T;
        }

        string IDLCMetadata.GetNetworkUniqueIdentifier(bool includeBuildStamp)
        {
            // Create unique id
            string id = string.Concat(nameInfo.UniqueKey, ",", nameInfo.Version.ToString());

            // Check for build time stamp
            if(includeBuildStamp == true)
                id = string.Concat(id, ",", buildTime.ToFileTime());

            return id;
        }

        string IDLCMetadata.GetNetworkUniqueIdentifierHash()
        {
            // Check for cached
            if(networkUniqueIdentifierHash != null)
                return networkUniqueIdentifierHash;

            // Get the network unique identifier
            string networkID = ((IDLCMetadata)this).GetNetworkUniqueIdentifier(true);

            // Create algorithm
            SHA256 hashAlgorithm = SHA256.Create();

            // Calculate sha256 hash
            byte[] bytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(networkID));

            // Create builder
            StringBuilder builder = new StringBuilder();

            // Process all bytes
            for(int  i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            // Cache the hash string
            networkUniqueIdentifierHash = builder.ToString();
            return networkUniqueIdentifierHash;
        }

        public void ReadFromStream(Stream stream)
        {
            // Read binary values
            ReadBinaryStream(stream);
        }

        public DLCAsync ReadFromStreamAsync(IDLCAsyncProvider asyncProvider, Stream stream)
        {
            // Read binary values
            ReadBinaryStream(stream);
            return DLCAsync.Completed(true);
        }

        private void ReadBinaryStream(Stream stream)
        {
            // Create binary reader
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read name info
                nameInfo.ReadBinary(reader);

                // Read all values
                guid = reader.ReadString();
                description = reader.ReadString();
                developer = reader.ReadString();
                publisher = reader.ReadString();
                toolkitVersion = DLCFormatUtils.ReadVersion(reader);
                unityVersion = reader.ReadString();
                contentFlags = (DLCContentFlags)reader.ReadUInt32();
                buildTime = DateTime.FromFileTime(reader.ReadInt64());
                shippedWithGame = reader.ReadBoolean();

                // Custom metadata
                bool hasCustomMetadata = reader.ReadBoolean();

                if(hasCustomMetadata == true)
                {
                    // Read type
                    string customMetadataTypeString = DLCFormatUtils.ReadString(reader);

                    // Try to resolve type
                    Type customMetadataType = Type.GetType(customMetadataTypeString, false);

                    // Check for resolved
                    if(customMetadataType != null)
                    {
                        // Create instance
                        customMetadata = ScriptableObject.CreateInstance(customMetadataType) as DLCCustomMetadata;

                        // Load metadata
                        if(customMetadata != null)
                            customMetadata.FromSerializeString(DLCFormatUtils.ReadString(reader));
                    }
                }
            }
        }
    }
}
