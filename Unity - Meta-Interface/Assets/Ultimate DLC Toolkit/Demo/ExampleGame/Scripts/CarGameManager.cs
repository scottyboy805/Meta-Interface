using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLCToolkit.Demo
{
    public sealed class CarGameManager : MonoBehaviour
    {
        // Private
        private static int selectedCar = 0;
        private static int selectedCarAppearance = 0;
        private static int selectedTrack = 0;

        private static List<CarInfo> availableCars = null;
        private static List<TrackInfo> availableTracks = null;

        private CarController spawnedCar = null;
        private bool lapStarted = false;
        private float lapStartTime = 0;
        private float lapBestTime = -1f;

        // Public
        public CarInfo[] cars;
        public TrackInfo[] tracks;
        public CarFollowCam carCamera;
        public Transform spawnPos;
        public bool spawnSelection = false;

        public Text timer;
        public Text bestTime;

        // Properties
        public IReadOnlyList<CarInfo> AvailableCars
        {
            get
            {
                if (availableCars == null)
                {
                    availableCars = new List<CarInfo>(cars);
                    for(int i = 0; i < availableCars.Count; i++)
                    {
                        // Clone car infos
                        availableCars[i] = Instantiate(availableCars[i]);

                        // Clone car appearances
                        for (int j = 0; j < availableCars[i].carAppearances.Count; j++)
                            availableCars[i].carAppearances[j] = Instantiate(availableCars[i].carAppearances[j]);
                    }
                }

                return availableCars;
            }
        }

        public IReadOnlyList<TrackInfo> AvailableTracks
        {
            get
            {
                if (availableTracks == null)
                {
                    availableTracks = new List<TrackInfo>(tracks);
                    for(int i = 0;i < availableTracks.Count;i++)
                    {
                        availableTracks[i] = Instantiate(availableTracks[i]);
                    }
                }

                return availableTracks;
            }
        }

        public CarInfo SelectedCar
        {
            get { return AvailableCars.Count > 0 ? AvailableCars[selectedCar] : null; }
        }

        public CarAppearanceInfo SelectedCarAppearance
        {
            get
            {
                CarInfo selectedCar = SelectedCar;
                return selectedCar != null && selectedCar.carAppearances.Count > 0 ? selectedCar.carAppearances[selectedCarAppearance] : null;
            }
        }

        public TrackInfo SelectedTrack
        {
            get { return AvailableTracks.Count > 0 ? AvailableTracks[selectedTrack] : null; }
        }

        // Methods
#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        private static void OnEnterPlayMode(EnterPlayModeOptions options)
        {
            // Support enter play mode options
            selectedCar = 0;
            selectedCarAppearance = 0;
            selectedTrack = 0;
            availableCars = null;
            availableTracks = null;
        }
#endif

        private void Start()
        {
            // Reset selection if it is invalid
            if (SelectedCar == null)
                selectedCar = 0;

            if (SelectedCarAppearance == null)
                selectedCarAppearance = 0;

            if (SelectedTrack == null)
                selectedTrack = 0;

            // Load best time
            if(SelectedTrack != null)
                lapBestTime = PlayerPrefs.GetFloat(SelectedTrack.trackName, -1f);


            // Spawn the car
            SpawnCar();
        }

        private void Update()
        {
            if (lapStarted == true && timer != null)
            {
                TimeSpan raceTime = TimeSpan.FromSeconds(Time.time - lapStartTime);
                timer.text = string.Format("{0:00}:{1:00}:{2:00}", raceTime.Minutes, raceTime.Seconds, raceTime.Milliseconds);
            }

            // Update best time
            if (bestTime != null)
            {
                if (lapBestTime >= 0f)
                {
                    TimeSpan bestRaceTime = TimeSpan.FromSeconds(lapBestTime);
                    bestTime.text = string.Format("Best: {0:00}:{1:00}:{2:00}", bestRaceTime.Minutes, bestRaceTime.Seconds, bestRaceTime.Milliseconds);
                }
                else
                {
                    bestTime.text = "Best: --:--:---";
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Make sure a car is triggering
            if (other.GetComponentInParent<CarController>() == null)
                return;

            float currentLapTime = Time.time - lapStartTime;

            // Check for improved lap time
            if (lapStarted == true && (lapBestTime < 0f || currentLapTime < lapBestTime))
            {
                lapBestTime = currentLapTime;

                // Save best lap
                if(SelectedTrack != null)
                    PlayerPrefs.SetFloat(SelectedTrack.trackName, lapBestTime);

                Debug.Log("New best lap");
            }

            // Set lap started flag - player has crossed the line once
            lapStarted = true;
            lapStartTime = Time.time;
        }

        [ContextMenu("Reset Best Lap Time")]
        public void ResetBestLapTime()
        {
            if(SelectedTrack != null)
                PlayerPrefs.SetFloat(SelectedTrack.trackName, -1f);
        }

        public void SelectNextCar()
        {
            selectedCar++;
            selectedCarAppearance = 0;

            if (selectedCar >= AvailableCars.Count)
                selectedCar = 0;

            // Show the new car
            SpawnCar();
        }

        public void SelectPreviousCar()
        {
            selectedCar--;
            selectedCarAppearance = 0;

            if (selectedCar < 0)
                selectedCar = AvailableCars.Count - 1;

            // Show the new car
            SpawnCar();
        }

        public void SelectNextCarAppearance()
        {
            selectedCarAppearance++;

            if(SelectedCar == null || selectedCarAppearance >= SelectedCar.carAppearances.Count)
                selectedCarAppearance = 0;

            // Show the new car
            SpawnCar();
        }

        public void SelectPreviousCarAppearance()
        {
            selectedCarAppearance--;

            if (SelectedCar == null)
                selectedCarAppearance = 0;
            else if(selectedCarAppearance < 0)
                selectedCarAppearance = SelectedCar.carAppearances.Count - 1;

            // Show the new car
            SpawnCar();
        }

        public void SelectNextTrack()
        {
            selectedTrack++;

            if(selectedTrack >= AvailableTracks.Count)
                selectedTrack = 0;

            // Load best time
            if (SelectedTrack != null)
                lapBestTime = PlayerPrefs.GetFloat(SelectedTrack.trackName, -1f);
        }

        public void SelectPreviousTrack()
        {
            selectedTrack--;

            if (selectedTrack < 0)
                selectedTrack = AvailableTracks.Count - 1;

            // Load best time
            if (SelectedTrack != null)
                lapBestTime = PlayerPrefs.GetFloat(SelectedTrack.trackName, -1f);
        }


        public void AddCar(CarInfo newCar)
        {
            if (AvailableCars.Contains(newCar) == false)
            {
                newCar.carName += " (DLC)";
                availableCars.Add(newCar);

                foreach (CarAppearanceInfo appearance in newCar.carAppearances)
                    appearance.appearanceName += " (DLC)";
            }
        }

        public void AddCarAppearance(CarAppearanceInfo newCarAppearance)
        {
            // This is only for adding appearances to cars that are part of the base game
            if (string.IsNullOrEmpty(newCarAppearance.forCarName) == true)
                return;

            // Try to find target car
            CarInfo targetCar = AvailableCars.FirstOrDefault(c => c.carName == newCarAppearance.forCarName);

            // Add if found
            if (targetCar != null && targetCar.carAppearances.Contains(newCarAppearance) == false)
            {
                newCarAppearance.appearanceName += " (DLC)";
                targetCar.carAppearances.Add(newCarAppearance);
            }
        }

        public void AddTrack(TrackInfo newTrack)
        {
            if (AvailableTracks.Contains(newTrack) == false)
            {
                newTrack.trackName += " (DLC)";
                availableTracks.Add(newTrack);
            }
        }        

        private void SpawnCar()
        {
            // Destroy old car
            if (spawnedCar != null)
            {
                Destroy(spawnedCar.gameObject);
                spawnedCar = null;
            }

            // Get the car selection
            CarInfo selectedCar = SelectedCar;
            CarAppearanceInfo selectedCarAppearance = SelectedCarAppearance;

            // Check for error
            if (selectedCar == null)
                return;

            // Create new car
            if(spawnSelection == true)
            {
                spawnedCar = selectedCar.SpawnNonDrivableCar(spawnPos);
            }
            else
            {
                spawnedCar = selectedCar.SpawnCar(spawnPos);
            }

            // Apply appearance
            if (selectedCarAppearance != null)
                selectedCarAppearance.ApplyToCar(spawnedCar);


            // Start camera
            if (carCamera != null)
                carCamera.target = spawnedCar.transform;
        }
    }
}
