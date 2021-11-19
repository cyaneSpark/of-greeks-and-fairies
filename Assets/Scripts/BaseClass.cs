using System;
using UnityEngine;

namespace Fairies
{
    public abstract class BaseClass
    {
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

            message = string.Format("[{0}]: {1}", this, message);

            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogType.Log:
                    Debug.Log(message);
                    break;
                case LogType.Exception:
                    Debug.LogException(new Exception(message.ToString()));
                    break;
            }
        }
    }
}