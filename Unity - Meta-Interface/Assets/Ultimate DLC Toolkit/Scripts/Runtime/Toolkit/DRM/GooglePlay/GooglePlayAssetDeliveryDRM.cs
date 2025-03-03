#if DLCTOOLKIT_DRM_GOOGLEPLAY || DLCTOOLKIT_DRM_TEST_GOOGLEPLAY
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;
#if ENABLE_CLOUD_SERVICES_PURCHASING && UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace DLCToolkit.DRM
{
    /// <summary>
    /// A DRM provider to support DLC via the GooglePlay store using Google Play Asset Delivery.
    /// Supports listing DLC unique keys but may not list all DLC content available on the store. Instead only content available at the time of building the game may be available.
    /// Supports ownership verification and auto-install or on demand install by the Play store.
    /// Must be explicitly enabled by defining `DLCTOOLKIT_DRM_GOOGLEPLAY` in the player settings.
    /// </summary>
    public sealed class GooglePlayAssetDeliveryDRM : IDRMProvider
#if ENABLE_CLOUD_SERVICES_PURCHASING && UNITY_PURCHASING
        , IDetailedStoreListener
#endif
    {
        // Private
#if ENABLE_CLOUD_SERVICES_PURCHASING && UNITY_PURCHASING
        private bool iapInitialized = false;
        private string iapError = null;
        private Product[] iapProducts = null;
#endif

        // Constructor
        public GooglePlayAssetDeliveryDRM()
        {
        }

        // Methods
        DLCAsync<string[]> IDRMProvider.GetDLCUniqueKeysAsync(IDLCAsyncProvider asyncProvider)
        {
            // Get count
            string[] uniqueKeys = AndroidAssetPacks.GetCoreUnityAssetPackNames();

            // Log warning
            Debug.LogWarning("All available DLC may not be listed on this platform unless game APK is up to date");

            return DLCAsync<string[]>.Completed(true, uniqueKeys);            
        }

        DLCAsync<DLCStreamProvider> IDRMProvider.GetDLCStreamAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Try to get dlc path
            string path = AndroidAssetPacks.GetAssetPackPath(uniqueKey);

            // Check for null
            if (path != null)
                return DLCAsync<DLCStreamProvider>.Completed(true, DLCStreamProvider.FromFile(path));

            return DLCAsync<DLCStreamProvider>.Error("DLC not found as part of base game: " + uniqueKey);
        }

        DLCAsync IDRMProvider.IsDLCAvailableAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(TrackStateAsync());
            IEnumerator TrackStateAsync()
            {
                // Wait for initialized
                yield return asyncProvider.RunAsync(InitializeManifestAndProducts(async));

                // Check for error
                if (async.IsSuccessful == false)
                    yield break;

#if ENABLE_CLOUD_SERVICES_PURCHASING && UNITY_PURCHASING
                // Get the manifest entry
                DLCManifestEntry manifestEntry = DLC.ManifestAsync.Result.DLCContents.FirstOrDefault(d => d.DLCUniqueKey == uniqueKey);

                // Check for found
                if (manifestEntry != null)
                {
                    // Check if product is IAP linked
                    if (string.IsNullOrEmpty(manifestEntry.DLCIAPName) == false)
                    {
                        // Update status
                        async.UpdateStatus("DLC is linked to a play store IAP! Waiting for Unity.Purchasing to initialize to verify ownership: " + manifestEntry.DLCIAPName);

                        // Check for initialized
                        while (iapInitialized == false)
                            yield return null;

                        // Check for failure to initialize
                        if (iapError != null)
                        {
                            async.Error(iapError);
                            yield break;
                        }


                        // Get the linked product
                        Product linkedProduct = iapProducts.FirstOrDefault(p => p.definition.id == manifestEntry.DLCIAPName);

                        // Check for product found
                        if (linkedProduct != null)
                        {
                            // Check for purchased
                            bool hasPurchased = linkedProduct.hasReceipt == true
                                || string.IsNullOrEmpty(linkedProduct.receipt) == false
                                || string.IsNullOrEmpty(linkedProduct.transactionID) == false;

                            // Check for not owned
                            if (hasPurchased == false)
                            {
                                async.UpdateStatus("DLC content is linked to a play store IAP that has not been purchased by the user: " + manifestEntry.DLCIAPName);
                                async.Complete(false);
                                yield break;
                            }
                        }
                    }
                }
#endif


                // Create request
                GetAssetPackStateAsyncOperation operation = AndroidAssetPacks.GetAssetPackStateAsync(new string[] { uniqueKey });

                // Update status
                async.UpdateStatus("Requesting availability status of DLC content: " + uniqueKey);

                // Wait for completed
                yield return operation;

                // Get the state
                AndroidAssetPackState state = operation.states[0];

                // Check for error
                if (state.error != AndroidAssetPackError.NoError)
                {
                    async.Error(state.error.ToString());
                    yield break;
                }

                // Check status
                async.Complete(state.status == AndroidAssetPackStatus.Completed);
            }
            return async;
        }

        DLCAsync IDRMProvider.RequestInstallDLCAsync(IDLCAsyncProvider asyncProvider, string uniqueKey)
        {
            // Create async
            DLCAsync async = new DLCAsync();

            // Run async
            asyncProvider.RunAsync(TrackInstallAsync());
            IEnumerator TrackInstallAsync()
            {
                // Create request
                DownloadAssetPackAsyncOperation operation = AndroidAssetPacks.DownloadAssetPackAsync(new string[] { uniqueKey });

                // Wait for completed
                while (operation.isDone == false)
                {
                    // Update progress
                    async.UpdateProgress(operation.progress);

                    // Wait a frame
                    yield return null;
                }

                // Complete operation
                async.Complete(Array.Exists(operation.downloadedAssetPacks, k => k == uniqueKey));
            }
            return async;
        }

        void IDRMProvider.RequestUninstallDLC(string uniqueKey)
        {
            // Cancel any downloads
            AndroidAssetPacks.CancelAssetPackDownload(new string[] { uniqueKey });

            // Remove pack
            AndroidAssetPacks.RemoveAssetPack(uniqueKey);
        }

        void IDRMProvider.TrackDLCUsage(string uniqueKey, bool isInUse)
        {
            // Do nothing
        }


        private IEnumerator InitializeManifestAndProducts(DLCAsync async)
        {
            // Wait for manifest
            yield return DLC.ManifestAsync;

            // Check for manifest
            if (DLC.ManifestAsync.IsSuccessful == false)
            {
                async.Error("Could not fetch DLC manifest: " + DLC.ManifestAsync.Status);
                yield break;
            }

#if ENABLE_CLOUD_SERVICES_PURCHASING && UNITY_PURCHASING
            // Check for already initialized
            if(iapInitialized == true || iapError != null)
                yield break;

            // Get manifest - We can only add products that are known about ahead of time
            DLCManifest manifest = DLC.ManifestAsync.Result;

            // Create the product builder
            ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Process all entries
            foreach (DLCManifestEntry entry in manifest.DLCContents)
            {
                // Check for IAP entry
                if (string.IsNullOrEmpty(entry.DLCIAPName) == true)
                    continue;

                // Add the product as non-consumable
                builder.AddProduct(entry.DLCIAPName, ProductType.NonConsumable);
            }

            // Initialize purchasing
            UnityPurchasing.Initialize(this, builder);
#endif
        }

        #region IAP
#if ENABLE_CLOUD_SERVICES_PURCHASING && UNITY_PURCHASING
        void IStoreListener.OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            iapInitialized = true;
            iapError = null;
            iapProducts = controller.products.all;
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error)
        {
            iapInitialized = true;
            iapError = error.ToString();
        }

        void IStoreListener.OnInitializeFailed(InitializationFailureReason error, string message)
        {
            iapInitialized = true;
            iapError = error.ToString() + " : " + message;
        }

        PurchaseProcessingResult IStoreListener.ProcessPurchase(PurchaseEventArgs purchaseEvent)
        {
            return PurchaseProcessingResult.Complete;
        }

        void IStoreListener.OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
        }

        void IDetailedStoreListener.OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
        }
#endif
#endregion
    }
}
#endif