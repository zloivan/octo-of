using System.Runtime.CompilerServices;

namespace OnlyFarms.Utilities
{
    public class OFLogger
    {
        [System.Diagnostics.Conditional("ENABLE_LOG")]
        public static void Log(string message, UnityEngine.Object context = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string filePath = "")
        {
            UnityEngine.Debug.Log($"<b>{ExtractClassName(filePath)}.{member}:</b> {message}", context);
        }

        [System.Diagnostics.Conditional("ENABLE_LOG")]
        public static void LogWarning(string message, UnityEngine.Object context = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string filePath = "")
        {
            UnityEngine.Debug.LogWarning($"<b>{ExtractClassName(filePath)}.{member}:</b> {message}", context);
        }

        public static void LogError(string message, UnityEngine.Object context = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string filePath = "")
        {
            UnityEngine.Debug.LogError($"<b>{ExtractClassName(filePath)}.{member}:</b> {message}", context);
        }

        private static string ExtractClassName(string filePath)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
    }
}