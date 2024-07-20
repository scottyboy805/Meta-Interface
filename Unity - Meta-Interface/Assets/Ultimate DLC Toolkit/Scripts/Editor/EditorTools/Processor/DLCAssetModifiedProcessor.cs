using UnityEditor;
using DLCToolkit.Profile;
using DLCToolkit.BuildTools;
using UnityEngine.Events;

namespace DLCToolkit.EditorTools
{
    public sealed class DLCAssetModifiedProcessor : AssetModificationProcessor
    {
        // Events
        public static readonly UnityEvent OnModifiedAsset = new UnityEvent();

        // Methods
        private static void OnWillCreateAsset(string assetPath)
        {
            CheckForChangedDLCContent(assetPath);
        }

        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            CheckForChangedDLCContent(assetPath);
            return AssetDeleteResult.DidNotDelete;
        }

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            CheckForChangedDLCContent(sourcePath);
            CheckForChangedDLCContent(destinationPath);
            return AssetMoveResult.DidNotMove;
        }

        public static string[] OnWillSaveAssets(string[] assetPaths)
        {
            foreach (string assetPath in assetPaths)
                CheckForChangedDLCContent(assetPath, true);
            return assetPaths;
        }

        private static void CheckForChangedDLCContent(string path, bool saveAssets = false)
        {
            // Find DLC profiles
            DLCProfile[] allProfiles = DLCBuildPipeline.GetAllDLCProfiles(null, true);

            // Check all profiles to see if the path is associated
            foreach(DLCProfile profile in allProfiles)
            {
                // Check for save assets
                if (saveAssets == true && path == profile.DLCProfileAssetPath)
                    continue;

                // Check for associated
                if(profile.IsAssetAssociated(path) == true)
                {
                    // Mark as modified
                    profile.MarkAsModified();
                }
            }

            // Raise event
            OnModifiedAsset.Invoke();
        }
    }
}
