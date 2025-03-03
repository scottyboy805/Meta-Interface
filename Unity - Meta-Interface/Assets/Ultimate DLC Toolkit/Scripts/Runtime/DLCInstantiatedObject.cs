
using UnityEngine;

namespace DLCToolkit
{
    internal sealed class DLCInstantiatedObject : MonoBehaviour
    {
        // Private
        private DLCContent associatedContent = null;

        // Methods
        private void Awake()
        {
            // Listen for content unload events
            DLC.OnContentUnloaded.AddListener(OnDLCContentUnloaded);
        }

        private void OnDestroy()
        {
            // Remove listener
            DLC.OnContentWillUnload.RemoveListener(OnDLCContentUnloaded);
        }

        private void OnDLCContentUnloaded(DLCContent content)
        {
            // Check for instance
            if (gameObject.scene.name != null)
                Destroy(gameObject);
        }

        internal static void AssociateDLCObject(DLCContent content, GameObject prefab)
        {
            // Create instance component
            DLCInstantiatedObject instantiated = prefab.AddComponent<DLCInstantiatedObject>();

            // Register content
            instantiated.associatedContent = content;

            // Hide in inspector
            instantiated.hideFlags = HideFlags.HideInInspector;
        }
    }
}
