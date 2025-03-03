using System;
using UnityEngine;

namespace DLCToolkit
{
    /// <summary>
    /// Represents additional user metadata that can be included in DLC content.
    /// </summary>
    [Serializable]
    public abstract class DLCCustomMetadata : ScriptableObject
    {
        // Methods
        internal string ToSerializeString()
        {
            // Get as string
            return JsonUtility.ToJson(this);
        }

        internal void FromSerializeString(string input)
        {
            JsonUtility.FromJsonOverwrite(input, this);
        }
    }
}
