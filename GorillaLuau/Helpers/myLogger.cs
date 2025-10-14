using UnityEngine;

namespace GorillaLuau.Helpers
{
    public class myLogger
    {
        public static void Log(string message)
        {
            Debug.Log($"[GorillaLuau] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[GorillaLuau] {message}");
        }
    }
}
