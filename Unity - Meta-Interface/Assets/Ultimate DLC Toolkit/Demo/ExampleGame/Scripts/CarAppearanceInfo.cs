using System;
using UnityEngine;

namespace DLCToolkit.Demo
{
    [Serializable]
    [CreateAssetMenu(menuName = "DLC Toolkit/Example/New Car Appearance")]
    public sealed class CarAppearanceInfo : ScriptableObject
    {
        // Public
        public string forCarName;
        public string appearanceName;
        public Material appearanceMaterial;

        // Methods
        public void ApplyToCar(CarController car)
        {
            foreach(Renderer renderer in car.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = appearanceMaterial;
            }
        }
    }
}
