using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using UnityEngine.Events;

namespace UltimateReplay.Demo
{
    public class KillcamReplay : MonoBehaviour
    {
        // Events
        //public UnityEvent OnkillcamFinished;

        // Private
        //private ReplayMemoryStorage killcamStorage = null;
        //private ReplayRecordOperation killcamRecord = null;
        //private ReplayPlaybackOperation killcamPlayback = null;
        //private bool isViewingKillcam = false;

        // Public
        public GameObject killcamUI;
        public GameObject killcamCamera;

        public SimpleCharacter character;

        // Methods
        public void ShowKillcamPerspective()
        {
            // Show Ui and camera
            killcamUI.SetActive(true);
            killcamCamera.SetActive(true);

            // Hide torso
            character.SetTorsoRenderer(false);
        }

        public void HideKillcamPerspective()
        {
            // Disable UI
            killcamUI.SetActive(false);
            killcamCamera.SetActive(false);

            // Hide torso
            character.SetTorsoRenderer(true);
        }

        //private void Start()
        //{
        //    // Create storage
        //    killcamStorage = new ReplayMemoryStorage("Killcam: " + gameObject.name);

        //    // Start recording
        //    RecordKillcam();
        //}

        //public void RecordKillcam()
        //{
        //    // Check for already recording
        //    if (killcamRecord != null && killcamRecord.IsRecordingOrPaused == true)
        //        return;

        //    // Start recording
        //    killcamRecord = ReplayManager.BeginRecording(killcamStorage);
        //}


        //public void PlayKillcam()
        //{
        //    // Check for already replaying
        //    if (killcamPlayback != null && killcamPlayback.IsReplayingOrPaused == true)
        //        return;

        //    // Stop recording
        //    if (killcamRecord != null)
        //    {
        //        killcamRecord.StopRecording();
        //        killcamRecord = null;
        //    }


        //    // Start replaying
        //    killcamPlayback = ReplayManager.BeginPlayback(killcamStorage);

        //    // Show Ui
        //    killcamUI.SetActive(true);
        //    killcamCamera.SetActive(true);
        //    isViewingKillcam = true;

        //    // Listen for end
        //    killcamPlayback.OnPlaybackEnd.AddListener(OnKillcamPlaybackEnd);
        //}

        //private void OnKillcamPlaybackEnd()
        //{
        //    // Remove listener
        //    killcamPlayback.OnPlaybackEnd.RemoveListener(OnKillcamPlaybackEnd);

        //    // Disable UI
        //    killcamUI.SetActive(false);
        //    killcamCamera.SetActive(false);
        //    isViewingKillcam = false;

        //    // Trigger event
        //    OnkillcamFinished.Invoke();

        //    // Reset playback states
        //    this.killcamPlayback = null;


        //    // Start recording again
        //    RecordKillcam();
        //}
    }
}
