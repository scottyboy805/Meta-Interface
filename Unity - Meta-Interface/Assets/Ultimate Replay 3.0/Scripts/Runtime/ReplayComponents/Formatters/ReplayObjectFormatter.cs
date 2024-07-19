using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UltimateReplay.ComponentData;

namespace UltimateReplay.Formatters
{
    public sealed class ReplayObjectFormatter : ReplayFormatter
    {
        // Types
        [Flags]
        internal enum ReplayObjectSerializeFlags : byte
        {
            None = 0,
            Prefab = 1 << 1,
            Components = 1 << 2,
            Variables = 1 << 3,
            Events = 1 << 4,
            Methods = 1 << 5,
        }

        // Private
        [ReplayTokenSerialize("Flags")]
        private ReplayObjectSerializeFlags serializeFlags = 0;
        [ReplayTokenSerialize("Prefab ID")]
        private ReplayIdentity prefabIdentity = new ReplayIdentity();


        [ReplayTokenSerialize("Components")]
        private List<ReplayComponentData> componentStates = new List<ReplayComponentData>(4);
        [ReplayTokenSerialize("Variables")]
        private List<ReplayVariableData> variableStates = new List<ReplayVariableData>();
        [ReplayTokenSerialize("Events")]
        private List<ReplayEventData> eventStates = new List<ReplayEventData>();
        [ReplayTokenSerialize("Methods")]
        private List<ReplayMethodData> methodStates = new List<ReplayMethodData>();

        // Properties
        internal ReplayObjectSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
        }

        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the parent prefab if applicable.
        /// </summary>
        public ReplayIdentity PrefabIdentity
        {
            get { return prefabIdentity; }
            set 
            { 
                prefabIdentity = value;

                // Clear bits
                serializeFlags &= ~ReplayObjectSerializeFlags.Prefab;

                // Add flag if id is valid
                if (prefabIdentity.IsValid == true)
                    serializeFlags |= ReplayObjectSerializeFlags.Prefab;
            }
        }

