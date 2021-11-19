using UnityEngine;

namespace Fairies
{
    [System.Serializable]
    public class AudioLine : BaseClass
    {
        public AudioClip clip;
        public Actor speaker;
        public string path;

        public override string ToString() => string.Format("{0}_\"{1}\"_{2}", speaker, path, clip != null ? "LOADED" : "UNLOADED");
        /// <summary>
        /// Gets created under Resources/<see cref="speaker"/>
        /// </summary>
        public AudioLine(Actor speaker, string subPath)
        {
            this.speaker = speaker;
            path = string.Format("{0}/{1}", speaker.ToString(), subPath);
        }

        public static implicit operator AudioClip(AudioLine line)
        {
            if (line == null) return null;
            if (line.clip == null)
            {
                Debug.LogError("Hasn't loaded " + line.path);
                return null;
            }

            return line.clip;
        }

        public bool Load()
        {
            if (clip != null) return true;
            clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                LogError("Could not find " + path);
                return false;
            }
            Log("Loaded");
            return true;
        }

        public void Unload()
        {
            AudioClip aC = clip;
            clip = null;
            Resources.UnloadAsset(aC);
            Log("Unloaded");
        }
    }
}