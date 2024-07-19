using System;

namespace UltimateReplay
{
    /// <summary>
    /// Attribute used to mark members as serializable using a text format.
    /// The serialized name can be specified via the attribute or the member name will be used if no name is provided.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReplayTokenSerializeAttribute : Attribute
    {
        // Private
        private string overrideName = null;
        private bool isOptional = false;

        // Properties
        public string OverrideName
        {
            get { return overrideName; }
        }

        public bool IsOptional
        {
            get { return isOptional; }
        }

        // Constructor
        public ReplayTokenSerializeAttribute(string overrideName = null, bool isOptional = false)
        {
            this.overrideName = overrideName;
            this.isOptional = isOptional;
        }

        // Methods
        public string GetSerializeName(string fallback)
        {
            if (overrideName != null)
                return overrideName;

            return fallback;
        }
    }
}
