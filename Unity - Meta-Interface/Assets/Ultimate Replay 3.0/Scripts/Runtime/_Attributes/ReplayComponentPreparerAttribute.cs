using System;

namespace UltimateReplay
{
    /// <summary>
    /// Use this attribute to register a type as a component preparer.
    /// This attribute only works in conjunction with the <see cref="DefaultReplayPreparer"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ReplayComponentPreparerAttribute : Attribute
    {
        // Public
        public Type componentType;
        public int priority = 100;

        // Constructor
        public ReplayComponentPreparerAttribute(Type componentType, int priority = 100)
        {
            this.componentType = componentType;
            this.priority = priority;
        }
    }
}
