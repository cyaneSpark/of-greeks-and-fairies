using System;
using UnityEngine;

namespace Fairies
{
    [System.Serializable]
    public class SingleRequest : BaseClass
    {
        const float defaultDuration = 60;
        const float req1_At01 = 0.33f;
        const float req2_At01 = 0.67f;

        [SerializeField] private bool restart = false;
        private string itemStr => itemB == null ? itemA.ToString() : string.Format("{0}_{1}", itemA, itemB);
        public override string ToString() => string.Format("{0}_{1}_{2}_{3}", requester, itemStr, maxDuration, clipsLoaded ? "LOADED" : "UNLOADED");
        public SingleRequest(Actor requester, Item itemA, Item? itemB = null, float maxDuration = defaultDuration, bool restart = false)
        {
            this.requester = requester;
            this.itemA = itemA;
            this.itemB = itemB;
            this.maxDuration = maxDuration;
            this.restart = restart;

            string GetPath(string name) => string.Format("{0}/{1}", itemStr, name);

            line_req0 = new AudioLine(requester, GetPath("r0"));

            line_req1 = new AudioLine(requester, GetPath("r1"));
            line_req1_a = new AudioLine(requester, GetPath("r1_a"));
            line_req1_b = new AudioLine(requester, GetPath("r1_b"));

            line_req2 = new AudioLine(requester, GetPath("r2"));
            line_req2_a = new AudioLine(requester, GetPath("r2_a"));
            line_req2_b = new AudioLine(requester, GetPath("r2_b"));

            line_wrong = new AudioLine(requester, GetPath("w"));
            line_wrong_a = new AudioLine(requester, GetPath("w_a"));
            line_wrong_b = new AudioLine(requester, GetPath("w_b"));

            line_success = new AudioLine(requester, GetPath("s"));
            line_success_a = new AudioLine(requester, GetPath("s_a"));
            line_success_b = new AudioLine(requester, GetPath("s_b"));

            line_timeout = new AudioLine(requester, GetPath("t"));
            line_timeout_a = new AudioLine(requester, GetPath("t_a"));
            line_timeout_b = new AudioLine(requester, GetPath("t_b"));
        }

        public void Reset()
        {
            timeElapsed = 0;
            hasRequested0 = false;
            hasRequested1 = false;
            hasRequested2 = false;
        }

        public Actor requester;
        private Item itemA;
        private Item? itemB;

        private bool fulfiledA = false;
        private bool fulfiledB = false;

        [Range(15, 90)] [SerializeField] private float maxDuration;
        float req1_At => req1_At01 * maxDuration;
        float req2_At => req2_At01 * maxDuration;

        float timeElapsed = 0;

        public enum State { ClipsNotLoaded, Running, TimeOut }

        protected void RequestLine(AudioLine audioLine) => onLineRequested?.Invoke(this, audioLine);

        public event EventHandler<AudioLine> onLineRequested;

        public enum GiveItemCallback { Reject, Partial, Complete }

        public GiveItemCallback TryGiveItem(Item candidate)
        {
            // SINGLE ITEM
            if (itemB == null)
            {
                // Item A 
                if (candidate == itemA)
                {
                    RequestLine(line_success);
                    return GiveItemCallback.Complete;
                }

                // Wrong item
                RequestLine(line_wrong);

                fulfiledA = true;

                return GiveItemCallback.Reject;
            }

            // TWO ITEMS
            if (candidate == itemA)
            {
                fulfiledA = true;

                if (fulfiledB)
                {
                    RequestLine(line_success);
                    return GiveItemCallback.Complete;
                }

                RequestLine(line_success_a);
                return GiveItemCallback.Partial;
            }

            else if (candidate == itemB)
            {
                fulfiledB = true;

                if (fulfiledA)
                {
                    RequestLine(line_success);
                    return GiveItemCallback.Complete;
                }

                RequestLine(line_success_b);
                return GiveItemCallback.Partial;
            }

            // It's a wrong item
            else
            {
                if (!fulfiledA && !fulfiledB)
                    RequestLine(line_wrong);
                else if (!fulfiledA)
                    RequestLine(line_wrong_a);
                else
                    RequestLine(line_wrong_b);

                return GiveItemCallback.Reject;
            }
        }

        private bool hasRequested0;
        private bool hasRequested1;
        private bool hasRequested2;

