using System.Runtime.CompilerServices;

namespace OnlyFarms.Utilities
{
    public class OFLogger
    {
        [System.Diagnostics.Conditional("ENABLE_LOG")]
        public static void Log(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string filePath = "")
        {
            UnityEngine.Debug.Log($"<b>{ExtractClassName(filePath)}.{member}:</b> {message}");
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        public static void LogWarning(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string filePath = "")
        {
            UnityEngine.Debug.LogWarning($"<b>{ExtractClassName(filePath)}.{member}:</b> {message}");
        }

        public static void LogError(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string filePath = "")
        {
            UnityEngine.Debug.LogError($"<b>{ExtractClassName(filePath)}.{member}:</b> {message}");
        }

        private static string ExtractClassName(string filePath)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
    }
}