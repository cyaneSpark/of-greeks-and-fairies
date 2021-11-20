using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fairies
{
    public interface IInteractionManager
    {
        void HandleActorMotion(Actor actor, ActorMotion motion);
        event EventHandler<DeliveryArgs> onTryToDeliver;

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
        public GameObject interactionManagerHolder;
        private IInteractionManager interactionManager;

        public AudioSource grandma, doctor, priest;
        public AudioSource weather, grandpa;

        private Dictionary<Actor, AudioSource> speakers = new Dictionary<Actor, AudioSource>();

        List<AudioLine> lineRequests = new List<AudioLine>();

        private void Awake()
        {
            speakers.Add(Actor.grandma, grandma);
            speakers.Add(Actor.doctor, doctor);
            speakers.Add(Actor.priest, priest);

            interactionManager = interactionManagerHolder.GetComponent<IInteractionManager>();

            if (interactionManager == null)
                LogError("NO INTERACTION MANAGER");
            else
                interactionManager.onTryToDeliver += InteractionManager_onTryToDeliver;

            StartCoroutine(MonitorActiveRequestsIE());
            StartCoroutine(MonitorPendingPlayesIE());
            StartCoroutine(MonitorSilenceIE());
        }

        public float minTimeBetweenSpeaking = 1;
        float timeInSilence = 0;

        public float interLinePause = 0.5f;
        public float linesRequestPause = 0;
        public float interRequestPause = 2;
        public float interPhasePause = 0;

        private Dictionary<Actor, SingleRequest> activeRequests = new Dictionary<Actor, SingleRequest>();

        [SerializeField] private Phase currentPhase;
        [SerializeField] private State currentState;

        enum State { StoryLines, LinesToRequests, RequestAnnouncement, RequestWaiting, PhaseEnd }

        List<Actor> successfulRequests = new List<Actor>();

        IEnumerator Start()
        {
            // Play the lines of that phase
            foreach (Phase currentPhase in (Phase[])Enum.GetValues(typeof(Phase)))
            {
#if UNITY_EDITOR
                if (DEBUG_ONLY_doStartFromPhase && currentPhase < DEBUG_ONLY_startFromPhase)
                {
                    LogWarning("EDITOR ONLY :: SKIPPING {0}", currentPhase);
                    continue;
                }
#endif
                this.currentPhase = currentPhase;

                // Load the lines of that phase
                currentState = State.StoryLines;
                AudioClip[] storyLines = Resources.LoadAll<AudioClip>(GetStoryBeatPath(currentPhase));

                // Loop through them
                foreach (AudioClip line in storyLines)
                {
                    // Infer the metadata
                    ClipMetaData metaData = GetClipMetaData(line);

                    // Infer the branch and if it's one we should be on
                    if (metaData.branch != "")
                    {
                        bool goodBranch = true;

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

                        else
                        {
                            LogError("Invalid Branch :: {0}", metaData.branch);
                            continue;
                        }

                        if (goodBranch)
                            LogInfo("Using branch {0}", metaData.branch);
                        else
                        {
                            LogWarning("Skipping branch {0} | conditions not met", metaData.branch);
                            continue;
                        }
                    }

                    // Play the sound
                    yield return HandleLineIE(metaData.speaker, line);

                    // Let it die out
                    for (float t = 0; t < interLinePause; t += Time.deltaTime)
                        yield return null;
                }

                // Give it a sec
                currentState = State.LinesToRequests;
                for (float t = 0; t < linesRequestPause; t += Time.deltaTime)
                    yield return null;


                // Go through the requests of that phase
                currentState = State.RequestAnnouncement;
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
                currentState = State.RequestWaiting;
                while (activeRequests.Count > 0)
                    yield return null;

                // Wait if there's any thing still playing
                while (lineRequests.Count > 0)
                    yield return null;
                yield return null; // Let it queue if needed
                while (timeInSilence == 0)
                    yield return null;

                // Unload everything
                foreach (SingleRequest request in requests[currentPhase])
                    request.UnloadClips();

                // Let it die out
                currentState = State.PhaseEnd;
                for (float t = 0; t < interPhasePause; t += Time.deltaTime)
                    yield return null;
            }
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

#if UNITY_EDITOR
                DEBUG_ONLY_activeRequests.Clear();
                DEBUG_ONLY_activeRequests.AddRange(activeRequests.Values);
#endif

                yield return null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Dictionary serialization (<see cref="activeRequests"/> is problematic, use this to view
        /// </summary>
        List<SingleRequest> DEBUG_ONLY_activeRequests = new List<SingleRequest>();
        [SerializeField] private bool DEBUG_ONLY_doStartFromPhase = false;
        [SerializeField] private Phase DEBUG_ONLY_startFromPhase = Phase.fever;
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

            LogInfo("TRY TO DELIVER {0} {1}", actor, item);
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
    }
}