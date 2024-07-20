using DLCToolkit.Profile;

namespace DLCToolkit.EditorTools
{
    internal abstract class DLCWizardPage
    {
        // Private
        private DLCProfile profile = null;

        // Properties
        public abstract string PageName { get; }

        public DLCProfile Profile
        {
            get { return profile; }
        }

        // Constructor
        public DLCWizardPage(DLCProfile profile)
        {
            this.profile = profile;
        }

        // Methods
        public abstract void OnGUI();
    }
}
