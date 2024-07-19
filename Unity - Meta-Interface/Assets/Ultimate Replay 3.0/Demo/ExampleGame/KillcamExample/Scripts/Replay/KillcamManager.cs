using System;
using System.Collections;
using System.Collections.Generic;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public class KillcamManager : MonoBehaviour
    {
        // Private
        private class KillcamState
        {
            // Public
            public KillcamReplay killcam;
            public ReplayStorage killcamStorage;
            public ReplayRecordOperation killcamRecord;
            public ReplayPlaybackOperation killcamPlayback;
        }

        // Private
        private Dictionary<KillcamReplay, KillcamState> activeKillcams = new Dictionary<KillcamReplay, KillcamState>();
        private bool isViewingKillcam = false;

        // Public
        public SimpleFPSController playerController;
        public List<KillcamReplay> otherPlayerKillcams;
        public float recordDuration = 5f;

        // Methods
        private void Start()
        {
            // Add listener
            playerController.damage.OnKilled.AddListener(OnPlayerKilled);

            // Setup all active killcams
            foreach(KillcamReplay killcam in otherPlayerKillcams)
            {
                activeKillcams[killcam] = new KillcamState
                {
                    killcam = killcam,
                    killcamStorage = new ReplayMemoryStorage("Killcam: " + killcam.gameObject.name, recordDuration),
                };
            }


            // Start recording for all players
            BeginRecordingAll();
        }

        private void OnPlayerKilled(GameObject killedBy)
        {
            StartCoroutine(OnPlayerShowKillcamDelayed(killedBy, playerController.viewDeathTime));   
        }

        private IEnumerator OnPlayerShowKillcamDelayed(GameObject killedBy, float waitTime)
        {
            // Wait for death animation to play
            yield return new WaitForSeconds(waitTime);

            // Disable player cameras
            playerController.fpsCamera.enabled = false;
            playerController.deathCamera.enabled = false;


            // Find killcam
            KillcamReplay replay = otherPlayerKillcams.Find(k => k.gameObject == killedBy);

            // Check for no killcam available
            if (replay == null)
            {
                // Simply respawn and continue with the game
                playerController.Respawn();
                yield break;
            }


            // Finish recording for all players
            StopRecordingAll();


            // We can start the replay
            ReplayScene playbackScene = CreateDuplicatePlayersForKillcam(replay);

            // Disable all players that are active - we will create a new duplicate set of players for the replay so that the current state is not lost
            SetAllPlayersinSceneActive(false);

            // Start the replay
            BeginKillcamPlayback(replay, playbackScene);
        }

        private void BeginRecordingAll()
        {
            // Begin recording
            foreach(KillcamState state in activeKillcams.Values)
            {
                // Check for already recording
                if (state.killcamRecord != null && state.killcamRecord.IsRecordingOrPaused == true)
                    continue;

                // Start the recording process
                state.killcamRecord = ReplayManager.BeginRecording(state.killcamStorage);
            }
        }

        private void StopRecordingAll()
        {
            // Stop recording
            foreach(KillcamState state in activeKillcams.Values)
            {
                // Check for not recording
                if (state.killcamRecord == null || state.killcamRecord.IsRecordingOrPaused == false)
                    continue;

                // Stop the recording process
                state.killcamRecord.StopRecording();
                state.killcamRecord = null;
            }
        }

        private void BeginKillcamPlayback(KillcamReplay killcam, ReplayScene scene)
        {
            // Check for already viewing killcam
            if (isViewingKillcam == true)
                throw new InvalidOperationException("Another killcam is already replaying!");

            // Get the replay state
            KillcamState state = activeKillcams[killcam];

            // Create the replay
            state.killcamPlayback = ReplayManager.BeginPlayback(state.killcamStorage, scene);
            isViewingKillcam = true;

            // Add playback end listener
            state.killcamPlayback.OnPlaybackEnd.AddListener(() => OnEndKillcamPlayback(killcam, state, scene));
        }

        private void OnEndKillcamPlayback(KillcamReplay killcam, KillcamState state, ReplayScene scene)
        {
            Debug.Log("End replay");

            // Remove listener
            state.killcamPlayback.OnPlaybackEnd.RemoveAllListeners();

            // Reset state
            isViewingKillcam = false;
            state.killcamPlayback = null;

            // Hide killcam perspective
            killcam.HideKillcamPerspective();


            // Return to game state
            DestroyDuplicatePlayersForKillcam(scene);

            // Enable all players in game again
            SetAllPlayersinSceneActive(true);

            // Respawn player
            playerController.Respawn();

            // Start recording again
            BeginRecordingAll();
        }

        private void SetAllPlayersinSceneActive(bool active)
        {
            // Setup player
            playerController.gameObject.SetActive(active);

            // Setup all opponents
            foreach (KillcamReplay killcam in otherPlayerKillcams)
                killcam.gameObject.SetActive(active);
        }

        private ReplayScene CreateDuplicatePlayersForKillcam(KillcamReplay targetReplay)
        {
            // Create the scene
            ReplayScene scene = new ReplayScene();

            // Duplicate player
            SimpleFPSController playerDuplicate = Instantiate(playerController);
            playerDuplicate.fpsCamera.enabled = false;
            playerDuplicate.SetTorsoRenderer(true);

            // Clone identity - This is important so that the recorded killcam can be replayed onto this duplicate object
            ReplayObject.CloneReplayObjectIdentity(playerController.gameObject, playerDuplicate.gameObject);

            // Add to scene
            scene.AddReplayObject(playerDuplicate.gameObject);


            // Duplicate all opponents
            foreach(KillcamReplay killcam in otherPlayerKillcams)
            {
                // Duplicate opponent
                KillcamReplay killcamDuplicate = Instantiate(killcam);

                // Check if we should view the replay from this perspective
                if (targetReplay == killcam)
                {
                    killcamDuplicate.ShowKillcamPerspective();
                }

                // Clone identity - This is important so that the recorded killcam can be replayed onto this duplicate object
                ReplayObject.CloneReplayObjectIdentity(killcam.gameObject, killcamDuplicate.gameObject);

                // Add to scene
                scene.AddReplayObject(killcamDuplicate.gameObject);
            }

            return scene;
        }

        private void DestroyDuplicatePlayersForKillcam(ReplayScene scene)
        {
            // Destroy all
            foreach(ReplayObject targetObject in scene.ActiveReplayObjects)
            {
                // Destroy the object because it is no longer needed for playback
                Destroy(targetObject.gameObject);
            }

            // Clear the scene to be tidy
            scene.Clear();
        }
    }
}
