using DLCToolkit.Assets;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DLCToolkit.Demo
{
    public class CarGameMenu : MonoBehaviour
    {
        // Public
        public CarGameManager gameManager;

        public Text trackName;
        public Text bestLap;
        public Text difficulty;
        public Text carName;
        public Text carAppearanceName;
        public Slider carSpeed;
        public Slider carHandling;
        public Slider carDrift;

        // Methods
        private void Start()
        {
            // Load the DLC content
            StartCoroutine(LoadDLCAsync());
        }

        private void OnEnable()
        {
            UpdateUI();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) == true)
                StartCoroutine(LoadDLCAsync());
        }
        public void StartRace()
        {
            // Check for game manager
            if(gameManager == null)
            {
                Debug.LogError("Game manager reference not assigned!");
                return;
            }

            // Get selected track
            TrackInfo selectedTrack = gameManager.SelectedTrack;

            // Check for none
            if(selectedTrack == null)
            {
                Debug.LogError("No tracks setup!");
                return;
            }

            // Switch to the track
            SceneManager.LoadScene(selectedTrack.trackSceneName);
        }

        public void UpdateUI()
        {
            // Check for game manager
            if (gameManager == null)
            {
                Debug.LogError("Game manager reference not assigned!");
                return;
            }

            // Get selections
            CarInfo car = gameManager.SelectedCar;
            CarAppearanceInfo carAppearance = gameManager.SelectedCarAppearance;
            TrackInfo track = gameManager.SelectedTrack;


            if (trackName != null) trackName.text = track != null ? track.trackName : "???";
            if(bestLap != null)
            {
                float time = PlayerPrefs.GetFloat(track.trackName, -1f);
                TimeSpan bestRaceTime = TimeSpan.FromSeconds(time);
                bestLap.text = time == -1f ? "Best: --:--:---" : string.Format("Best: {0:00}:{1:00}:{2:00}", bestRaceTime.Minutes, bestRaceTime.Seconds, bestRaceTime.Milliseconds);
            }
            if (difficulty != null) difficulty.text = track != null ? "Difficulty: " + track.difficultyLevel : "Difficulty: ???";
            if(carName != null) carName.text = car != null ? car.carName : "???";
            if(carAppearanceName != null) carAppearanceName.text = carAppearance != null ? carAppearance.appearanceName : "???";
            if (carSpeed != null) carSpeed.value = car != null ? car.carSpeed : 0.5f;
            if (carHandling != null) carHandling.value = car != null ? car.carHandling : 0.5f;
            if (carDrift != null) carDrift.value = car != null ? car.carDrift : 0.5f;
        }

        private IEnumerator LoadDLCAsync()
        {
            Debug.Log("Loading DLC contents...");

            // Get all DLC
            DLCAsync<string[]> listDLCRequest = DLC.FetchLocalDLCUniqueKeys();

            // Wait for completed
            yield return listDLCRequest;

            // Check for error
            if(listDLCRequest.IsSuccessful == false)
            {
                Debug.LogError("Could not list available DLC contents: " + listDLCRequest.Status);
                yield break;
            }

            // Get all DLC
            string[] availableDLCKeys = listDLCRequest.Result;

            // Load all DLC
            foreach (string dlcKey in availableDLCKeys)
            {
                Debug.Log("Attempt to load DLC: " + dlcKey);

                // Load the dlc content
                DLCAsync<DLCContent> async = DLC.LoadDLCAsync(dlcKey);

                // Wait for completed
                yield return async;

                // Check for loaded
                if(async.IsSuccessful == false || async.Result.IsLoaded == false)
                {
                    Debug.LogWarning("Could not load DLC, it may need to be rebuilt: " +  dlcKey);
                    continue;
                }

                // Get the dlc content
                DLCContent dlc = async.Result;

                // Scan for new car contents
                foreach (DLCSharedAsset car in dlc.SharedAssets.EnumerateAllOfType<CarInfo>())
                {
                    // Load the asset
                    CarInfo newCar = car.Load<CarInfo>();

                    // Add the car
                    if (newCar != null)
                    {
                        Debug.Log("Add DLC car: " + newCar.carName);
                        gameManager.AddCar(newCar);
                    }
                }


                // Scan for new car appearance contents
                foreach(DLCSharedAsset carAppearance in dlc.SharedAssets.EnumerateAllOfType<CarAppearanceInfo>())
                {
                    // Load the asset
                    CarAppearanceInfo newCarAppearance = carAppearance.Load<CarAppearanceInfo>();

                    // Add the new car appearance
                    if(newCarAppearance != null)
                    {
                        Debug.Log("Add DLC car appearance: " + newCarAppearance.appearanceName);
                        gameManager.AddCarAppearance(newCarAppearance);
                    }
                }

                // Scan for new tracks
                foreach(DLCSharedAsset track in dlc.SharedAssets.EnumerateAllOfType<TrackInfo>())
                {
                    // Load the asset
                    TrackInfo newTrack = track.Load<TrackInfo>();

                    // Add the new track
                    if(newTrack != null)
                    {
                        Debug.Log("Add DLC track: " + newTrack.trackName);
                        gameManager.AddTrack(newTrack);
                    }
                }
            }
        }
    }
}
