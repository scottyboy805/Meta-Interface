
namespace MetaInterface
{
    public class MetaConfig
    {
        // Private
        private string ignoreMembersWithAttribute = null;

        // Public
        public static readonly MetaConfig Default = new MetaConfig();

        public bool DiscardTypeComments = false;
        public bool DiscardMemberComments = false;

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