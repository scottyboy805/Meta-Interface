using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DLCToolkit.Format
{
    internal class DLCIconSet : IDLCBundleEntry, IDLCIconProvider, IDisposable
    {
        // Type
        protected struct IconHeader
        {
            // Public
            public DLCIconType type;
            public int streamStart;
            public int streamSize;
        }

        // Private
        private IDLCAsyncProvider asyncProvider = null;
        private Stream sourceStream = null;
        private Dictionary<DLCIconType, IconHeader> iconStreams = new Dictionary<DLCIconType, IconHeader>();
        private Dictionary<string, IconHeader> customIconStreams = new Dictionary<string, IconHeader>();
        private Dictionary<object, Texture2D> iconCache = new Dictionary<object, Texture2D>();

        // Constructor
        internal DLCIconSet(IDLCAsyncProvider asyncProvider)
        {
            this.asyncProvider = asyncProvider;
        }

        // Methods
        public void Dispose()
        {
            // Release all cached icons
            foreach(Texture2D icon in iconCache.Values)
            {
                // Destroy the texture to save memory
                if (icon != null && Application.isPlaying == true)
                    GameObject.Destroy(icon);
            }

            // Clear cache
            iconCache.Clear();
        }

        public bool HasIcon(DLCIconType iconType)
        {
            return iconStreams.ContainsKey(iconType);
        }

        public bool HasCustomIcon(string iconKey)
        {
            return customIconStreams.ContainsKey(iconKey);
        }

        public Texture2D LoadIcon(DLCIconType iconType)
        {
            // Check for loaded
            Texture2D cached;
            if (iconCache.TryGetValue(iconType, out cached) == true)
                return cached;

            // Try to get icon header
            IconHeader header;
            if (iconStreams.TryGetValue(iconType, out header) == false)
                return null;

            // Load the icon texture
            return ExtractTextureFromStream(header, iconType);
        }

        public Texture2D LoadCustomIcon(string iconKey)
        {
            // Check for loaded
            Texture2D cached;
            if (iconCache.TryGetValue(iconKey, out cached) == true)
                return cached;

            // Try to get icon header
            IconHeader header;
            if (customIconStreams.TryGetValue(iconKey, out header) == false)
                return null;

            // Load the icon texture
            return ExtractTextureFromStream(header, iconKey);
        }

        public DLCAsync<Texture2D> LoadIconAsync(DLCIconType iconType)
        {
            // Check for loaded
            Texture2D cached;
            if (iconCache.TryGetValue(iconType, out cached) == true)
                return DLCAsync<Texture2D>.Completed(true, cached);

            // Try to get icon header
            IconHeader header;
            if (iconStreams.TryGetValue(iconType, out header) == false)
                return DLCAsync<Texture2D>.Error("DLC does not have the requested icon: " + iconType);

            // Create async
            DLCAsync<Texture2D> async = new DLCAsync<Texture2D>();

            // Read icon
            asyncProvider.RunAsync(ExtractTextureFromStreamAsync(async, header, iconType));

            return async;
        }

        public DLCAsync<Texture2D> LoadCustomIconAsync(string iconKey)
        {
            // Check for loaded
            Texture2D cached;
            if (iconCache.TryGetValue(iconKey, out cached) == true)
                return DLCAsync<Texture2D>.Completed(true, cached);

            // Try to get icon header
            IconHeader header;
            if (customIconStreams.TryGetValue(iconKey, out header) == false)
                return DLCAsync<Texture2D>.Error("DLC does not have the requested icon: " + iconKey);

            // Create async
            DLCAsync<Texture2D> async = new DLCAsync<Texture2D>();

            // Read icon
            asyncProvider.RunAsync(ExtractTextureFromStreamAsync(async, header, iconKey));

            return async;
        }

        private Texture2D ExtractTextureFromStream(IconHeader header, object key)
        {
            // Seek to offset
            Debug.Log("Stream icon data: " + key);
            sourceStream.Seek(header.streamStart, SeekOrigin.Begin);

            // Allocate the read buffer
            byte[] bytes = new byte[header.streamSize];

            // Read into buffer
            sourceStream.Read(bytes);

            // Load the texture data
            Debug.Log("Decoding icon image data...");
            Texture2D icon = new Texture2D(0, 0);
            bool success = ImageConversion.LoadImage(icon, bytes);

            // Check for failed
            if (success == false)
            {
                Debug.LogWarning("Failed to load icon: " + header.type);
            }
            else
            {
                // Cache the icon
                Debug.Log("Icon loaded successfully");
                iconCache[key] = icon;
            }
            return icon;
        }

        private IEnumerator ExtractTextureFromStreamAsync(DLCAsync<Texture2D> async, IconHeader header, object key)
        {
            // Seek to offset
            Debug.Log("Stream icon data async: " + key);
            sourceStream.Seek(header.streamStart, SeekOrigin.Begin);

            // Allocate the read buffer
            byte[] bytes = new byte[header.streamSize];

            // Read into buffer async
            ValueTask<int> task = sourceStream.ReadAsync(bytes);

            // Update status
            async.UpdateStatus("Streaming icon data");

            // Wait for completed
            while(task.IsCompleted == false)
            {
                // Wait a frame
                yield return null;
            }

            // Update progress
            async.UpdateProgress(0.5f);


            // Load the texture data - No way to do this async but should not be a problem for small icons
            Debug.Log("Decoding icon image data...");
            Texture2D icon = new Texture2D(0, 0);
            bool success = ImageConversion.LoadImage(icon, bytes);

            // Cache the icon
            if (success == false)
            {
                Debug.LogWarning("Failed to load icon: " + header.type);
            }
            else
            {
                // Cache the icon
                Debug.Log("Icon loaded successfully");
                iconCache[key] = icon;
            }

            // Complete the operation
            async.Complete(success, icon);
        }

        public void ReadFromStream(Stream stream)
        {
            // Store source stream for later
            this.sourceStream = stream;

            // Create reader - keep stream open for lazy icon loading
            BinaryReader reader = new BinaryReader(stream);

            // Get size
            int buildInSize = reader.ReadUInt16();
            int customSize = reader.ReadUInt16();

            // Read all build in
            for(int i = 0; i < buildInSize; i++)
            {
                // Load the icon header
                ReadIconEntry(reader);
            }

            // Read all custom
            for(int i = 0; i < customSize; i++)
            {
                // Load the custom icon header
                ReadIconEntry(reader);
            }
        }

        public DLCAsync ReadFromStreamAsync(IDLCAsyncProvider asyncProvider, Stream stream)
        {
            // Call through
            ReadFromStream(stream);

            // Complete operation
            return DLCAsync.Completed(true);
        }

        private void ReadIconEntry(BinaryReader reader)
        {
            int iconType = reader.ReadInt16();

            // Check for custom
            if (iconType < 0)
            {
                // Add custom icon type
                customIconStreams.Add(reader.ReadString(), new IconHeader
                {
                    type = (DLCIconType)iconType,
                    streamStart = reader.ReadInt32(),
                    streamSize = reader.ReadInt32(),
                });
            }
            else
            {
                // Add built in icon type
                iconStreams.Add((DLCIconType)iconType, new IconHeader
                {
                    type = (DLCIconType)iconType,
                    streamStart = reader.ReadInt32(),
                    streamSize = reader.ReadInt32(),
                });
            }
        }
    }
}
