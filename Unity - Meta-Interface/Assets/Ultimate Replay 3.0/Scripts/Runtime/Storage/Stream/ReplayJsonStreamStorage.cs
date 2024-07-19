#if ULTIMATEREPLAY_ENABLE_JSON
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UltimateReplay.Formatters;
using UnityEngine;
using UnityEngine.Scripting;
using UltimateReplay.ComponentData;

#if !ULTIMATERPLAY_DISABLE_BSON
using Newtonsoft.Json.Bson;
#endif

namespace UltimateReplay.Storage
{
    internal sealed class ReplayJsonStreamStorage : ReplayStreamStorage
    {
        // Private
        //private static readonly Dictionary<Type, MethodInfo> specialWriteMethods = new Dictionary<Type, MethodInfo>();
        //private static readonly Dictionary<Type, MethodInfo> specialReadMethods = new Dictionary<Type, MethodInfo>();

        private readonly Dictionary<Type, Func<object>> specialReadMethodsQuickCall = new Dictionary<Type, Func<object>>();
        private readonly Dictionary<Type, Action<object>> specialWriteMethodsQuickCall = new Dictionary<Type, Action<object>>();

#if !ULTIMATERPLAY_DISABLE_BSON
        private bool useBson = false;
#endif
        private ReplayStreamSource source = null;
        private StreamWriter streamWriter = null;
        private JsonWriter writer = null;
        private JsonReader reader = null;
        private bool includeOptionalProperties = false;

        // Public
        public const string headerTag = "Replay Header";
        public const string dataTag = "Replay Data";
        public const string segmentTableTag = "Segment Table";
        public const string persistentDataTag = "Persistent Data";
        public const string metadataTag = "Metadata";

        // Properties
        protected override ReplayStreamSource StreamSource
        {
            get { return source; }
        }

        // Constructor
        static ReplayJsonStreamStorage()
        {
            // Check all methods
            //foreach(MethodInfo method in typeof(ReplayJsonStreamStorage).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            //{
            //    // Check for special write
            //    if(method.Name.StartsWith("Special_Write") == true)
            //    {
            //        // Get parameters
            //        ParameterInfo[] param = method.GetParameters();

            //        // Check for single
            //        if(param.Length == 1)
            //            specialWriteMethods[param[0].ParameterType] = method;
            //    }

            //    // Check for special read
            //    else if(method.Name.StartsWith("Special_Read") == true)
            //    {
            //        // Get parameters
            //        ParameterInfo[] param = method.GetParameters();

            //        // Check for none
            //        if(param.Length == 0)
            //            specialReadMethods[method.ReturnType] = method;
            //    }
            //}
        }

#if !ULTIMATERPLAY_DISABLE_BSON
        public ReplayJsonStreamStorage(ReplayStreamSource source, string replayName = null, bool includeOptionalProperties = false, bool useBson = false)
#else
        public ReplayJsonStreamStorage(ReplayStreamSource source, string replayName = null, bool includeOptionalProperties = false)
#endif
            : base(replayName, false)
        {
            this.source = source;
            this.includeOptionalProperties = includeOptionalProperties;

#if !ULTIMATERPLAY_DISABLE_BSON
            this.useBson = useBson;
#endif

            // Process all special read methods
            LoadSpecialReadMethods();
            LoadSpecialWriteMethods();
            //foreach (KeyValuePair<Type, MethodInfo> specialRead in specialReadMethods)
            //    specialReadMethodsQuickCall[specialRead.Key] = specialRead.Value.CreateDelegate(typeof(Func<object>).MakeGenericType(specialRead.Key), this);
        }

        // Methods
        private void LoadSpecialReadMethods()
        {
            specialReadMethodsQuickCall[typeof(ReplayIdentity)] = Special_ReadIdentityValue;
            specialReadMethodsQuickCall[typeof(Vector2)] = Special_ReadVector2Value;
            specialReadMethodsQuickCall[typeof(Vector3)] = Special_ReadVector3Value;
            specialReadMethodsQuickCall[typeof(Quaternion)] = Special_ReadQuaternionValue;
            specialReadMethodsQuickCall[typeof(Color)] = Special_ReadColorValue;
            specialReadMethodsQuickCall[typeof(Color32)] = Special_ReadColor32Value;
        }

