using System.Collections.Generic;
using System.Reflection;
using UltimateReplay.Storage;

namespace UltimateReplay.ComponentData
{
    /// <summary>
    /// Contains data about a serialized method call.
    /// </summary>
    public struct ReplayMethodData : IReplaySerialize, IReplayTokenSerialize
    {
        // Private
        private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplayMethodData>();

        [ReplayTokenSerialize("ID")]
        private ReplayIdentity behaviourIdentity;
        [ReplayTokenSerialize("Hash")]
        private int methodHash;
        private MethodInfo targetMethod;
        [ReplayTokenSerialize("Arguments")]
        private object[] methodArguments;

        // Properties
        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the replay component that recorded the method call.
        /// </summary>
        public ReplayIdentity BehaviourIdentity
        {
            get { return behaviourIdentity; }
        }

        /// <summary>
        /// The method signature hash for the target replay method.
        /// Used to identify a given method between sessions so long as the signature has not changed.
        /// Signature includes assembly name and version, namespace, declaring type name, method name, return type and parameter types.
        /// </summary>
        public int TargetMethodHash
        {
            get { return methodHash; }
        }

        /// <summary>
        /// The method info for the target recorded method.
        /// </summary>
        public MethodInfo TargetMethod
        {
            get { return targetMethod; }
        }

        /// <summary>
        /// The method argument values that were passed to the method.
        /// Method arguments can only be primitive types such as int.
        /// </summary>
        public object[] MethodArguments
        {
            get { return methodArguments; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="behaviourIdentity">The identity of the behaviour component that recorded the method call</param>
        /// <param name="targetMethod">The target method information</param>
        /// <param name="methodArguments">The argument list for the target method</param>
        public ReplayMethodData(ReplayIdentity behaviourIdentity, MethodInfo targetMethod, params object[] methodArguments)
        {
            this.behaviourIdentity = behaviourIdentity;
            this.methodHash = ReplayMethods.GetReplayMethodHash(targetMethod);
            this.targetMethod = targetMethod;
            this.methodArguments = methodArguments;
        }

        // Methods
        #region TokenSerialize
        IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
        {
            foreach (ReplayToken token in tokens)
            {
                if (token.IsOptional == false || includeOptional == true)
                    yield return token;
            }
        }
        #endregion

        /// <summary>
        /// Serialize the method data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            // Write identity
            state.Write(behaviourIdentity);

            if (targetMethod == null)
                targetMethod = ReplayMethods.GetReplayMethod(methodHash);

            // Write method info
            ReplayMethods.SerializeMethodInfo(targetMethod, state, methodArguments);
        }

        /// <summary>
        /// Deserialize the method data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            // Read identity
            state.ReadSerializable(ref behaviourIdentity);

            // Read method info
            ReplayMethods.DeserializeMethodInfo(state, out targetMethod, out methodArguments);

            // Get method hash if method is resolved
            methodHash = ReplayMethods.GetReplayMethodHash(targetMethod);
        }
    }
}
