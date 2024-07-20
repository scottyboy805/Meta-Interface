using System;
using UnityEngine;

namespace DLCToolkit.Profile
{
    /// <summary>
    /// Represents a custom icon entry for a DLC profile.
    /// </summary>
    [Serializable]
    public sealed class DLCCustomIcon
    {
        // Private
        [SerializeField]
        private string customKey = "";
        [SerializeField]
        private Texture2D customIcon = null;

        // Properties
        /// <summary>
        /// The unique key for the custom icon.
        /// </summary>
        public string CustomKey
        {
            get { return customKey; }
            internal set { customKey = value; }
        }

        /// <summary>
        /// The assigned texture for the custom icon.
        /// </summary>
        public Texture2D CustomIcon
        {
            get { return customIcon; }
            internal set { customIcon = value; }
        }
    }
}
