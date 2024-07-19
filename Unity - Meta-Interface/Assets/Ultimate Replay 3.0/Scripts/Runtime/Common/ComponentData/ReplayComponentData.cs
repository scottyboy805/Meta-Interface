using System;
using System.Collections.Generic;
using UltimateReplay.Formatters;
using UltimateReplay.Storage;

namespace UltimateReplay.ComponentData
{
    /// <summary>
    /// Contains all serialized data relating to a specific recorder component.
    /// </summary>
    public struct ReplayComponentData : IReplaySerialize, IReplayTokenSerialize, IDisposable
    {
        // Type
        [Flags]
        private enum ReplayComponentDataFlags : byte
        {
            None = 0,
            /// <summary>
            /// The component provides a formatter id.
            /// </summary>
            FormatterId = 1 << 0,
            /// <summary>
            /// State size is serialized as 1 byte.
            /// </summary>
            StateSize_1 = 1 << 1,
            /// <summary>
            /// State size is serialized as 2 bytes.
            /// </summary>
            StateSize_2 = 1 << 2,
            /// <summary>
            /// State size is serialized as 4 bytes.
            /// </summary>
            StateSize_4 = 1 << 3,
        }

        // Private
        private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplayComponentData>();

        [ReplayTokenSerialize("ID")]
        private ReplayIdentity behaviourIdentity;
        [ReplayTokenSerialize("Formatter ID")]
        private int formatterSerializerID;
        private ReplayState componentStateData;

        // Properties
        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the behaviour script that the data belongs to.
        /// </summary>
        public ReplayIdentity BehaviourIdentity
        {
            get { return behaviourIdentity; }
        }

        /// <summary>
        /// An id value used to identify the corresponding serializer or '-1' if a serializer id could not be generated.
        /// </summary>
        public int ComponentSerializerID
        {
            get { return formatterSerializerID; }
        }

        /// <summary>
        /// The <see cref="ReplayState"/> containing all data that was serialized by the component.
        /// </summary>
        public ReplayState ComponentStateData
        {
            get { return componentStateData; }
        }

        [ReplayTokenSerialize("Data")]
        internal ReplayFormatter FormatterData
        {
            get
            {
                // Get the formatter
                ReplayFormatter formatter = GetFormatter();

                // Deserialize
                if(formatter != null && componentStateData != null)
                {
                    componentStateData.PrepareForRead();
                    formatter.OnReplayDeserialize(componentStateData);
                }

                return formatter;
            }
            set
            {
                // Make sure formatter matches
                if (value.FormatterId != formatterSerializerID)
                    throw new InvalidOperationException("Formatter must match serialized formatter ID");

                // Create state
                if (componentStateData == null)
                    componentStateData = ReplayState.pool.GetReusable();

                // Clear old data
                componentStateData.Clear();

                // Serialize
                value.OnReplaySerialize(componentStateData);
            }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="behaviourIdentity">The identity of the behaviour component</param>
        /// <param name="componentSerializerID">The id of the component serializer</param>
        /// <param name="componentStateData">The data associated with the component</param>
        public ReplayComponentData(ReplayIdentity behaviourIdentity, int componentSerializerID, ReplayState componentStateData)
        {
            this.behaviourIdentity = behaviourIdentity;
            this.formatterSerializerID = componentSerializerID;
            this.componentStateData = componentStateData;
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
        /// Release the component data.
        /// </summary>
        public void Dispose()
        {
            behaviourIdentity = ReplayIdentity.invalid;
            formatterSerializerID = -1;
            componentStateData.Dispose();
            componentStateData = null;
        }

        /// <summary>
        /// Serialize the component data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            ReplayComponentDataFlags flags = ReplayComponentDataFlags.None;

            // Check for serializer
            if (formatterSerializerID != -1) flags |= ReplayComponentDataFlags.FormatterId;

            // Check storage size
            if (componentStateData.Size < byte.MaxValue) flags |= ReplayComponentDataFlags.StateSize_1;
            else if (componentStateData.Size < ushort.MaxValue) flags |= ReplayComponentDataFlags.StateSize_2;
            else flags |= ReplayComponentDataFlags.StateSize_4;

            // Write flags
            state.Write((byte)flags);

            // Write identity of component
            state.Write(behaviourIdentity);

            if ((flags & ReplayComponentDataFlags.FormatterId) != 0)
            {
                state.Write((byte)formatterSerializerID);
            }

            // Write size value
            if ((flags & ReplayComponentDataFlags.StateSize_1) != 0) state.Write((byte)componentStateData.Size);
            else if ((flags & ReplayComponentDataFlags.StateSize_2) != 0) state.Write((ushort)componentStateData.Size);
            else state.Write(componentStateData.Size);

            // Add component state to back
            state.Append(componentStateData);
        }

        /// <summary>
        /// Deserialize the component data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            ReplayComponentDataFlags flags = (ReplayComponentDataFlags)state.ReadByte();

            // Read identity
            behaviourIdentity = state.ReadIdentity();

            if ((flags & ReplayComponentDataFlags.FormatterId) != 0)
            {
                formatterSerializerID = state.ReadByte();
            }

            // Read state size
            int size = 0;

            if ((flags & ReplayComponentDataFlags.StateSize_1) != 0) size = state.ReadByte();
            else if ((flags & ReplayComponentDataFlags.StateSize_2) != 0) size = state.ReadUInt16();
            else size = state.ReadInt32();

            // Create component state data
            componentStateData = ReplayState.pool.GetReusable();

            // Read all bytes
            if(size < ReplayState.sharedDataBuffer.Length)
            {
                // Read into buffer
                state.ReadBytes(ReplayState.sharedDataBuffer, 0, size);

                // Update state
                componentStateData.Write(ReplayState.sharedDataBuffer, 0, size);
            }
            else
            {
                // Must make a large allocation
                byte[] bytes = state.ReadBytes(size);

                // Write to state
                componentStateData.Write(bytes);
            }
        }

