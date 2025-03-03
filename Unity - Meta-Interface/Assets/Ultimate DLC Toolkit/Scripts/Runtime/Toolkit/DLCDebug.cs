
using System;

namespace DLCToolkit
{
    internal static class Debug
    {
        // Event
        internal static event Action OnLog;

        // Internal
        internal static DLCLogLevel logLevel = DLCLogLevel.Info;
        internal static string logPrefix = "";

        // Methods
        public static void Log(string msg)
        {
            // Trigger log
            OnLog?.Invoke();

            if(logLevel <= DLCLogLevel.Info)
                UnityEngine.Debug.Log(logPrefix + msg);
        }

        public static void LogWarning(string msg)
        {
            // Trigger log
            OnLog?.Invoke();

            if (logLevel <= DLCLogLevel.Warning)
                UnityEngine.Debug.LogWarning(logPrefix + msg);
        }

        public static void LogError(string msg)
        {
            // Trigger log
            OnLog?.Invoke();

            if (logLevel <= DLCLogLevel.Error)
                UnityEngine.Debug.LogError(logPrefix + msg);
        }
    }
}
