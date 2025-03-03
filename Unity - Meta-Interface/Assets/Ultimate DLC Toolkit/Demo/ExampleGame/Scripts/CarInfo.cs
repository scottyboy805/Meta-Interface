using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLCToolkit.Demo
{
    [Serializable]
    [CreateAssetMenu(menuName = "DLC Toolkit/Example/New Car")]
    public sealed class CarInfo : ScriptableObject
    {
        // Public
        public string carName;
        public CarController carObject;

        public List<CarAppearanceInfo> carAppearances;

        [Range(0f, 1f)]
        public float carSpeed = 0.5f;
        [Range(0f, 1f)]
        public float carHandling = 0.5f;
        [Range(0f, 1f)]
        public float carDrift = 0.5f;

        // Methods
        public CarController SpawnCar(Transform location)
        {
            // Spawn the car
            return Instantiate(carObject, location.position, location.rotation);
        }

        public CarController SpawnNonDrivableCar(Transform location)
        {
            // Create the car as normal
            CarController car = SpawnCar(location);

            // Disable controller and physics
            car.enabled = false;
            car.rb.isKinematic = true;

            return car;
        }
    }
}