        /// <summary>
        /// Try to resolve the type of the corresponding formatter type.
        /// </summary>
        /// <returns>The type of the matching serialize or null if the type could not be resolved</returns>
        public Type ResolveFormatterType()
        {
            // Check for valid serializer id
            if(formatterSerializerID > 0)
            {
                // Get the type from the serializer id
                return ReplayFormatter.GetFormatterType((byte)formatterSerializerID);// ReplayManager.GetFormatter((byte)componentSerializerID).GetType();// ReplaySerializers.GetSerializerTypeFromID(componentSerializerID);
            }

            return null;
        }

        public ReplayFormatter GetFormatter()
        {
            // Check for formatter id
            if (formatterSerializerID > 0)
                return ReplayFormatter.GetFormatter((byte)formatterSerializerID);

            // Formatter not found
            return null;
        }

        public T GetFormatter<T>() where T : ReplayFormatter
        {
            return GetFormatter() as T;
        }

        public ReplayFormatter CreateFormatter()
        {
            // Check for formatter id
            if (formatterSerializerID > 0)
                return ReplayFormatter.CreateFormatter((byte)formatterSerializerID);

            // Formatter not found
            return null;
        }

        public T CreateFormatter<T>() where T : ReplayFormatter
        {
            return CreateFormatter() as T;
        }

        /// <summary>
        /// Deserialize the component data onto the specified component serializer instance.
        /// The specified serialize must be the correct type or have the correct serializer id.
        /// </summary>
        /// <param name="componentSerializer">An <see cref="IReplaySerialize"/> implementation that should be a correct typed serializer</param>
        /// <returns>True if the deserialize was successful or false if not</returns>
        public bool DeserializeComponent(IReplaySerialize componentSerializer)
        {
            // Check for null
            if (componentSerializer == null) throw new ArgumentNullException("componentSerializer");

            // Get the serializer type
            Type deserializerType = ResolveFormatterType();

            // Check for found
            if (deserializerType == null)
                return false;

            // Check for matching types
            if (deserializerType != componentSerializer.GetType())
                return false;

            // Prepare state and deserialize
            componentStateData.PrepareForRead();
            componentSerializer.OnReplayDeserialize(componentStateData);

            // Success
            return true;
        }
    }
}
