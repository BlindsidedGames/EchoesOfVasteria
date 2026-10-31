using System.Runtime.InteropServices;
using Blindsided.SaveData;
using UnityEngine;

namespace TimelessEchoes.Stats
{
    // Presentation transport only. The Watch never owns a save or awards progression.
    public static class WatchCompanionPrototype
    {
        private static object forecast;
        private static string forecastMap;
        private static GameData forecastOwner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            forecast = null;
            forecastMap = null;
            forecastOwner = null;
        }

        internal static void RememberForecast(string map, object value, GameData owner)
        {
            forecastMap = map;
            forecast = value;
            forecastOwner = owner;
        }

        internal static object GetForecast(string map, GameData owner) =>
            ReferenceEquals(owner, forecastOwner) && map == forecastMap ? forecast : null;

        internal static void ClearForecast() => Reset();

#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
        [DllImport("__Internal")] private static extern void EOVWatchCompanion_Publish(string json);
        [DllImport("__Internal")] private static extern int EOVWatchCompanion_ConsumeRefreshRequest();
        public static bool Supported => true;
#else
        public static bool Supported => false;
#endif

        public static bool ConsumeRefreshRequest()
        {
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
            return EOVWatchCompanion_ConsumeRefreshRequest() != 0;
#else
            return false;
#endif
        }

        public static void Publish(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
            EOVWatchCompanion_Publish(json);
#endif
        }
    }
}
