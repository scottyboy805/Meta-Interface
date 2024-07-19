using UnityEngine.EventSystems;

namespace UltimateReplay.StatePreparation
{
    [ReplayComponentPreparer(typeof(UIBehaviour), 50)]
    internal sealed class UIComponentPreparer : ComponentPreparer<UIBehaviour>
    {
        // Methods
        public override void PrepareForPlayback(UIBehaviour component, ReplayState additionalData)
        {
            // Do nothing - UI components should not be prepared
        }

        public override void PrepareForGameplay(UIBehaviour component, ReplayState additionalData)
        {
            // Do nothing - UI components should not be prepared
        }
    }
}
