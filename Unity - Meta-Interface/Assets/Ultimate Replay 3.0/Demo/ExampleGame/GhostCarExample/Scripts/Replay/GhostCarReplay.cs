using System;
using System.IO;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateReplay.Demo
{
    public class GhostCarReplay : MonoBehaviour
    {
        // Private
        private const string prefsKeyBestLap = "ultimatereplay.ghostcar.bestlap";
        private const string replayFileCurrent = "current.replay";
        private const string replayFileBest = "best.replay";

        private ReplayStorage recordStorage = null;
        private ReplayStorage playbackStorage = null;
        private ReplayRecordOperation recordOp = null;
        private ReplayPlaybackOperation playbackOp = null;

        private bool lapStarted = false;
        private float lapStartTime = 0;
        private float lapBestTime = -1f;

        // Public
        public Text timer;
        public Text bestTime;
        public ReplayObject playerCar;
        public ReplayObject ghostCar;

        // Methods
        [ContextMenu("Reset Best Lap Time")]
        public void ResetBestLapTime()
        {
            PlayerPrefs.SetFloat(prefsKeyBestLap, -1f);
        }

        public void Start()
        {
            // Load best time
            lapBestTime = PlayerPrefs.GetFloat(prefsKeyBestLap, -1f);
        }

        public void OnDestroy()
        {
            // Release record storage
            if (recordStorage != null)
                recordStorage.Dispose();

            // Release playback storage
            if (playbackStorage != null)
                playbackStorage.Dispose();            
        }

        public void Update()
        {
            if (lapStarted == true)
            {
                TimeSpan raceTime = TimeSpan.FromSeconds(Time.time - lapStartTime);
                timer.text = string.Format("{0:00}:{1:00}:{2:00}", raceTime.Minutes, raceTime.Seconds, raceTime.Milliseconds);
            }

            // Update best time
            if(lapBestTime >= 0f)
            {
                TimeSpan bestRaceTime = TimeSpan.FromSeconds(lapBestTime);
                bestTime.text = string.Format("Best: {0:00}:{1:00}:{2:00}", bestRaceTime.Minutes, bestRaceTime.Seconds, bestRaceTime.Milliseconds);
            }
            else
            {
                bestTime.text = "Best: --:--:---";
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // Make sure a car is triggering
            if (other.GetComponentInParent<CarController>() == null)
                return;

            bool betterLap = false;
            float currentLapTime = Time.time - lapStartTime;

            // Check for improved lap time
            if(lapStarted == true && (lapBestTime < 0f || currentLapTime < lapBestTime))
            {
                betterLap = true;
                lapBestTime = currentLapTime;

                // Save best lap
                PlayerPrefs.SetFloat(prefsKeyBestLap, lapBestTime);

                Debug.Log("New best lap: " + bestTime.text);
            }

            lapStartTime = Time.time;

            // Stop replaying ghost
            if(playbackOp != null)
            {
                playbackOp.StopPlayback();
                playbackOp = null;

                if (playbackStorage != null)
                {
                    playbackStorage.Dispose();
                    playbackStorage = null;
                }
            }

            // Stop recording player
            if(recordOp != null)
            {
                recordOp.StopRecording();
                recordOp = null;

                recordStorage.Dispose();
                recordStorage = null;

                // Check for better lap
                if(betterLap == true)
                {
                    if(File.Exists(replayFileBest) == true)
                        File.Delete(replayFileBest);

                    // Save new file
                    File.Move(replayFileCurrent, replayFileBest);
                }
            }

            // Start replaying ghost car if available
            if(lapBestTime >= 0f && File.Exists(replayFileBest) == true)
            {
                // Enable the ghost car
                ghostCar.gameObject.SetActive(true);

                // Clone identities
                ReplayObject.CloneReplayObjectIdentity(playerCar, ghostCar);

                // Load ghost replay
                playbackStorage = ReplayFileStorage.FromFile(replayFileBest);

                // Start replaying - Pass in the ghost replay scene since we only want to replay the ghost car
                playbackOp = ReplayManager.BeginPlayback(playbackStorage, playerCar, ghostCar);

                // Add end playback listener
                playbackOp.OnPlaybackEnd.AddListener(OnGhostPlaybackEnd);
            }

            // Create storage for current player lap
            recordStorage = ReplayFileStorage.FromFile(replayFileCurrent);

            // Start recording - Pass in the player replay scene since we only want to record the player car
            recordOp = ReplayManager.BeginRecording(recordStorage, playerCar);

            // Set lap started flag - player has crossed the line once
            lapStarted = true;
        }

        private void OnGhostPlaybackEnd()
        {
            // Hide ghost car
            ghostCar.gameObject.SetActive(false);

            // Cleanup playback
            playbackStorage.Dispose();
            playbackStorage = null;

            Debug.Log("Ghost car finished");
        }
    }
}