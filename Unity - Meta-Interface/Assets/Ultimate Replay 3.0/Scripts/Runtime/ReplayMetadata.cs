using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateReplay
{
    /// <summary>
    /// Stores all additional non-essential information about a replay.
    /// Can be useful to help display information about the replay such as when it was created, or which Unity scene is required for best playback accuracy.
    /// You can also derive from this class to add additional custom metadata fields that you would like to save.
    /// Note that only primitive types and arrays will be serialized and only reference types that are marked as <see cref="SerializableAttribute"/> will be saved (Serialization follows standard Unity practices but does not support reference types unless marked as serializable).
    /// </summary>
    [Serializable]
    public class ReplayMetadata : IReplayStreamSerialize, IReplayTokenSerialize
    {
        // Private
        private IEnumerable<ReplayToken> tokens = null;

        [SerializeField]
        private string replayName = "Untitled";
        [SerializeField]
        private int sceneId = -1;
        [SerializeField]
        private string sceneName = "";
        [SerializeField]
        private string scenePath = "";

        [SerializeField]
        private string appName = "";
        [SerializeField]
        private string developerName = "";
        [SerializeField]
        private string userName = "";

        // Properties
        /// <summary>
        /// Get serializable type name of this metadata type.
        /// </summary>
        public string TypeName
        {
            get { return GetType().AssemblyQualifiedName; }
        }

        /// <summary>
        /// A name for the replay to help identify it.
        /// </summary>
        public string ReplayName
        {
            get { return replayName; }
            set { replayName = value; }
        }

        /// <summary>
        /// The id of the Unity scene that was active when the replay was recorded.
        /// Use <see cref="UpdateSceneMetadata(Scene)"/> to modify this value.
        /// </summary>
        public int SceneId 
        { 
            get { return sceneId; }
        }

        /// <summary>
        /// The name of the Unity scene that was active when the replay was recorded.
        /// Use <see cref="UpdateSceneMetadata(Scene)"/> to modify this value.
        /// </summary>
        public string SceneName
        {
            get { return sceneName; }
        }

        /// <summary>
        /// The path of the Unity scene that was active when the replay was recorded.
        /// Use <see cref="UpdateSceneMetadata(Scene)"/> to modify this value.
        /// </summary>
        public string ScenePath
        {
            get { return scenePath; }
        }

        /// <summary>
        /// Get the name of the app that created this replay.
        /// By default this will use the value of <see cref="Application.productName"/>.
        /// </summary>
        public string AppName
        {
            get { return appName; }
            set { appName = value; }
        }

        /// <summary>
        /// Get the name of the app developer that created this replay.
        /// By default this will use the value of <see cref="Application.companyName"/>.
        /// </summary>
        public string DeveloperName
        {
            get { return developerName; }
            set { developerName = value; }
        }

        /// <summary>
        /// Get the name of the user that created this replay.
        /// By default this will use the value of <see cref="Environment.UserName"/>.
        /// </summary>
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        public ReplayMetadata() { }

        /// <summary>
        /// Create a new instance with the specified metadata replay name.
        /// </summary>
        /// <param name="replayName">The name of the replay</param>
        public ReplayMetadata(string replayName = null)
        {
            // Check for empty
            if (string.IsNullOrEmpty(replayName) == true)
                replayName = "Untitled";

            this.replayName = replayName;
        }

        // Methods
        #region TokenSerialize
        IEnumerable<ReplayToken> IReplayTokenSerialize.GetSerializeTokens(bool includeOptional)
        {
            if(tokens == null)
                tokens = GetSerializeTokens();

            foreach (ReplayToken token in tokens)
            {
                if (token.IsOptional == false || includeOptional == true)
                    yield return token;
            }
        }
        #endregion

        /// <summary>
        /// Update all metadata from default sources.
        /// Scene information will be updated from <see cref="SceneManager.GetActiveScene"/> and company and product info will be updated based on Unity player settings.
        /// </summary>
        public void UpdateMetadata()
        {
            // Update scene
            UpdateSceneMetadata(SceneManager.GetActiveScene());

            // Update company and product info
            if(this.appName == "" ) this.appName = Application.productName;
            if(this.developerName == "") this.developerName = Application.companyName;
            if(this.userName == "") this.userName = Environment.UserName;
        }

        /// <summary>
        /// Update all metadata related to scene info from the specified scene.
        /// </summary>
        /// <param name="scene">The Unity scene to store metadata for</param>
        public void UpdateSceneMetadata(Scene scene)
        {
            if(this.sceneId == -1) this.sceneId = scene.buildIndex;
            if(this.sceneName == "") this.sceneName = scene.name;
            if(this.scenePath == "") this.scenePath = scene.path;
        }

        /// <summary>
        /// Copy the current metadata to the specified metadata object.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns>True if the copy was successful or false if not</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool CopyTo(ReplayMetadata destination)
        {
            // Check for null
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // Encode json
            string json = JsonUtility.ToJson(this);

            // Map onto target
            JsonUtility.FromJsonOverwrite(json, destination);
            return true;
        }

        private IEnumerable<ReplayToken> GetSerializeTokens()
        {
            // Get type
            Type type = GetType();

            // Process all fields
            foreach(FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                // Check for public or attribute
                if (field.IsPublic == false && field.IsDefined(typeof(SerializeField), false) == false)
                    continue;

                // Check for non-serialized
                if (field.IsDefined(typeof(NonSerializedAttribute), false) == true)
                    continue;

                // Create the token
                yield return ReplayToken.Create(field);
            }

            // Process properties
            IEnumerable<ReplayToken> propertyTokens = ReplayToken.Tokenize(type);

            // Properties are supported via ReplayTokenSerializeAttribute only
            foreach (ReplayToken propertyToken in propertyTokens)
                yield return propertyToken;
        }

        /// <summary>
        /// Create a <see cref="ReplayMetadata"/> instance from the specified type name.
        /// The type name must be a valid <see cref="ReplayMetadata"/> type or derived type.
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns></returns>
        public static ReplayMetadata CreateFromType(string typeName)
        {
            // Try to find type
            Type type = Type.GetType(typeName, false);

            // Check for error
            if (type == null)
                return null;

            // Create instance
            return Activator.CreateInstance(type) as ReplayMetadata;
        }

        #region StreamSerialize
        void IReplayStreamSerialize.OnReplayStreamSerialize(BinaryWriter writer)
        {
            // Convert to json
            string json = JsonUtility.ToJson(this, false);

            // Encode to bytes
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            for (int i = 0; i < bytes.Length; i++)
                bytes[i]++;

            // Write size
            writer.Write((ushort)bytes.Length);

            // Write all bytes
            writer.Write(bytes);
        }

        void IReplayStreamSerialize.OnReplayStreamDeserialize(BinaryReader reader)
        {
            // Get size
            int size = reader.ReadUInt16();

            // Read all bytes
            byte[] bytes = reader.ReadBytes(size);

            for (int i = 0; i < bytes.Length; i++)
                bytes[i]--;

            // Get json
            string json = Encoding.UTF8.GetString(bytes);

            // Deserialize
            JsonUtility.FromJsonOverwrite(json, this);
        }
        #endregion
    }
}
