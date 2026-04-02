namespace OnlyFarms.Utilities
{
    public class OFLogger
    {
        [System.Diagnostics.Conditional("ENABLE_LOG")]
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        public static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}