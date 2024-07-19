
namespace MetaInterface
{
    public class MetaConfig
    {
        // Private
        private string ignoreMembersWithAttribute = null;

        // Public
        public static readonly MetaConfig Default = new MetaConfig();

        // Properties
        public bool HasIgnoreAttribute
        {
            get { return string.IsNullOrEmpty(ignoreMembersWithAttribute) == false; }
        }

        public string IgnoreMembersWithAttributeName
        {
            get { return ignoreMembersWithAttribute; }
            set { ignoreMembersWithAttribute = value; }
        }
    }
}