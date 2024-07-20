using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLCToolkit
{
    internal sealed class DLCConfig : ScriptableObject
    {
        // Type
        [Serializable]
        private class DLCPlatformContent
        {
            // Private
            [SerializeField]
            private RuntimePlatform platform;
            [SerializeField]
            private string uniqueKey;

            // Properties
            public RuntimePlatform Platform
            {
                get { return platform; }
            }

            public string UniqueKey
            {
                get { return uniqueKey; }
            }

            // Constructor
            public DLCPlatformContent(RuntimePlatform platform,  string uniqueKey)
            {
                this.platform = platform;
                this.uniqueKey = uniqueKey;
            }
        }

        // Internal
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        internal DLCLogLevel runtimeLogLevel = DLCLogLevel.Warning;
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        internal DLCLogLevel buildLogLevel = DLCLogLevel.Warning;
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        internal bool enableScripting = true;
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        internal bool scriptingDebug = false;
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        internal bool forceRebuild = false;
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        internal bool clearConsoleOnBuild = true;

        // Private
        [SerializeField]
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        private byte[] productHash = null;
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        [SerializeField]
        private byte[] versionHash = null;
#if !DLCTOOLKIT_DEBUG
        [HideInInspector]
#endif
        [SerializeField]
        private List<DLCPlatformContent> platformContent = new List<DLCPlatformContent>();

        // Internal
        internal const string assetName = "DLC Config";

        // Properties
        public byte[] ProductHash
        {
            get { return productHash; }
        }

        public byte[] VersionHash
        {
            get { return versionHash; }
        }

        public bool EnableScripting
        {
            get { return enableScripting; }
        }

        public bool ScriptingDebug
        {
            get { return scriptingDebug; }
        }

        public bool ForceRebuild
        {
            get { return forceRebuild; }
        }

        // Methods
        public string[] GetPlatformDLCUniqueKeys()
        {
            return GetPlatformDLCUniqueKeys(Application.platform);
        }

        public string[] GetPlatformDLCUniqueKeys(RuntimePlatform platform)
        {
            // Check for editor
            switch(platform)
            {
                case RuntimePlatform.WindowsEditor: platform = RuntimePlatform.WindowsPlayer; break;
                case RuntimePlatform.OSXEditor: platform = RuntimePlatform.OSXPlayer; break;
                case RuntimePlatform.LinuxEditor: platform = RuntimePlatform.LinuxPlayer; break;
            }

            List<string> uniqueKeys = new List<string>();

            // Check all content
            foreach(DLCPlatformContent content in platformContent)
            {
                // Register platform keys
                if(content.Platform == platform)
                    uniqueKeys.Add(content.UniqueKey);
            }

            return uniqueKeys.ToArray();
        }

        internal void UpdateProductHash(byte[] hash)
        {
            this.productHash = hash;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        internal void UpdateVersionHash(byte[] hash)
        {
            this.versionHash = hash;

            // Save asset
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        internal void UpdatePlatformContent(IEnumerable<KeyValuePair<RuntimePlatform, string>> content)
        {
            this.platformContent.Clear();

            // Add all
            foreach(KeyValuePair<RuntimePlatform, string> entry in content)
            {
                this.platformContent.Add(new DLCPlatformContent(entry.Key, entry.Value));
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        internal void SetRuntimeLogLevel()
        {
            if (Application.isPlaying == true)
            {
                Debug.logLevel = runtimeLogLevel;
                Debug.logPrefix = "DLC: ";
            }
        }

        internal void SetBuildLogLevel()
        {
            if(Application.isPlaying == false)
            {
                Debug.logLevel = buildLogLevel;
                Debug.logPrefix = "DLC Build: ";
            }
        }
    }
}
