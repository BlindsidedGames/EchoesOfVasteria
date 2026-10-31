using System;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;

namespace TimelessEchoes.Stats
{
    public static class BackgroundRunPrototype
    {
        public static bool Enabled
        {
            get
            {
#if EOV_BACKGROUND_PROTOTYPE
                return true;
#else
                return Environment.GetCommandLineArgs().Contains("-eov-background-prototype") ||
                    PlayerPrefs.GetInt("Gameplay.BackgroundRunPrototype", 0) == 1;
#endif
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
        [DllImport("__Internal")] private static extern void EOVLiveActivity_StartOrUpdate(string json);
        [DllImport("__Internal")] private static extern void EOVLiveActivity_End();
        [DllImport("__Internal")] private static extern int EOVLiveActivity_IsSupported();
        [DllImport("__Internal")] private static extern int EOVLiveActivity_IsDemoLaunch();
#endif
        public static void Publish(string map, BackgroundRunEstimator.Result forecast, double currentDistance, double resources)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var json = JsonConvert.SerializeObject(new
            {
                schemaVersion = 1, mapId = map, mapTitle = (map == "Haloween" ? "Hollow" : map) + (Demo ? " · demo" : ""),
                sampleCount = forecast.SampleCount, generatedAt = now, currentDistance,
                projectedDistance = forecast.Distance, reapCap = forecast.BaseCap,
                projectedReaps = forecast.Reaps, projectedTasks = forecast.Tasks, projectedKills = forecast.Kills,
                projectedResources = resources, forecastEnd = now + Math.Max(1, forecast.Seconds), status = forecast.Status
            });
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
            if (EOVLiveActivity_IsSupported() != 0) EOVLiveActivity_StartOrUpdate(json);
#endif
        }

        public static void End()
        {
#if UNITY_IOS && !UNITY_EDITOR && EOV_BACKGROUND_PROTOTYPE
            EOVLiveActivity_End();
#endif
        }
    }
}