        /// <summary>
        /// Returns true while we need to keep going
        /// </summary>
        public State Update(float dT)
        {
            if (!clipsLoaded)
            {
                LogWarning("No clips!");
                return State.ClipsNotLoaded;
            }
            timeElapsed += dT;

            // Done!
            if (timeElapsed >= maxDuration)
            {
                RequestLine(line_timeout);

                if (restart)
                {
                    LogInfo("Timeout - Restarting!");
                    Reset();
                    return State.Running;
                }

                LogInfo("Timeout - Ending!");
                return State.TimeOut;
            }

            // Do we need to update?
            if (!hasRequested0)
            {
                RequestLine(line_req0);
                hasRequested0 = true;
            }

            if (!hasRequested1 && timeElapsed > req1_At)
            {
                RequestLine(line_req1);
                hasRequested1 = true;
            }

            if (!hasRequested2 && timeElapsed > req2_At)
            {
                RequestLine(line_req2);
                hasRequested2 = true;
            }

            return State.Running;
        }

        bool clipsLoaded = false;

        public void LoadClips()
        {
            line_req0.Load();

            line_req1.Load();
            line_req1_a.Load();
            line_req1_b.Load();

            line_req2.Load();
            line_req2_a.Load();
            line_req2_b.Load();

            line_wrong.Load();
            line_wrong_a.Load();
            line_wrong_b.Load();

            line_success.Load();
            line_success_a.Load();
            line_success_b.Load();

            line_timeout.Load();
            line_timeout_a.Load();
            line_timeout_b.Load();

            clipsLoaded = true;

            LogInfo("Loaded Clips");
        }

        public void UnloadClips()
        {
            clipsLoaded = true;

            line_req0.Unload();

            line_req1.Unload();
            line_req1_a.Unload();
            line_req1_b.Unload();

            line_req2.Unload();
            line_req2_a.Unload();
            line_req2_b.Unload();

            line_wrong.Unload();
            line_wrong_a.Unload();
            line_wrong_b.Unload();

            line_success.Unload();
            line_success_a.Unload();
            line_success_b.Unload();

            line_timeout.Unload();
            line_timeout_a.Unload();
            line_timeout_b.Unload();

            LogInfo("Unloaded Clips");
        }

        /// <summary>
        /// Call out at the beginning with all items
        /// </summary>
        private AudioLine line_req0;

        /// <summary>
        /// Call out as a first reminder (check <see cref="req1_At"/> if all items are missing
        /// </summary>
        private AudioLine line_req1;

        /// <summary>
        /// Call out as a first reminder (check <see cref="req1_At"/> if only A is missing
        /// </summary>
        private AudioLine line_req1_a;

        /// <summary>
        /// Call out as a first reminder (check <see cref="req1_At"/> if only B is missing
        /// </summary>
        private AudioLine line_req1_b;

        /// <summary>
        /// Call out as a last call (check <see cref="req2_At"/> if all items are missing
        /// </summary>
        private AudioLine line_req2;

        /// <summary>
        /// Call out as a last call (check <see cref="req2_At"/> if only A is missing
        /// </summary>
        private AudioLine line_req2_a;

        /// <summary>
        /// Call out as a last call (check <see cref="req2_At"/> if only B is missing
        /// </summary>
        private AudioLine line_req2_b;

        /// <summary>
        /// Call out when the type was wrong, and all items are missing
        /// </summary>
        private AudioLine line_wrong;

        /// <summary>
        /// Call out when the type was wrong, and only A is missing
        /// </summary>
        private AudioLine line_wrong_a;

        /// <summary>
        /// Call out when the type was wrong, and only B is missing
        /// </summary>
        private AudioLine line_wrong_b;

        /// <summary>
        /// Call out when the request is successfully delivered and both items are there
        /// </summary>
        private AudioLine line_success;

        /// <summary>
        /// Call out when the request is successfully delivered, but only a has been delivered
        /// </summary>
        private AudioLine line_success_a;

        /// <summary>
        /// Call out when the request is successfully delivered, but only b has been delivered
        /// </summary>
        private AudioLine line_success_b;

        /// <summary>
        /// Call out when the request times out (after <see cref="maxDuration"/> and all items are missing
        /// </summary>
        private AudioLine line_timeout;

        /// <summary>
        /// Call out when the request times out (after <see cref="maxDuration"/> but only A is missing
        /// </summary>
        private AudioLine line_timeout_a;

        /// <summary>
        /// Call out when the request times out (after <see cref="maxDuration"/> but only B is missing
        /// </summary>
        private AudioLine line_timeout_b;
    }
}