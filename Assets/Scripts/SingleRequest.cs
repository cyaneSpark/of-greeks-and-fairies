using System;
using UnityEngine;

namespace Fairies
{
    [System.Serializable]
    public class SingleRequest : BaseClass
    {
        const float req1_At01 = 0.33f;
        const float req2_At01 = 0.67f;

        [SerializeField] private bool restart = false;
        public override string ToString() => string.Format("{0}_{1}_{2}_{3}", requester, item, maxDuration, clipsLoaded ? "LOADED" : "UNLOADED");
        public SingleRequest(Actor requester, Item item, float maxDuration, bool restart = false)
        {
            this.requester = requester;
            this.item = item;
            this.maxDuration = maxDuration;
            this.restart = restart;

            string GetPath(string name) => string.Format("{0}/{1}", item, name);

            line_req0 = new AudioLine(requester, GetPath("r0"));
            line_req1 = new AudioLine(requester, GetPath("r1"));
            line_req2 = new AudioLine(requester, GetPath("r2"));
            line_wrongType = new AudioLine(requester, GetPath("wt"));
            line_wrongQuantity = new AudioLine(requester, GetPath("wq"));
            line_success = new AudioLine(requester, GetPath("s"));
            line_timeout = new AudioLine(requester, GetPath("t"));
        }

        public void Reset()
        {
            timeElapsed = 0;
            hasRequested0 = false;
            hasRequested1 = false;
            hasRequested2 = false;
        }

        public Actor requester;
        private Item item;

        [Range(15, 90)] [SerializeField] private float maxDuration;
        float req1_At => req1_At01 * maxDuration;
        float req2_At => req2_At01 * maxDuration;

        float timeElapsed = 0;

        public enum State { ClipsNotLoaded, Running, TimeOut }

        protected void RequestLine(AudioLine audioLine) => onLineRequested?.Invoke(this, audioLine);

        public event EventHandler<AudioLine> onLineRequested;

        public bool TryCompleteRequest(Item candidate)
        {
            // Correct type and quantity
            if (candidate == item)
            {
                RequestLine(line_success);
                return true;
            }

            // Correct type (wrong quantity)
            if (candidate.IsSameTypeWith(item))
                RequestLine(line_wrongQuantity);

            // Incorrect type (indifferent quantity)
            else
                RequestLine(line_wrongType);

            return false;
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
            line_req2.Load();
            line_wrongType.Load();
            line_wrongQuantity.Load();
            line_success.Load();
            line_timeout.Load();
            clipsLoaded = true;

            LogInfo("Loaded Clips");
        }

        public void UnloadClips()
        {
            clipsLoaded = true;
            line_req0.Unload();
            line_req1.Unload();
            line_req2.Unload();
            line_wrongType.Unload();
            line_wrongQuantity.Unload();
            line_success.Unload();
            line_timeout.Unload();

            LogInfo("Unloaded Clips");
        }

        /// <summary>
        /// Call out at the beginning
        /// </summary>
        private AudioLine line_req0;

        /// <summary>
        /// Call out as a first reminder (check <see cref="req1_At"/>
        /// </summary>
        private AudioLine line_req1;

        /// <summary>
        /// Call out as a last call (check <see cref="req2_At"/>
        /// </summary>
        private AudioLine line_req2;

        /// <summary>
        /// Call out when the type was wrong
        /// </summary>
        private AudioLine line_wrongType;

        /// <summary>
        /// Call out when the quantity was wrong
        /// </summary>
        private AudioLine line_wrongQuantity;

        /// <summary>
        /// Call out when the request is successfully delivered
        /// </summary>
        private AudioLine line_success;

        /// <summary>
        /// Call out when the request times out (after <see cref="maxDuration"/>
        /// </summary>
        private AudioLine line_timeout;
    }
}