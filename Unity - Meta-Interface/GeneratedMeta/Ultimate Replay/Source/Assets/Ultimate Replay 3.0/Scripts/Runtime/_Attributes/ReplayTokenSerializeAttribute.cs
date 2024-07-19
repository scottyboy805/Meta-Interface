/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/_Attributes/ReplayTokenSerializeAttribute.cs
/// </summary>
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
        // Properties
        public string OverrideName
        {
            get
            {
                return overrideName;
            }
        }

        public bool IsOptional
        {
            get
            {
                return isOptional;
            }
        }

        // Constructor
        public ReplayTokenSerializeAttribute(string overrideName = null, bool isOptional = false)
        {
            this.overrideName = overrideName;
            this.isOptional = isOptional;
        }

        // Methods
        public string GetSerializeName(string fallback) => throw new System.NotImplementedException();
    }
}