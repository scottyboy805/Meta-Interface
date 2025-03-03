using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace DLCToolkit.Format
{
    /// <summary>
    /// Contains Unity asset bundle data stored as part of the DLC bundle.
    /// </summary>
    internal class DLCContentBundle : IDLCBundleEntry
    {
        // Private
        private Stream contentStream = null;

        private AssetBundle contentBundle = null;
        private DLCLoadState loadState = DLCLoadState.NotLoaded;        

        // Properties
        public AssetBundle Bundle
        {
            get { return contentBundle; }
        }

        public bool IsLoaded
        {
            get { return loadState == DLCLoadState.Loaded; }
        }

        public bool IsNotLoaded
        {
            get { return loadState == DLCLoadState.NotLoaded; }
        }

        public bool IsLoadedOrAvailableForLoading
        {
            get { return loadState != DLCLoadState.NotLoaded; }
        }

        // Constructor
        internal DLCContentBundle(Stream contentStream)
        {
            this.contentStream = contentStream;
        }

        // Methods
        public void RequestLoad()
        {
            // Return to start
            contentStream.Seek(0, SeekOrigin.Begin);

            // Request read
            ReadFromStream(contentStream);
        }

        public DLCAsync RequestLoadAsync(IDLCAsyncProvider asyncProvider)
        {
            // Return to start
            contentStream.Seek(0, SeekOrigin.Begin);

            // Request read
            return ReadFromStreamAsync(asyncProvider, contentStream);
        }

        public void Unload(bool withAssets)
        {
            if (loadState == DLCLoadState.Loaded)
            {
                // Unload the content bundle
                contentBundle.Unload(withAssets);

                // Release
                contentBundle = null;
                loadState = DLCLoadState.NotLoaded;
            }            
        }

        public DLCAsync UnloadAsync(IDLCAsyncProvider asyncProvider, bool withAssets)
        {
            // Check for incorrect load state
            if (loadState != DLCLoadState.Loaded)
                return DLCAsync.Completed(true);

            // Create async
            DLCAsync async = new DLCAsync();
            asyncProvider.RunAsync(WaitForUnloadAsync(async, withAssets));

            return async;
        }

        private IEnumerator WaitForUnloadAsync(DLCAsync async, bool withAssets)
        {
            // Unload the content bundle
            AsyncOperation operation = contentBundle.UnloadAsync(withAssets);

            // Update status
            async.UpdateStatus("Unloading asset bundle");

            // Wait for completed
            while(operation.isDone == false)
            {
                // Update progress
                async.UpdateProgress(operation.progress);
                yield return null;
            }

            // Release
            contentBundle = null;
            loadState = DLCLoadState.NotLoaded;

            // Complete the operation
            async.Complete(true);
        }

        public void ReadFromStream(Stream stream)
        {
            // Check for already completed
            if (loadState == DLCLoadState.Loaded || loadState == DLCLoadState.FailedToLoad)
                return;

            // Load the bundle crc
            uint crc = 0;
            //FetchCrc(stream, out crc);

            // Update load state
            loadState = DLCLoadState.Loading;

            // Try to load the bundle
            // IMPORTANT - Asset bundle from stream assumes that stream position is zero, so we must pass a sub-stream with data starting at position 0 to support seek calls
            contentBundle = AssetBundle.LoadFromStream(stream, crc);

            // Check for success
            loadState = contentBundle != null ? DLCLoadState.Loaded : DLCLoadState.FailedToLoad;
        }

        public DLCAsync ReadFromStreamAsync(IDLCAsyncProvider asyncProvider, Stream stream)
        {
            // Check for already loaded
            if (loadState == DLCLoadState.Loaded)
                return DLCAsync.Completed(true);

            // Check for already failed to load
            if (loadState == DLCLoadState.FailedToLoad)
                return DLCAsync.Error("The bundle failed to load on a previous request and will not attempt again");

            // Load the bundle crc
            uint crc = 0;
            //FetchCrc(stream, out crc);

            // Create the async op
            DLCAsync async = new DLCAsync();

            // Submit load request
            asyncProvider.RunAsync(ReadAssetBundleFromStreamAsync(async, stream, crc));

            return async;
        }

        private IEnumerator ReadAssetBundleFromStreamAsync(DLCAsync async, Stream stream, uint crc)
        {
            // Update load state
            loadState = DLCLoadState.Loading;

            // Issue the load request
            AssetBundleCreateRequest request = AssetBundle.LoadFromStreamAsync(stream, crc);

            // Update status
            async.UpdateStatus("Loading content bundle");

            // Wait for completed
            while(request.isDone == false)
            {
                // Update progress
                async.UpdateProgress(request.progress);
#if DEBUG
                Debug.Log("Load progress = " + request.progress);
#endif

                // Wait a frame
                yield return null;
            }

            // Get bundle
            contentBundle = request.assetBundle;

            // Check for success
            loadState = (request.assetBundle != null) ? DLCLoadState.Loaded : DLCLoadState.NotLoaded;

            // Update status
            async.UpdateStatus("Loading complete");

            // Complete operation
            async.Complete(loadState == DLCLoadState.Loaded);
        }

        private void FetchCrc(Stream stream, out uint crc)
        {
            // Create buffer
            byte[] bytes = new byte[sizeof(uint)];

            // Read bytes
            stream.Read(bytes, 0, bytes.Length);

            // Get int
            crc = BitConverter.ToUInt32(bytes, 0);
        }
    }
}