        private void LoadSpecialWriteMethods()
        {
            specialWriteMethodsQuickCall[typeof(ReplayIdentity)] = (object o) => Special_WriteIdentityValue((ReplayIdentity)o);
            specialWriteMethodsQuickCall[typeof(Vector2)] = (object v) => Special_WriteVector2Value((Vector2)v);
            specialWriteMethodsQuickCall[typeof(Vector3)] = (object v) => Special_WriteVector3Value((Vector3)v);
            specialWriteMethodsQuickCall[typeof(Quaternion)] = (object q) => Special_WriteQuaternionValue((Quaternion)q);
            specialWriteMethodsQuickCall[typeof(Color)] = (object c) => Special_WriteColorValue((Color)c);
            specialWriteMethodsQuickCall[typeof(Color32)] = (object c) => Special_WriteColor32Value((Color32)c);
        }

        protected override void OnStreamOpenWrite(Stream writeStream)
        {
#if !ULTIMATERPLAY_DISABLE_BSON
            if (useBson == true)
            {
#pragma warning disable 0618
                this.writer = new BsonWriter(writeStream);
#pragma warning restore 0618
            }
            else
#endif
            {
                this.streamWriter = new StreamWriter(writeStream);
                this.writer = new JsonTextWriter(streamWriter);

                // Use indented formatting
                writer.Formatting = Formatting.Indented;
            }            
        }

        protected override void OnStreamOpenRead(Stream readStream)
        {
#if !ULTIMATERPLAY_DISABLE_BSON
            if (useBson == true)
            {
#pragma warning disable 0618
                this.reader = new BsonReader(readStream);
#pragma warning restore 0618
            }
            else
#endif
            {
                this.reader = new JsonTextReader(new StreamReader(readStream));
            }
        }

        protected override void OnStreamCommit(Stream writeStream)
        {
            // Release writer
            if (streamWriter != null)
            {
                streamWriter.Dispose();
                streamWriter = null;
            }
        }

        protected override void OnStreamSeek(Stream stream, long offset)
        {
            // Write any buffered data before seek
            if(streamWriter != null)
                streamWriter.Flush();

            base.OnStreamSeek(stream, offset);
        }

        private object OnCreateInstance(Type type)
        {
            // Check for special types
            if (type == typeof(ReplayState))
            {
                return ReplayState.pool.GetReusable();
            }
            else if (type == typeof(ReplaySnapshot))
            {
                return ReplaySnapshot.pool.GetReusable();
            }
            else if(type == typeof(ReplayComponentData))
            {
                return default(ReplayComponentData);
            }
            else if(type == typeof(ReplaySnapshot.ReplayStateEntry))
            {
                return default(ReplaySnapshot.ReplayStateEntry);
            }
            else if(type == typeof(Vector3))
            {
                return default(Vector3);
            }
            else if(type == typeof(Quaternion))
            {
                return default(Quaternion);
            }

            // Check for list types
            if (type == typeof(List<ReplayComponentData>))
            {
                return new List<ReplayComponentData>();
            }
            else if(type == typeof(List<ReplayVariableData>))
            {
                return new List<ReplayVariableData>();
            }
            else if(type == typeof(List<ReplayEventData>))
            {
                return new List<ReplayEventData>();
            }
            else if(type == typeof(List<ReplayMethodData>))
            {
                return new List<ReplayMethodData>();
            }    

            //Debug.Log(type.ToString());

            // Getting pooled instance or creating default is much much faster than using activator
            //switch(type)
            //{
            //    case Type when type == typeof(ReplayState): return ReplayState.pool.GetReusable(); break;
            //    case Type when type == typeof(ReplaySnapshot): return ReplaySnapshot.pool.GetReusable(); break;
            //    case Type when type == typeof(ReplayComponentData): return default(ReplayComponentData); break;
            //    case Type when type == typeof(ReplayVariableData): return default(ReplayVariableData); break;
            //    case Type when type == typeof(ReplayEventData): return default(ReplayEventData); break;
            //    case Type when type == typeof(ReplayMethodData): return default(ReplayMethodData); break;

