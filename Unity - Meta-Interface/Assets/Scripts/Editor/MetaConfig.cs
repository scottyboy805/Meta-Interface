using System.Collections.Generic;

namespace MetaInterface
{
    public class MetaConfig
    {
        // Private
        private string ignoreMembersWithAttribute = null;
        private readonly List<string> preprocessorDefineSymbols = new List<string>();

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

        public IList<string> PreprocessorDefineSymbols
        {
            get { return preprocessorDefineSymbols; }
        }
    }
}