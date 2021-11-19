using System;
using System.Collections;
using UnityEngine;

namespace Fairies
{
    public abstract class BaseBehaviour : MonoBehaviour
    {
        protected Coroutine DoThenCallback(IEnumerator doIE, Action callback) => StartCoroutine(DoThenCallbackIE(doIE, callback));
        protected IEnumerator DoThenCallbackIE(IEnumerator doIE, Action callback)
        {
            yield return DoIE(doIE, 1, 0);
            callback?.Invoke();
        }
        protected IEnumerator Wait1_ThenDoIE(IEnumerator doIE) => DoIE(doIE, 1, 0);
        protected IEnumerator DoIE_ThenWait1(IEnumerator doIE) => DoIE(doIE, 0, 1);
        protected IEnumerator DoIE(IEnumerator doIE, float waitBefore, float waitAfter, Func<bool> interrupt = null)
        {
            for (float t = 0; t < waitBefore; t += Time.deltaTime)
            {
                if (interrupt != null && interrupt())
                    yield break;
                yield return null;
            }
            yield return doIE;
            for (float t = 0; t < waitAfter; t += Time.deltaTime)
            {
                if (interrupt != null && interrupt())
                    yield break;
                yield return null;
            }
        }

        public void Log(object message) => _Log(message, LogType.Log);
        public void LogInfo(string message, params object[] args) => _Log(string.Format(message, args), LogType.Log);
        public void LogError(object message) => _Log(message, LogType.Error);
        public void LogError(string message, params object[] args) => _Log(string.Format(message, args), LogType.Error);
        public void LogWarning(object message) => _Log(message, LogType.Warning);
        public void LogWarning(string message, params object[] args) => _Log(string.Format(message, args), LogType.Warning);
        public void LogException(object message) => _Log(message, LogType.Exception);
        public void LogException(string message, params object[] args) => _Log(string.Format(message, args), LogType.Exception);
        private void _Log(object message, LogType logType)
        {
            if (!Debug.isDebugBuild)
                return;

            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(message, this);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message, this);
                    break;
                case LogType.Log:
                    Debug.Log(message, this);
                    break;
                case LogType.Exception:
                    Debug.LogException(new Exception(message.ToString()), this);
                    break;
            }
        }
    }
}