            //    case Type when type == typeof(Vector3): return default(Vector3); break;
            //    case Type when type == typeof(Quaternion): return default(Quaternion); break;
            //}

            // Try to create instance
            return Activator.CreateInstance(type, true);
        }

        #region ThreadWrite
        protected override void ThreadWriteReplayHeader(ReplayStreamHeader header)
        {
            // Write file start
            writer.WriteStartObject();

            // Write header entry
            writer.WritePropertyName(headerTag);            
            writer.WriteStartObject();
            {
                // Write header body
                WriteJsonObject((IReplayTokenSerialize)header, false);
            }
            writer.WriteEndObject();

            // Write start of data
            writer.WritePropertyName(dataTag);
            writer.WriteStartArray();

            // Flush so that start segment offset is correct
            writer.Flush();
        }

        protected override void ThreadWriteReplaySegment(ReplaySegment segment)
        {
            writer.WriteStartObject();
            {
                // Write segment data
                WriteJsonObject((IReplayTokenSerialize)segment, false);
            }
            writer.WriteEndObject();

            // Flush so that next segment offset is correct
            writer.Flush();
        }

        protected override void ThreadWriteReplaySegmentTable(ReplaySegmentTable table)
        {
            // End main array
            writer.WriteEndArray();


            // Write segment table
            writer.WritePropertyName(segmentTableTag);
            writer.WriteStartObject();
            {
                // Write segment table
                WriteJsonObject((IReplayTokenSerialize)table, false);
            }
            writer.WriteEndObject();

            // Flush data because we need to calcualte the offset
            writer.Flush();
        }

        protected override void ThreadWriteReplayPersistentData(ReplayPersistentData data)
        {
            // Write persistent data
            writer.WritePropertyName(persistentDataTag);
            writer.WriteStartObject();
            {
                // Write persistent data
                WriteJsonObject((IReplayTokenSerialize)data, false);
            }
            writer.WriteEndObject();

            // Flush data because we need to calcualte the offset
            writer.Flush();
        }

        protected override void ThreadWriteReplayMetadata(ReplayMetadata metadata)
        {
            // Write metadata data
            writer.WritePropertyName(metadataTag);
            writer.WriteStartObject();
            {
                writer.WritePropertyName("TypeName");
                WriteJsonValue(metadata.TypeName, false);

                // Write persistent data
                WriteJsonObject((IReplayTokenSerialize)metadata, false);
            }
            writer.WriteEndObject();

            // End file
            writer.WriteEndObject();

            // Flush data because we need to calculate the offset
            writer.Flush();
        }
        #endregion

        #region ThreadRead
        protected override void ThreadReadReplayHeader(ref ReplayStreamHeader header)
        {
            // Read file start
            ReadJsonToken(JsonToken.StartObject);

            // Read header entry
            ReadJsonToken(JsonToken.PropertyName);
            ReadJsonToken(JsonToken.StartObject);

            // Read header
            ReadJsonObject((IReplayTokenSerialize)header);
        }

        protected override void ThreadReadReplaySegment(ref ReplaySegment segment, int segmentID)
        {
            readStream.Position = 0;

            // Must go back to start of file
            OnStreamOpenRead(readStream);            

            // Read start of root object - important to not skip that as we miss the main data
            ReadJsonToken(JsonToken.StartObject);

            // Skip until
            while (reader.Read() == true)
            {
                // Check for segment table data
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == dataTag)
                    break;

                // Skip other data
                reader.Skip();
            }

            // Read start of array
            ReadJsonToken(JsonToken.StartArray);
            ReadJsonToken(JsonToken.StartObject);

            // Skip until we reach the desired offset into replay data array
            int currentSegment = 1;

            while(currentSegment < segmentID)
            {
                // Skip over segment object
                reader.Skip();
                reader.Read(); // Read end token
                currentSegment++;
            }

            // We are now at the correct location for reading
            ReadJsonObject((IReplayTokenSerialize)segment);
        }

