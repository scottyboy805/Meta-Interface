using System;
using UnityEngine;

namespace DLCToolkit.Demo
{
    [Serializable]
    [CreateAssetMenu(menuName = "DLC Toolkit/Example/New Track")]
    public sealed class TrackInfo : ScriptableObject
    {
        // Public
        public string trackName;
        public string trackSceneName;

        public string difficultyLevel = "Normal";
    }
}
