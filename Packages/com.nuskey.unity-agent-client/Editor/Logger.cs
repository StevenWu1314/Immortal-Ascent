using System;

namespace UnityAgentClient
{
    internal static class Logger
    {
        const string Prefix = "[UnityAgentClient]";

        public static void LogVerbose(string message)
        {
            var settings = AgentSettingsProvider.Load();
            if (settings == null || !settings.VerboseLogging) return;

            UnityEngine.Debug.Log($"{Prefix} {message}");
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError($"{Prefix} {message}");
        }

        internal static void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning($"{Prefix} {message}");
        }
    }
}