        protected override void ThreadReadReplaySegmentTable(ref ReplaySegmentTable table)
        {
            readStream.Position = 0;

            // Must go back to start of file
            OnStreamOpenRead(readStream);
            
            // Read start of root object - important to not skip that as we miss the main data
            ReadJsonToken(JsonToken.StartObject);

            // Skip until
            while (reader.Read() == true)
            {
                // Check for segment table data
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == segmentTableTag)
                    break;

                // Skip other data
                reader.Skip();
            }

            // Read start of object
            ReadJsonToken(JsonToken.StartObject);

            // Read segment table
            ReadJsonObject((IReplayTokenSerialize)table);
        }

        protected override void ThreadReadReplayPersistentData(ref ReplayPersistentData data)
        {
            readStream.Position = 0;

            // Must go back to start of file
            OnStreamOpenRead(readStream);            

            // Read start of root object - important to not skip that as we miss the main data
            ReadJsonToken(JsonToken.StartObject);

            // Skip until
            while (reader.Read() == true)
            {
                // Check for segment table data
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == persistentDataTag)
                    break;

                // Skip other data
                reader.Skip();
            }

            // Read file start
            ReadJsonToken(JsonToken.StartObject);

            // Read header
            ReadJsonObject((IReplayTokenSerialize)data);
        }

        protected override void ThreadReadReplayMetadata(Type metadataType, ref ReplayMetadata metadata)
        {
            //data = new ReplayPersistentData();
            metadata = (ReplayMetadata)Activator.CreateInstance(metadataType);

            // Must go back to start of file
            OnStreamOpenRead(readStream);
            readStream.Position = 0;

            // Read start of root object - important to not skip that as we miss the main data
            ReadJsonToken(JsonToken.StartObject);

            // Skip until
            while (reader.Read() == true)
            {
                // Check for segment table data
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == metadataTag)
                    break;

                // Skip other data
                reader.Skip();
            }

            // Read file start
            ReadJsonToken(JsonToken.StartObject);

            // Check for version 120 - Feature metadata type name
            if (deserializeVersionContext >= 120)
            {
                // Read type name
                ReadJsonToken(JsonToken.PropertyName);
                ReadJsonToken(JsonToken.String);

                string typeName = ReadJsonAny(reader, typeof(string)) as string;

                if (metadata != null && typeName != null && metadata.TypeName != typeName)
                {
                    try
                    {
                        // Create custom metadata instance
                        metadata = ReplayMetadata.CreateFromType(typeName);
                    }
                    catch { }
                }
            }

            // Read metadata
            ReadJsonObject((IReplayTokenSerialize)metadata);
        }
        #endregion

        #region WriteJson
        private void WriteJsonObject(IReplayTokenSerialize serialize, bool isArrayElement)
        {
            // Write all tokens
            foreach(ReplayToken token in serialize.GetSerializeTokens(includeOptionalProperties))
            {
                // Write name
                if(isArrayElement == false)
                    writer.WritePropertyName(token.Identifier);

                // Write value
                WriteJsonValue(token.FetchValue(serialize), isArrayElement);
            }
        }

        private void WriteJsonValue(object value, bool isArrayElement)
        {
            // Check for null
            if(value == null)
            {
                writer.WriteNull();
                return;
            }

            // Get type
            Type valueType = value.GetType();

            // Check for token serialize
            if (value is IReplayTokenSerialize)
            {
                writer.WriteStartObject();
                {
                    WriteJsonObject((IReplayTokenSerialize)value, false);
                }
                writer.WriteEndObject();
            }
            // Check for token serialize provider
            else if(value is IReplayTokenSerializeProvider)
            {
                WriteJsonValue(((IReplayTokenSerializeProvider)value).SerializeTarget, isArrayElement);   
            }
            // Check for special serializer
            else if (specialWriteMethodsQuickCall.TryGetValue(valueType, out var writeMethod) == true)
            {
                writeMethod(value);
                //writeMethod.Invoke(this, new object[] { value });
            }
            // Check for array
            else if (valueType.IsArray == true || value is IList)
            {
                // Write array block
                writer.WriteStartArray();
                {
                    // Get the array
                    IList arr = value as IList;

                    // Write all elements
                    foreach (object element in arr)
                    {
                        WriteJsonValue(element, true);
                    }
                }
                writer.WriteEndArray();
            }
            // Check for dictionary
            else if (value is IDictionary)
            {
                // Write array block
                writer.WriteStartArray();
                {
                    // Get the dictionary
                    IDictionary dict = value as IDictionary;

                    // Write all elements
                    foreach (DictionaryEntry element in dict)
                    {
                        // Write object wrapper
                        writer.WriteStartObject();
                        {
                            // Key
                            writer.WritePropertyName("Key");
                            WriteJsonValue(element.Key, true);

                            // Value
                            writer.WritePropertyName("Value");
                            WriteJsonValue(element.Value, true);
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();
            }
            else
            {
                try
                {
                    // Write the value direct
                    writer.WriteValue(value);
                }
                catch(JsonWriterException)
                {
                    Debug.LogWarning("Could not serialize type as json. Make sure user types implement 'IReplayTokenSerialize': " + valueType);
                    writer.WriteNull();
                }
            }
        }
        #endregion

        #region SpecialWrite
        [Preserve]
        private void Special_WriteIdentityValue(ReplayIdentity id)
        {
            writer.WriteValue(id.ID);
        }

        [Preserve]
        private void Special_WriteVector2Value(Vector2 vec)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(vec.x);
                writer.WritePropertyName("Y"); writer.WriteValue(vec.y);
            }
            writer.WriteEndObject();
        }

        [Preserve]
        private void Special_WriteVector3Value(Vector3 vec)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(vec.x);
                writer.WritePropertyName("Y"); writer.WriteValue(vec.y);
                writer.WritePropertyName("Z"); writer.WriteValue(vec.z);
            }
            writer.WriteEndObject();
        }

        [Preserve]
        private void Special_WriteQuaternionValue(Quaternion quat)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(quat.x);
                writer.WritePropertyName("Y"); writer.WriteValue(quat.y);
                writer.WritePropertyName("Z"); writer.WriteValue(quat.z);
                writer.WritePropertyName("W"); writer.WriteValue(quat.w);
            }
            writer.WriteEndObject();
        }

        [Preserve]
        private void Special_WriteColorValue(Color color)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(color.r);
                writer.WritePropertyName("Y"); writer.WriteValue(color.g);
                writer.WritePropertyName("Z"); writer.WriteValue(color.b);
                writer.WritePropertyName("W"); writer.WriteValue(color.a);
            }
            writer.WriteEndObject();
        }

        [Preserve]
        private void Special_WriteColor32Value(Color32 color)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(color.r);
                writer.WritePropertyName("Y"); writer.WriteValue(color.g);
                writer.WritePropertyName("Z"); writer.WriteValue(color.b);
                writer.WritePropertyName("W"); writer.WriteValue(color.a);
            }
            writer.WriteEndObject();
        }
        #endregion

        #region SpecialRead
        [Preserve]
        private object Special_ReadIdentityValue()
        {
            return new ReplayIdentity((uint)Convert.ChangeType(reader.Value, typeof(uint)));
        }

        [Preserve]
        private object Special_ReadVector2Value()
        {
            Vector2 result = default;

            if(reader.TokenType == JsonToken.StartObject)
            {
                ReadJsonToken(JsonToken.PropertyName); result.x = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.y = (float)reader.ReadAsDouble();
            }
            ReadJsonToken(JsonToken.EndObject);

            return result;
        }

        [Preserve]
        private object Special_ReadVector3Value()
        {
            Vector3 result = default;

            if (reader.TokenType == JsonToken.StartObject)
            {
                ReadJsonToken(JsonToken.PropertyName); result.x = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.y = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.z = (float)reader.ReadAsDouble();
            }
            ReadJsonToken(JsonToken.EndObject);

            return result;
        }

        [Preserve]
        private object Special_ReadQuaternionValue()
        {
            Quaternion result = default;

            if (reader.TokenType == JsonToken.StartObject)
            {
                ReadJsonToken(JsonToken.PropertyName); result.x = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.y = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.z = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.w = (float)reader.ReadAsDouble();
            }
            ReadJsonToken(JsonToken.EndObject);

            return result;
        }

        [Preserve]
        private object Special_ReadColorValue()
        {
            Color result = default;

            if (reader.TokenType == JsonToken.StartObject)
            {
                ReadJsonToken(JsonToken.PropertyName); result.r = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.g = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.b = (float)reader.ReadAsDouble();
                ReadJsonToken(JsonToken.PropertyName); result.a = (float)reader.ReadAsDouble();
            }
            ReadJsonToken(JsonToken.EndObject);

            return result;
        }

        [Preserve]
        private object Special_ReadColor32Value()
        {
            Color32 result = default;

            if (reader.TokenType == JsonToken.StartObject)
            {
                ReadJsonToken(JsonToken.PropertyName); result.r = (byte)reader.ReadAsInt32();
                ReadJsonToken(JsonToken.PropertyName); result.g = (byte)reader.ReadAsInt32();
                ReadJsonToken(JsonToken.PropertyName); result.b = (byte)reader.ReadAsInt32();
                ReadJsonToken(JsonToken.PropertyName); result.a = (byte)reader.ReadAsInt32();
            }
            ReadJsonToken(JsonToken.EndObject);

            return result;
        }
        #endregion

        private void ReadJsonObject(IReplayTokenSerialize serialize)
        {
            while(reader.Read() == true)
            {
                // Check for end object
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                // Check for property
                if(reader.TokenType == JsonToken.PropertyName)
                {
                    // Get the property name
                    string propertyName = (string)reader.Value;

                    // Get value
                    reader.Read();

                    // Find corresponding property
                    foreach(ReplayToken token in serialize.GetSerializeTokens(includeOptionalProperties))
                    {
                        // Check for matching value
                        if(token.Identifier == propertyName)
                        {
                            // Read value
                            object value = ReadJsonAny(reader, token.ValueType);

                            // Store value
                            token.StoreValue(serialize, value);
                            break;
                        }
                    }
                }
            }
        }

        private object ReadJsonAny(JsonReader reader, Type elementType, object propInstCached = null)
        {
            // Check for special reader
            if (specialReadMethodsQuickCall.TryGetValue(elementType, out var readMethod) == true)
            {
                return readMethod();//.DynamicInvoke();// readMethod();//.Invoke(this, null);
            }

            // Check for special reader
            //if (specialReadMethods.TryGetValue(elementType, out var readMethod) == true)
            //{
            //    return readMethod.Invoke(this, null);
            //}

            // Check for primitive
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    {
                        return null;
                    }

                case JsonToken.Boolean:
                    {
                        return (bool)reader.Value;
                    }

                case JsonToken.String:
                    {
                        return (string)reader.Value;
                    }

                case JsonToken.Integer:
                    {
                        // Check for enum
                        if (elementType.IsEnum == true)
                            return Enum.ToObject(elementType, (long)reader.Value);

                        return Convert.ChangeType((long)reader.Value, elementType);
                    }

                case JsonToken.Float:
                    {
                        return Convert.ChangeType((double)reader.Value, elementType);
                    }

                // Check for object
                case JsonToken.StartObject:
                    {
                        // Create instance
                        object obj = propInstCached;
                        
                        // Use cashed instance where possible to avoid allocation and expensive constructor calls
                        if(obj == null)
                            obj = OnCreateInstance(elementType);

                        // Get provider
                        IReplayTokenSerializeProvider provider = obj as IReplayTokenSerializeProvider;

                        // Check for token serialize provider
                        if (provider != null)
                            obj = provider.SerializeTarget;

                        // Check for token serialize
                        if(obj is IReplayTokenSerialize)
                        {
                            // Get start of object
                            //ReadJsonToken(JsonToken.StartObject);
                            {
                                while (reader.Read() == true)
                                {
                                    // Check for end object
                                    if (reader.TokenType == JsonToken.EndObject)
                                        break;

                                    // Check for property
                                    if (reader.TokenType == JsonToken.PropertyName)
                                    {
                                        // Get the property name
                                        string propertyName = (string)reader.Value;

                                        // Get value
                                        reader.Read();

                                        // Process all tokens
                                        foreach (ReplayToken token in ((IReplayTokenSerialize)obj).GetSerializeTokens(includeOptionalProperties))
                                        {
                                            // Check for matching value
                                            if (token.Identifier == propertyName)
                                            {
                                                Type propType = token.ValueType;
                                                object propInst = null;

                                                // Check for replay formatter
                                                if(typeof(ReplayFormatter).IsAssignableFrom(propType) == true)
                                                {
                                                    // Fetch the formatter
                                                    ReplayFormatter formatter = (ReplayFormatter)token.FetchValue(obj);

                                                    // Get the type
                                                    propType = formatter.GetType();
                                                    propInst = formatter;
                                                }

                                                // Get the value for the token
                                                object value = ReadJsonAny(reader, propType, propInst);

                                                // Assign value
                                                token.StoreValue(obj, value);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            //ReadJsonToken(JsonToken.EndObject);
                        }

                        // Check for provider
                        if(provider != null)
                        {
                            // Update serialize target
                            provider.SerializeTarget = obj as IReplayTokenSerialize;

                            // Get provider instead
                            return provider;
                        }

                        // Get the deserialized object
                        return obj;
                    }

                // Check for array
                case JsonToken.StartArray:
                    {
                        // Get start of array
                        //ReadJsonToken(JsonToken.StartArray);

                        // Check for dictionary
                        if (typeof(IDictionary).IsAssignableFrom(elementType) == true)
                        {
                            // Create instance
                            IDictionary dict = (IDictionary)OnCreateInstance(elementType);

                            // Try to get dict key type
                            Type dictKeyType = dict.GetType().GetGenericArguments()[0];

                            // Try to get dict value type
                            Type dictValueType = dict.GetType().GetGenericArguments()[1];

                            // Read all elements
                            while (reader.Read() == true)
                            {
                                // Check for end array
                                if (reader.TokenType == JsonToken.EndArray)
                                    break;

                                // Read wrapper object
                                //ReadJsonToken(JsonToken.StartObject);
                                if(reader.TokenType == JsonToken.StartObject)
                                {
                                    // Key
                                    ReadJsonToken(JsonToken.PropertyName);
                                    reader.Read();
                                    object keyVal = ReadJsonAny(reader, dictKeyType);

                                    // Value
                                    ReadJsonToken(JsonToken.PropertyName);
                                    reader.Read();
                                    object value = ReadJsonAny(reader, dictValueType);

                                    // Update collection
                                    dict.Add(keyVal, value);
                                }
                                //ReadJsonToken(JsonToken.EndObject);
                            }

                            // Get dictionary result
                            return dict;
                        }
                        // Check for array
                        else if (elementType.IsArray == true || typeof(IList).IsAssignableFrom(elementType) == true)
                        {
                            // Get type of collection - Arrays will be created as List<T> while they are populated and then converted to array using `CopyTo`
                            Type arrType = (elementType.IsArray == true) 
                                ? typeof(List<>).MakeGenericType(elementType.GetElementType()) 
                                : elementType;

                            // Create instance
                            IList arr = (IList)OnCreateInstance(arrType);

                            // Try to get array or list item type
                            Type arrValueType = (elementType.IsArray == true)
                                ? elementType.GetElementType()
                                : elementType.GetGenericArguments()[0];

                            // Read all elements
                            while (reader.Read() == true)
                            {
                                // Check for end array
                                if (reader.TokenType == JsonToken.EndArray)
                                    break;

                                // Read element
                                object element = ReadJsonAny(reader, arrValueType);

                                // Add the item
                                if(element != null)
                                    arr.Add(element);
                            }

                            // Convert to array
                            if (elementType.IsArray == true)
                            {
                                // Create our final array
                                Array arrImpl = Array.CreateInstance(elementType.GetElementType(), arr.Count);

                                // Copy items
                                arr.CopyTo(arrImpl, 0);

                                // Update reference to point to our final array
                                arr = arrImpl;
                            }

                            // Get the array/list result
                            return arr;
                        }

                        break;
                    }
            }

            // Could not deserialize the json value - invalid token type
            throw new NotSupportedException(reader.TokenType.ToString());
        }

        private void ReadJsonToken(JsonToken token)
        {
            if (reader.Read() == false || reader.TokenType != token)
                throw new FormatException("Could not read json token of type: " + token);
        }
    }
}
#endif