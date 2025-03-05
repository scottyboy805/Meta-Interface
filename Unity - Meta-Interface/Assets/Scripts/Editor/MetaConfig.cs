using System.Collections.Generic;

namespace MetaInterface
{
    public class MetaConfig
    {
        // Private
        private string ignoreMembersWithAttribute = null;
        private readonly List<string> preprocessorDefineSymbols = new List<string>();
        private readonly List<string> suppressWarnings = new List<string>();

        // Public
        public static readonly MetaConfig Default = new MetaConfig(suppressWarnings: new string[]
        {
            //"CS0067",          // Member is never used
            //"CS0414",          // Value is assigned but never used
        });

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

        public IList<string> SuppressWarnings
        {
            get { return suppressWarnings; }
        }

        public IList<string> PreprocessorDefineSymbols
        {
            get { return preprocessorDefineSymbols; }
        }

        // Constructor
        public MetaConfig(string ignoreMembersWithAttribute = null, IEnumerable<string> preprocessorDefineSymbols = null, IEnumerable<string> suppressWarnings = null)
        {
            this.ignoreMembersWithAttribute = ignoreMembersWithAttribute;

            // Add defines
            if(preprocessorDefineSymbols != null)
                this.preprocessorDefineSymbols.AddRange(preprocessorDefineSymbols);

            // Add suppress warnings
            if(suppressWarnings != null)
                this.suppressWarnings.AddRange(suppressWarnings);
        }
    }
}