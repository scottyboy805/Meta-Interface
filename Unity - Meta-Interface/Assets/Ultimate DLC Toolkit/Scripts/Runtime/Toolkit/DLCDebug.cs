
namespace DLCToolkit
{
    internal static class Debug
    {
        // Internal
        internal static DLCLogLevel logLevel = DLCLogLevel.Info;
        internal static string logPrefix = "";

        // Methods
        public static void Log(string msg)
        {
            if(logLevel <= DLCLogLevel.Info)
                UnityEngine.Debug.Log(logPrefix + msg);
        }

        public static void LogWarning(string msg)
        {
            if(logLevel <= DLCLogLevel.Warning)
                UnityEngine.Debug.LogWarning(logPrefix + msg);
        }

        public static void LogError(string msg)
        {
            if(logLevel <= DLCLogLevel.Error)
                UnityEngine.Debug.LogError(logPrefix + msg);
        }
    }
}
