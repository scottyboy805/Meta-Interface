using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLCToolkit
{
    internal sealed class DLCConfig : ScriptableObject
    {
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
        internal bool enabledEditorTestMode = true;
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

        public bool EnableEditorTestMode
        {
            get { return enabledEditorTestMode; }
        }

        public bool ForceRebuild
        {
            get { return forceRebuild; }
        }

        // Constructor
        internal DLCConfig()
        {
            Debug.OnLog += SetLogLevel;
        }

        // Methods
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

        internal void SetLogLevel()
        {
            if (Application.isPlaying == true)
            {
                Debug.logLevel = runtimeLogLevel;
                Debug.logPrefix = "DLC: ";
            }
            else
            {
                Debug.logLevel = buildLogLevel;
                Debug.logPrefix = "DLC Build: ";
            }
        }
    }
}
