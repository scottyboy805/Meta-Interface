using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UltimateReplay.Storage
{
    public sealed class ReplayPersistentData : IReplayStreamSerialize, IReplayTokenSerialize
    {
        // Private
        private struct PersistentDataByTimestamp : IReplayTokenSerialize
        {
            // Private
            private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<PersistentDataByTimestamp>();

            // Public
            [ReplayTokenSerialize("Time")]
            public float timeStamp;
            [ReplayTokenSerialize("State")]
            public ReplayState persistentData;

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
        }

        // Private
        private static readonly IEnumerable<ReplayToken> tokens = ReplayToken.Tokenize<ReplayPersistentData>();

        [ReplayTokenSerialize("Data Time Stamped")]
        private Dictionary<ReplayIdentity, List<PersistentDataByTimestamp>> persistentDataByTimestamp = null;
        [ReplayTokenSerialize("Data")]
        private Dictionary<ReplayIdentity, ReplayState> persistentData = null;

        // Properties
        public IEnumerable<ReplayIdentity> PersistentIdentitiesByTimestamp
        {
            get
            {
                return persistentDataByTimestamp != null
                    ? persistentDataByTimestamp.Keys
                    : Enumerable.Empty<ReplayIdentity>();
            }
        }

        public IEnumerable<ReplayIdentity> PersistentIdentities
        {
            get 
            { 
                return persistentData != null 
                    ? persistentData.Keys 
                    : Enumerable.Empty<ReplayIdentity>(); 
            }
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

        public bool HasPersistentDataByTimestamp(ReplayIdentity id)
        {
            return persistentDataByTimestamp != null && persistentDataByTimestamp.ContainsKey(id) == true;
        }

        public void StorePersistentDataByTimestamp(ReplayIdentity id, float timestamp, ReplayState state)
        {
            // Check for null
            if (state == null)
                return;

            // Create collection
            if (persistentDataByTimestamp == null)
            {
                persistentDataByTimestamp = new Dictionary<ReplayIdentity, List<PersistentDataByTimestamp>>();
            }

            // Add entry
            if(persistentDataByTimestamp.ContainsKey(id) == false)
                persistentDataByTimestamp.Add(id, new List<PersistentDataByTimestamp>());

            // Add new entry
            persistentDataByTimestamp[id].Add(new PersistentDataByTimestamp
            {
                timeStamp = timestamp,
                persistentData = state,
            });
        }

        public ReplayState FetchPersistentDataByTimestamp(ReplayIdentity id, float timestamp)
        {
            List<PersistentDataByTimestamp> list;

            // Check for key
            if (persistentDataByTimestamp == null || persistentDataByTimestamp.TryGetValue(id, out list) == false)
                return null;

            int index = -1;
            float smallestdifference = float.MaxValue;

            // Check for best matching time stamp
            for(int i = 0; i < list.Count; i++)
            {
                // Get difference amount between timestamps
                float delta = Mathf.Abs(timestamp - list[i].timeStamp);

                // Checkfor smaller difference
                if(delta < smallestdifference)
                {
                    // We have a new smallest time difference so make sure that the new target is updated
                    index = i;
                    smallestdifference = delta;
                }
            }

            // Check for valid index
            if (index != -1)
                return list[index].persistentData;

            // No data found
            return null;
        }

        public bool HasPersistentData(ReplayIdentity id)
        {
            return persistentData != null && persistentData.ContainsKey(id) == true;
        }

        public ReplayState FetchPersistentData(ReplayIdentity id)
        {
            if(persistentData != null)
            {
                // Check for key
                ReplayState state;
                if (persistentData.TryGetValue(id, out state) == true)
                    return state;
            }
            // No data stored
            return null;
        }

        public void StorePersistentData(ReplayIdentity id, ReplayState state)
        {
            // Check for null
            if (state == null)
                return;

            // Create collection
            if (persistentData == null)
            {
                persistentData = new Dictionary<ReplayIdentity, ReplayState>();
            }


            // Store the data - overwrite any already saved
            persistentData[id] = state;
        }

        public bool CopyTo(ReplayPersistentData destination)
        {
            // Check for null
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));          
            
            // Check for no data
            if ((persistentDataByTimestamp == null || persistentDataByTimestamp.Count == 0) && 
                (persistentData == null || persistentData.Count == 0))
                return true;

            // Copy all data
            if (persistentDataByTimestamp != null)
            {
                // Create new storage
                destination.persistentDataByTimestamp = new Dictionary<ReplayIdentity, List<PersistentDataByTimestamp>>();

                foreach (KeyValuePair<ReplayIdentity, List<PersistentDataByTimestamp>> dataByTimestamp in persistentDataByTimestamp)
                {
                    // Copy all states
                    List<PersistentDataByTimestamp> clone = new List<PersistentDataByTimestamp>();

                    for (int i = 0; i < dataByTimestamp.Value.Count; i++)
                    {
                        PersistentDataByTimestamp data = new PersistentDataByTimestamp
                        {
                            timeStamp = dataByTimestamp.Value[i].timeStamp,
                            persistentData = ReplayState.pool.GetReusable(),
                        };

                        // Copy replay state
                        if (dataByTimestamp.Value[i].persistentData.CopyTo(data.persistentData) == false)
                            return false;

                        // Add to list
                        clone.Add(data);
                    }

                    // Add entry
                    destination.persistentDataByTimestamp.Add(dataByTimestamp.Key, clone);
                }
            }

            if (persistentData != null)
            {
                // Create new storage
                destination.persistentData = new Dictionary<ReplayIdentity, ReplayState>();

                foreach (KeyValuePair<ReplayIdentity, ReplayState> data in persistentData)
                {
                    // Get reusable state
                    ReplayState clone = ReplayState.pool.GetReusable();

                    // Copy data
                    if (data.Value.CopyTo(clone) == false)
                        return false;

                    // Add entry
                    destination.persistentData.Add(data.Key, clone);
                }
            }

            return true;
        }

        #region IReplayStreamSerialize
        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Calculate sizes
            int sizeA = persistentDataByTimestamp != null ? persistentDataByTimestamp.Count : 0;
            int sizeB = persistentData != null ? persistentData.Count : 0;

            // Write sizes
            writer.Write((ushort)sizeA);
            writer.Write((ushort)sizeB);

            // Check for null
            if (persistentDataByTimestamp != null)
            {
                // Write all persistent timestamp data
                foreach (KeyValuePair<ReplayIdentity, List<PersistentDataByTimestamp>> item in persistentDataByTimestamp)
                {
                    // Write the identity
                    ((IReplayStreamSerialize)item.Key).OnReplayStreamSerialize(writer);

                    // Calcualte the size
                    int sizeC = item.Value != null ? item.Value.Count : 0;

                    // Write the number of elements
                    writer.Write((ushort)sizeC);

                    // Write all elements
                    for (int i = 0; i < sizeC; i++)
                    {
                        // Write time stamp
                        writer.Write(item.Value[i].timeStamp);

                        // Write data
                        ((IReplayStreamSerialize)item.Value[i].persistentData).OnReplayStreamSerialize(writer);
                    }
                }
            }

            if (persistentData != null)
            {
                // Write all persistent data
                foreach (KeyValuePair<ReplayIdentity, ReplayState> item in persistentData)
                {
                    // Write the identity
                    ((IReplayStreamSerialize)item.Key).OnReplayStreamSerialize(writer);

                    // Write data
                    ((IReplayStreamSerialize)item.Value).OnReplayStreamSerialize(writer);
                }
            }
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            // Read sizes
            int sizeA = reader.ReadUInt16();
            int sizeB = reader.ReadUInt16();

            // Check for size
            if(sizeA > 0)
            {
                // Create data
                persistentDataByTimestamp = new Dictionary<ReplayIdentity, List<PersistentDataByTimestamp>>();

                // Process all
                for(int i = 0; i < sizeA; i++)
                {
                    // Read identity
                    ReplayIdentity identity = new ReplayIdentity();
                    ReplayStreamUtility.StreamDeserialize(ref identity, reader);

                    // Read size
                    int sizeC = reader.ReadUInt16();

                    // Create list
                    List<PersistentDataByTimestamp> list = new List<PersistentDataByTimestamp>(sizeC);

                    // Process all
                    for(int j = 0; j < sizeC; j++)
                    {
                        PersistentDataByTimestamp data = new PersistentDataByTimestamp
                        {
                            timeStamp = reader.ReadSingle(),
                            persistentData = ReplayState.pool.GetReusable(),
                        };

                        // Read data
                        ((IReplayStreamSerialize)data.persistentData).OnReplayStreamDeserialize(reader);

                        // Add to list
                        list.Add(data);
                    }

                    // Add to persistent
                    persistentDataByTimestamp.Add(identity, list);
                }
            }

            if(sizeB > 0)
            {
                // Create data
                persistentData = new Dictionary<ReplayIdentity, ReplayState>();

                // Process all
                for(int i = 0; i < sizeB; i++)
                {
                    // Read identity
                    ReplayIdentity identity = new ReplayIdentity();
                    ReplayStreamUtility.StreamDeserialize(ref identity, reader);

                    // Read state
                    ReplayState state = ReplayState.pool.GetReusable();
                    ((IReplayStreamSerialize)state).OnReplayStreamDeserialize(reader);

                    // Add to persistenr
                    persistentData.Add(identity, state);
                }
            }
        }
        #endregion
    }
}