        /// <summary>
        /// A collection of <see cref="ReplayComponentData"/> containing all the necessary persistent data for all observed components.
        /// </summary>
        public IList<ReplayComponentData> ComponentStates
        {
            get { return componentStates; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayVariableData"/> containing all the necessary persistent data for all recorded variables.
        /// </summary>
        public IList<ReplayVariableData> VariableStates
        {
            get { return variableStates; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayEventData"/> containing all the necessary persistent data for all recorded events.
        /// </summary>
        public IList<ReplayEventData> EventStates
        {
            get { return eventStates; }
        }

        /// <summary>
        /// A collection of <see cref="ReplayMethodData"/> containing all the necessary persistent data for all recorded methods.
        /// </summary>
        public IList<ReplayMethodData> MethodStates
        {
            get { return methodStates; }
        }

        // Methods
        public override void OnReplaySerialize(ReplayState state)
        {
            // Get flags
            ReplayObjectSerializeFlags flags = serializeFlags;

            // Write flags
            state.Write((byte)flags);

            // Check for no flags
            if (flags == ReplayObjectSerializeFlags.None)
                return;

            // Prefab identity
            if ((flags & ReplayObjectSerializeFlags.Prefab) != 0)
                state.Write(prefabIdentity);


            // Write variables
            if ((serializeFlags & ReplayObjectSerializeFlags.Variables) != 0)
            {
                // Variable count
                state.Write((ushort)variableStates.Count);

                // Write all variables
                foreach (ReplayVariableData variableItem in variableStates)
                {
                    // Write variable data
                    variableItem.OnReplaySerialize(state);
                }
            }

            // Write events
            if ((serializeFlags & ReplayObjectSerializeFlags.Events) != 0)
            {
                // Event count
                state.Write((ushort)eventStates.Count);

                // Write all events
                foreach (ReplayEventData eventItem in eventStates)
                {
                    // Write event data
                    eventItem.OnReplaySerialize(state);
                }
            }

            // Write methods
            if ((serializeFlags & ReplayObjectSerializeFlags.Methods) != 0)
            {
                // Method count
                state.Write((ushort)methodStates.Count);

                // Write all methods
                foreach (ReplayMethodData methodState in methodStates)
                {
                    // Write method data
                    methodState.OnReplaySerialize(state);
                }
            }


            // Write components
            // NOTE - Component info is now stored last so that we can choose to not read it when simulating missed replay frames
            if ((serializeFlags & ReplayObjectSerializeFlags.Components) != 0)
            {
                // Component count
                state.Write((ushort)componentStates.Count);

                // Estimate 30 bytes per component data
                //int estimatedByteSize = 30 * componentStates.Count;
                //state.EnsureCapacity(estimatedByteSize);

                // Write all components
                foreach (ReplayComponentData componentItem in componentStates)
                {
                    // Write the component data
                    componentItem.OnReplaySerialize(state);
                }
            }
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            OnReplayDeserialize(state, false);
        }

        public void OnReplayDeserialize(ReplayState state, bool simulate)
        {
            // Release old component states
            foreach (ReplayComponentData componentState in componentStates)
                componentState.Dispose();

            // Clear old data
            componentStates.Clear();
            variableStates.Clear();
            eventStates.Clear();
            methodStates.Clear();

            // Read flags
            serializeFlags = (ReplayObjectSerializeFlags)state.ReadByte();

            // Check for no flags
            if (serializeFlags == ReplayObjectSerializeFlags.None)
                return;

            // Read prefab
            if ((serializeFlags & ReplayObjectSerializeFlags.Prefab) != 0)
                prefabIdentity = state.ReadIdentity();


            // Read variables
            if ((serializeFlags & ReplayObjectSerializeFlags.Variables) != 0)
            {
                // Variable count
                ushort count = state.ReadUInt16();

                for (int i = 0; i < count; i++)
                {
                    // Read the variable data
                    ReplayVariableData variableData = new ReplayVariableData();// state.ReadSerializable<ReplayVariableData>();

                    state.ReadSerializable(ref variableData);

                    // Register state
                    variableStates.Add(variableData);
                }
            }

            // Read events
            if ((serializeFlags & ReplayObjectSerializeFlags.Events) != 0)
            {
                // Event count
                ushort count = state.ReadUInt16();

                for (int i = 0; i < count; i++)
                {
                    // Read the event data
                    ReplayEventData eventData = new ReplayEventData();// state.ReadSerializable<ReplayEventData>();

                    state.ReadSerializable(ref eventData);

                    // Register event state
                    eventStates.Add(eventData);
                }
            }

            // Read methods
            if ((serializeFlags & ReplayObjectSerializeFlags.Methods) != 0)
            {
                // Method count
                ushort count = state.ReadUInt16();

                for (int i = 0; i < count; i++)
                {
                    // Read the method data
                    ReplayMethodData methodData = new ReplayMethodData();// state.ReadSerializable<ReplayMethodData>();

                    state.ReadSerializable(ref methodData);

                    // Register method state
                    methodStates.Add(methodData);
                }
            }


            // Read components
            // NOTE - We do not do component deserialize when simulating missed frames - only events, methods and variables
            if (simulate == false && (serializeFlags & ReplayObjectSerializeFlags.Components) != 0)
            {
                // Component count
                ushort count = state.ReadUInt16();

                for (int i = 0; i < count; i++)
                {
                    // Read the component data
                    ReplayComponentData componentData = default;

                    // Call deserialize
                    componentData.OnReplayDeserialize(state);

                    // Register state
                    componentStates.Add(componentData);
                }
            }
        }

        internal void UpdateFromObject(in ReplayIdentity prefabIdentity, IList<ReplayComponentData> components, IList<ReplayVariableData> variables, IList<ReplayEventData> events, IList<ReplayMethodData> methods, ReplayObjectSerializeFlags flags)
        {
            // Store flags
            this.serializeFlags = flags;

            // Prefab
            this.prefabIdentity = prefabIdentity;

            // Clear old
            this.componentStates.Clear();
            this.variableStates.Clear();
            this.eventStates.Clear();
            this.methodStates.Clear();

            // Only add components that have actually recorded data
            for(int i = 0; i < components.Count; i++)
            {
                if (components[i].ComponentStateData.Size > 0)
                    this.componentStates.Add(components[i]);
            }

            // Add all - Use add range for array copy optimization
            //this.componentStates.AddRange(components);
            this.variableStates.AddRange(variables);
            this.eventStates.AddRange(events);
            this.methodStates.AddRange(methods);
        }
    }
}
