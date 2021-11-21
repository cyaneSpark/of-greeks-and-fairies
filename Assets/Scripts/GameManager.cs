using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fairies
{
    public interface IInteractionManager
    {
        void HandleActorMotion(Actor actor, ActorMotion motion);
        event EventHandler<DeliveryArgs> onTryToDeliver;
        event EventHandler<bool> onPauseToggled;

        public enum ActorMotion { Appear, Partial, Reject, Disappear }

        public class DeliveryArgs : EventArgs
        {
            public Actor receiver;
            public Item item;

            public DeliveryArgs(Actor receiver, Item item)
            {
                this.receiver = receiver;
                this.item = item;
            }
        }
    }

    public partial class GameManager : BaseBehaviour
    {
        const string sceneVR = "VR_RIG";
#if UNITY_EDITOR
        const string EDITOR_ONLY_sceneTEST = "Test_FLOW";
#endif
        const float resetAfterPauseSecs = 60;


        private IInteractionManager interactionManager;


        public AudioSource grandma, doctor, priest;
        public AudioSource weather, grandpa;

        private Dictionary<Actor, AudioSource> speakers = new Dictionary<Actor, AudioSource>();

        List<AudioLine> lineRequests = new List<AudioLine>();

#if UNITY_EDITOR
        [SerializeField] bool EDITOR_ONLY_useTestScene = false;
        [SerializeField] bool EDITOR_ONLY_fastPause = false;
        [SerializeField] float EDITOR_ONLY_resetAfterPauseSecs_Fast = 6;
#endif

        private void Awake()
        {
            speakers.Add(Actor.grandma, grandma);
            speakers.Add(Actor.doctor, doctor);
            speakers.Add(Actor.priest, priest);

            Reset();
        }

        private void Reset()
        {
            LogInfo("RESET");

            // Unload VR rig
            StopAllCoroutines();

            timeInSilence = 0;

            foreach (Actor actor in new List<Actor>(activeRequests.Keys))
                TryRemoveRequest(actor);

            lineRequests.Clear();

            UnloadAllClips();

            StartCoroutine(ResetIE());
        }

        bool isSceneLoaded = false;

        private IEnumerator ResetIE()
        {
            string scene = sceneVR;

#if UNITY_EDITOR
            if (EDITOR_ONLY_useTestScene)
                scene = EDITOR_ONLY_sceneTEST;
#endif

            // Drop the necessary refs
            if (isSceneLoaded)
            {
                if (interactionManager != null)
                {
                    interactionManager.onTryToDeliver -= InteractionManager_onTryToDeliver;
                    interactionManager.onPauseToggled -= InteractionManager_onPauseToggled;
                    LogInfo("Disconnected from IM :: {0}", interactionManager);
                }

                // Unload
                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);

                while (!unload.isDone)
                    yield return null;

                LogInfo("Scene unloaded :: {0}", scene);

                isSceneLoaded = false;
            }

            // Load
            AsyncOperation load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);

            while (!load.isDone)
                yield return null;

            LogInfo("Scene loaded :: {0}", scene);
            isSceneLoaded = true;

            // Get the necessary refs
            interactionManager = FindObjectOfType<MainInteractionManager>();
#if UNITY_EDITOR
            if (interactionManager == null)
                interactionManager = FindObjectOfType<Dev.SampleInteractionManager>();
#endif

            if (interactionManager == null)
                throw new Exception("NO INTERACTION MANAGER - CAN NOT PROCEED");
            else
            {
                interactionManager.onTryToDeliver += InteractionManager_onTryToDeliver;
                interactionManager.onPauseToggled += InteractionManager_onPauseToggled;
                LogInfo("Connected to IM :: {0}", interactionManager);
            }

            Time.timeScale = 1; // in case pause was running

            StartCoroutine(MonitorActiveRequestsIE());
            StartCoroutine(MonitorPendingPlayesIE());
            StartCoroutine(MonitorSilenceIE());
            StartCoroutine(PlayIE());
        }

        public float minTimeBetweenSpeaking = 1;
        float timeInSilence = 0;

        public float interLinePause = 0.5f;
        public float linesRequestPause = 0;
        public float interRequestPause = 2;
        public float interPhasePause = 0;

        private Dictionary<Actor, SingleRequest> activeRequests = new Dictionary<Actor, SingleRequest>();

        [SerializeField] private Phase DEBUG_ONLY_currentPhase;
        [SerializeField] private State DEBUG_ONLY_currentState;

        enum State { StoryLines, LinesToRequests, RequestAnnouncement, RequestWaiting, PhaseEnd }

        List<Actor> successfulRequests = new List<Actor>();

        IEnumerator PlayIE()
        {
            // Play the lines of that phase
            foreach (Phase currentPhase in (Phase[])Enum.GetValues(typeof(Phase)))
            {
#if UNITY_EDITOR
                if (EDITOR_ONLY_doStartFromPhase && currentPhase < EDITOR_ONLY_startFromPhase)
                {
                    LogWarning("EDITOR ONLY :: SKIPPING {0}", currentPhase);
                    continue;
                }
#endif
                DEBUG_ONLY_currentPhase = currentPhase;

                // Load the lines of that phase
                DEBUG_ONLY_currentState = State.StoryLines;
                AudioClip[] storyLines = Resources.LoadAll<AudioClip>(GetStoryBeatPath(currentPhase));

                // Loop through them
                foreach (AudioClip line in storyLines)
                {
                    // Infer the metadata
                    ClipMetaData metaData = GetClipMetaData(line);
                    bool doParallel = false;

                    // Infer the branch and if it's one we should be on
                    if (metaData.branch != "")
                    {
                        bool goodBranch = false;

                        // Successful Priest
                        if (metaData.branch == "ps")
                            goodBranch = successfulRequests.Contains(Actor.priest);

                        // Successful Doctor
                        else if (metaData.branch == "ds")
                            goodBranch = successfulRequests.Contains(Actor.doctor);

                        // Successful Grandma
                        else if (metaData.branch == "gs")
                            goodBranch = successfulRequests.Contains(Actor.grandma);

                        // Successful Priest
                        else if (metaData.branch == "pf")
                            goodBranch = !successfulRequests.Contains(Actor.priest);

                        // Successful Doctor
                        else if (metaData.branch == "df")
                            goodBranch = !successfulRequests.Contains(Actor.doctor);

                        // Successful Grandma
                        else if (metaData.branch == "gf")
                            goodBranch = !successfulRequests.Contains(Actor.grandma);

                        // Parallel play!
                        else if (metaData.branch == "x")
                        {
                            goodBranch = true;
                            doParallel = true;
                        }
                        else
                        {
                            LogError("Invalid Branch :: {0}", metaData.branch);
                            continue;
                        }

                        if (goodBranch)
                            LogInfo("Using branch {0}{1}", metaData.branch, doParallel ? " [PARALLEL]" : "");
                        else
                        {
                            LogWarning("Skipping branch {0} | conditions not met", metaData.branch);
                            continue;
                        }
                    }

                    // Play the sound (parallelly)
                    if (doParallel)
                    {
                        StartCoroutine(HandleLineIE(metaData.speaker, line));
                        continue;
                    }

                    // Play the sound (serially)
                    yield return HandleLineIE(metaData.speaker, line);

                    // Let it die out
                    for (float t = 0; t < interLinePause; t += Time.deltaTime)
                        yield return null;
                }

                // Give it a sec
                DEBUG_ONLY_currentState = State.LinesToRequests;
                for (float t = 0; t < linesRequestPause; t += Time.deltaTime)
                    yield return null;


                // Go through the requests of that phase
                DEBUG_ONLY_currentState = State.RequestAnnouncement;
                successfulRequests.Clear();
                foreach (SingleRequest request in requests[currentPhase])
                {
                    // Initialize it
                    request.onLineRequested += Request_onLineRequested;
                    request.LoadClips();

                    // Start Monitoring it 
                    TryAddRequest(request);

                    // Let it die out
                    for (float t = 0; t < interRequestPause; t += Time.deltaTime)
                        yield return null;
                }

                // Wait for all the requests to play out
                DEBUG_ONLY_currentState = State.RequestWaiting;
                while (activeRequests.Count > 0)
                    yield return null;

                // Wait if there's any thing still playing
                while (lineRequests.Count > 0)
                    yield return null;
                yield return null; // Let it queue if needed
                while (timeInSilence == 0)
                    yield return null;

                // Unload everything
                UnloadAllClips();

                // Let it die out
                DEBUG_ONLY_currentState = State.PhaseEnd;
                for (float t = 0; t < interPhasePause; t += Time.deltaTime)
                    yield return null;
            }

            Reset();
        }

        void UnloadAllClips()
        {
            foreach (SingleRequest request in requests[DEBUG_ONLY_currentPhase])
                request.UnloadClips();
        }

        private IEnumerator MonitorActiveRequestsIE()
        {
            List<Actor> timedOutRequests = new List<Actor>();
            while (true)
            {
                timedOutRequests.Clear();
                foreach (KeyValuePair<Actor, SingleRequest> kVP in activeRequests)
                {
                    SingleRequest.State state = kVP.Value.Update(Time.deltaTime);

                    switch (state)
                    {
                        case SingleRequest.State.ClipsNotLoaded:
                            LogError("Request's clips haven't been loaded!");
                            break;
                        case SingleRequest.State.Running:
                            // all good
                            break;
                        case SingleRequest.State.TimeOut:
                            // Should be removed
                            timedOutRequests.Add(kVP.Key);
                            break;
                        case SingleRequest.State.Impossible:
                            // Should be removed
                            timedOutRequests.Add(kVP.Key);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                foreach (Actor actor in timedOutRequests)
                    TryRemoveRequest(actor);

                yield return null;
            }
        }

#if UNITY_EDITOR
        [SerializeField] private bool EDITOR_ONLY_doStartFromPhase = false;
        [SerializeField] private Phase EDITOR_ONLY_startFromPhase = Phase.fever;
#endif
        private IEnumerator MonitorSilenceIE()
        {
            while (true)
            {
                while (isNoisy())
                    yield return null;

                LogInfo("Silence started");

                while (!isNoisy())
                {
                    timeInSilence += Time.deltaTime;
                    yield return null;
                }

                timeInSilence = 0;
                LogInfo("Silence broke");

                bool isNoisy() => speakers.Any(x => x.Value.isPlaying);
            }
        }

        private IEnumerator MonitorPendingPlayesIE()
        {
            List<AudioLine> linesToHandle = new List<AudioLine>();
            while (true)
            {
                // Handle any pending lines
                linesToHandle.Clear();
                linesToHandle.AddRange(lineRequests);
                lineRequests.Clear(); // free it up so that new requests can come
                foreach (AudioLine line in linesToHandle)
                {
                    // Wait until there's silence
                    while (timeInSilence < minTimeBetweenSpeaking)
                        yield return null;

                    // Speak
                    LogInfo("Handling line {0}", line);
                    yield return HandleLineIE(line.speaker, line.clip);
                    LogInfo("Handled line {0}", line);
                }

                yield return null;
            }
        }

        private IEnumerator HandleLineIE(Actor speaker, AudioClip line)
        {
            if (line == null)
            {
                LogWarning("Null clip for {0}", speaker);
                yield break;
            }

            // Get the AS
            LogInfo("Playing {0}: {1}", speaker, line);
            AudioSource speakerAS = speakers[speaker];

            // Play the sound
            speakerAS.clip = line;
            speakerAS.Play();

            // Wait
            yield return null;
            while (speakerAS.isPlaying)
                yield return null;
        }

        private Coroutine pauseCR;

        private IEnumerator PauseIE()
        {
            float timeStartPause = Time.realtimeSinceStartup;
            float waitFor = resetAfterPauseSecs;

#if UNITY_EDITOR
            if (EDITOR_ONLY_fastPause)
                waitFor = EDITOR_ONLY_resetAfterPauseSecs_Fast;
#endif

            while (Time.realtimeSinceStartup - timeStartPause < waitFor)
                yield return null;

            LogInfo("Pause timed out ; resetting!");

            pauseCR = null;

            Reset();
        }

        private void TryPause()
        {
            Time.timeScale = 0;

            if (pauseCR != null) // was already paused?
            {
                LogWarning("Weird, we were already timing out our pause.. Ignoring");
                return;
            }

            pauseCR = StartCoroutine(PauseIE());
        }

        private void TryResume()
        {
            Time.timeScale = 1;

            if (pauseCR == null)
            {
                LogWarning("Weird.. we didn't have a pause time out.. Ignoring");
                return;
            }

            // Cancel timeout
            StopCoroutine(pauseCR);
            pauseCR = null;
        }

        private void Request_onLineRequested(object sender, AudioLine e)
        {
            LogInfo("Queued {0} for play", e);
            lineRequests.Add(e);
        }

        private bool TryRemoveRequest(Actor actor)
        {
            if (!activeRequests.ContainsKey(actor))
            {
                LogError("Asked to remove {0} ; not part of the {1} current active requests", actor, activeRequests.Count);
                return false;
            }

            activeRequests[actor].onLineRequested -= Request_onLineRequested;
            activeRequests.Remove(actor);
            
            if (interactionManager == null)
                LogError("Null interaction manager");
            else
                interactionManager.HandleActorMotion(actor, IInteractionManager.ActorMotion.Disappear);

            return true;
        }

        private bool TryAddRequest(SingleRequest request)
        {
            Actor actor = request.requester;
            if (activeRequests.ContainsKey(actor))
            {
                LogError("Asked to add {0} ; {1} already had an active requests ; replacing it", actor, activeRequests.Count);
                activeRequests[actor] = request;
                return false;
            }

            activeRequests.Add(actor, request);

            if (interactionManager == null)
                LogError("Null interaction manager");
            else
                interactionManager.HandleActorMotion(actor, IInteractionManager.ActorMotion.Appear);

            return true;
        }

        private void InteractionManager_onTryToDeliver(object sender, IInteractionManager.DeliveryArgs e)
        {
            Actor actor = e.receiver;
            Item item = e.item;

            LogInfo("IM :: Try to deliver {0} {1}", actor, item);

            if (actor == Actor.INVALID)
            {
                LogError("Tried delivering to an invalid Actor ; abort");
                return;
            }


            // Is it between the active requests?
            if (!activeRequests.ContainsKey(actor))
            {
                LogError("Requested to deliver to {0} ({1}) but there's no active req", actor, item);
                return;
            }

            // Is it a valid deliverance?
            SingleRequest.GiveItemResult giveItemResult = activeRequests[actor].TryGiveItem(item);

            switch (giveItemResult)
            {
                case SingleRequest.GiveItemResult.Reject:
                    LogWarning("Tried delivering to {0} ({1}) but was invalid", actor, item);
                    interactionManager.HandleActorMotion(actor, IInteractionManager.ActorMotion.Reject);
                    break;
                case SingleRequest.GiveItemResult.Partial:
                    LogWarning("Successfully delivered to {0} ({1}) ; partially complete", actor, item);
                    interactionManager.HandleActorMotion(actor, IInteractionManager.ActorMotion.Partial);
                    break;
                case SingleRequest.GiveItemResult.Complete:
                    LogInfo("Delivered to {0} ({1}) correctly", actor, item);
                    TryRemoveRequest(actor);
                    successfulRequests.Add(actor);
                    interactionManager.HandleActorMotion(actor, IInteractionManager.ActorMotion.Disappear);
                    break;
                default:
                    break;
            }

            // Mark any pending requests that wanted the same item for cancelation
            foreach (SingleRequest req in activeRequests.Values.Where(x => x.WantsItem(item)))
                req.MarkImpossible();
        }

        private void InteractionManager_onPauseToggled(object sender, bool doPause)
        {
            LogInfo("IM :: Pause toggled {0}", doPause ? "ON" : "OFF");

            if (doPause)
                TryPause();
            else
                TryResume();
        }
    }
}