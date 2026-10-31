using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TimelessEchoes.Stats
{
    public static class BackgroundRunPrototype
    {
        public static bool Available
        {
            get
            {
#if EOV_BACKGROUND_PROTOTYPE
                return true;
#else
                return Environment.GetCommandLineArgs().Contains("-eov-background-prototype");
#endif
            }
        }

        public static bool OptedIn
        {
            get => PlayerPrefs.GetInt("Gameplay.BackgroundRunPrototype", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("Gameplay.BackgroundRunPrototype", value ? 1 : 0);
                PlayerPrefs.Save();
                if (!value) End();
            }
        }

        public static bool Enabled
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
                if (EOVLiveActivity_IsDemoLaunch() != 0) return true;
#endif
                return Environment.GetCommandLineArgs().Contains("-eov-background-prototype") ||
                    Environment.GetCommandLineArgs().Contains("-eov-background-demo") || OptedIn;
            }
        }

        public static bool Demo
        {
            get
            {
                if (!Enabled) return false;
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
                // Unity's iOS IL2CPP host need not forward argv into System.Environment.
                return EOVLiveActivity_IsDemoLaunch() != 0;
#else
                return Environment.GetCommandLineArgs().Contains("-eov-background-demo");
#endif
            }
        }

#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
        [DllImport("__Internal")] private static extern void EOVLiveActivity_End();
        [DllImport("__Internal")] private static extern int EOVLiveActivity_IsDemoLaunch();
        [DllImport("__Internal")] private static extern int EOVLiveActivity_IsForeground();
#endif
        public static bool NativeForeground
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
                return EOVLiveActivity_IsForeground() != 0;
#else
                return true;
#endif
            }
        }

        public static void Publish(string map, BackgroundRunEstimator.Result forecast, double currentDistance, double resources, WatchForecastTimeline timeline = null)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = new
            {
                schemaVersion = 1, sourceID = Guid.NewGuid().ToString("N"), mapId = map, mapTitle = (map == "Haloween" ? "Hollow" : map) + (Demo ? " · demo" : ""),
                sampleCount = forecast.SampleCount, generatedAt = now, currentDistance,
                projectedDistance = forecast.Distance, reapCap = forecast.BaseCap,
                projectedReaps = forecast.Reaps, projectedTasks = forecast.Tasks, projectedKills = forecast.Kills,
                projectedResources = resources, forecastEnd = now + Math.Max(1, forecast.Seconds), status = forecast.Status
            };
            if (timeline != null) timeline.generatedAt = now;
            WatchCompanionPrototype.RememberForecast(map, payload, Blindsided.Oracle.oracle?.saveData, timeline);
        }

        public static void End()
        {
            WatchCompanionPrototype.ClearForecast();
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
            EOVLiveActivity_End();
#endif
        }
    }
}
