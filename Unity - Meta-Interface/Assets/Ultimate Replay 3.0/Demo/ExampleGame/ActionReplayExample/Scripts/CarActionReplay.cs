using System;
using UltimateReplay.StatePreparation;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UltimateReplay.Demo
{
    public class CarActionReplay : MonoBehaviour
    {
        // Type
        /// <summary>
        /// Custom preparer that does not do any preparation to the replay objects. 
        /// Required because we need to keep the colliders and rigid bodies enabled during playback so that triggers are detected to activate and deactivate replay cameras.
        /// </summary>
        private class CustomPreparer : IReplayPreparer
        {
            // Methods
            public void PrepareForPlayback(ReplayObject replayObject) { }
            public void PrepareForGameplay(ReplayObject replayObject) { }
        }

        // Private
        private ReplayStorage storage = null;
        private ReplayRecordOperation recordOp = null;
        private ReplayPlaybackOperation playbackOp = null;
        private bool lapStarted = false;
        private bool lapFinished = false;
        private float lapStartTime = 0;

        private Vector3 carStartPosition = Vector3.zero;
        private Quaternion carStartRotation = Quaternion.identity;

        // Public
        public Text timer;
        public GameObject uiRacing;
        public GameObject uiFinishedRace;
        public ReplayObject playerCar;
        public Camera chaseCamera;
        public TriggerCamera[] triggerCameras;
        public Camera[] carCameras;

        // Methods
        public void Start()
        {
            // Get car starting position
            carStartPosition = playerCar.transform.position;
            carStartRotation = playerCar.transform.rotation;

            // Add listeners
            foreach (TriggerCamera cam in triggerCameras)
                cam.OnTriggerStateChanged.AddListener(UpdateActionReplayCamera);
        }

        public void OnDestroy()
        {
            // Release the replay storage
            if(storage != null)
                storage.Dispose();
        }

        public void Update()
        {
            if (lapStarted == true && lapFinished == false)
            {
                TimeSpan raceTime = TimeSpan.FromSeconds(Time.time - lapStartTime);
                timer.text = string.Format("{0:00}:{1:00}:{2:00}", raceTime.Minutes, raceTime.Seconds, raceTime.Milliseconds);
            }

            // Check for restart
            if(lapFinished == true && Input.GetKeyDown(KeyCode.R) == true)
            {
                // Start racing again
                RestartRace();
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // Make sure a car is triggering
            if (other.GetComponentInParent<CarController>() == null)
                return;

            // Set finished flag
            if (lapStarted == true)
            {
                lapFinished = true;

                // Stop recording
                if (recordOp != null)
                {
                    recordOp.StopRecording();
                    recordOp = null;
                }

                // Start our action replay
                StartActionReplay();
            }
            else
            {
                lapStartTime = Time.time;
            }

            // Record the player car
            if(lapFinished == false)
            {
                // Create our storage
                storage = new ReplayMemoryStorage();

                // Start recording
                recordOp = ReplayManager.BeginRecording(storage, playerCar);
            }

            // Set lap started flag - player has crossed the line once
            lapStarted = true;
        }

        private void StartActionReplay()
        {
            // Start replay of player car
            playbackOp = ReplayManager.BeginPlayback(storage, playerCar, new CustomPreparer());

            // Disable main camera
            chaseCamera.gameObject.SetActive(false);

            // Switch UIs
            uiFinishedRace.SetActive(true);
            uiRacing.SetActive(false);

            // Activate replay cameras
            UpdateActionReplayCamera();
        }

        private void UpdateActionReplayCamera()
        {
            // Only update when the race has finished
            if (lapFinished == false)
                return;

            TriggerCamera activeCamera = null;

            // Check if any trigger cameras are available
            foreach (TriggerCamera triggerCam in triggerCameras)
            {
                triggerCam.cam.gameObject.SetActive(false);

                if (triggerCam.HasTriggerObjects == true)
                {
                    activeCamera = triggerCam;
                    triggerCam.cam.gameObject.SetActive(true);
                }
            }


            // Select random car camera
            int carCamIndex = (activeCamera == null) ? Random.Range(0, carCameras.Length) : -1;
            
            // Disable all car cameras
            for(int i = 0; i < carCameras.Length; i++)
            {
                carCameras[i].gameObject.SetActive(carCamIndex == i);
            }
        }

        private void RestartRace()
        {
            // Stop active replay
            if(playbackOp != null)
            {
                playbackOp.StopPlayback();
                playbackOp = null;
            }

            // Enable UI
            uiRacing.SetActive(true);
            uiFinishedRace.SetActive(false);

            // Disable all trigger cameras
            foreach (TriggerCamera triggerCam in triggerCameras)
                triggerCam.cam.gameObject.SetActive(false);

            // Disable all car cameras
            foreach(Camera cam in carCameras)
                cam.gameObject.SetActive(false);

            // Enable chase cam
            chaseCamera.gameObject.SetActive(true);

            // Reset state
            lapStarted = false;
            lapFinished = false;
            lapStartTime = Time.time;

            // Reset forces
            playerCar.GetComponent<Rigidbody>().velocity = Vector3.zero;
            playerCar.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            timer.text = "00:00:000";

            // Reset player car
            playerCar.transform.position = carStartPosition;
            playerCar.transform.rotation = carStartRotation;
        }
    }
}
