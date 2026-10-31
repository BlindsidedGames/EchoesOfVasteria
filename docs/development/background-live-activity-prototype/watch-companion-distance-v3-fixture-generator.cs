#if UNITY_EDITOR && UNITY_IOS
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Blindsided.SaveData;
using TimelessEchoes.Stats;
using UnityEngine;

public static class LocalWatchTimelineFixtures
{
    private static string Root => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Artifacts/LiveActivityPrototype");
    public static void ExportAndBuild()
    {
        Export();
        LocalLiveActivitySimulator.Export();
    }
    public static void Export()
    {
        if (!Application.dataPath.Contains("mac-live-activity-prototype")) throw new InvalidOperationException("Isolated checkout required");
        var reap = Input(20); Write("reaping", reap);
        var cap = Input(10); cap.Run.Distance = 99; cap.BaseCap = 50; cap.CapModifiers = (_, _) => (2, 0); Write("effective-cap", cap);
        var hollow = Input(20); hollow.Run.Map = "Haloween"; hollow.DistanceReaping = false; Write("hollow", hollow);
        var death = Input(20); death.MaxHealth = 2; death.Pressure = (_, _) => 1;
        death.Samples[0].DamageTaken = 100; Write("death", death);
        var bounded = Input(900); bounded.BaseCap = 10000; bounded.CapModifiers = (t, _) => (1, t); Write("bounded", bounded);
    }
    private static BackgroundRunEstimator.Input Input(double seconds) => new()
    {
        Run = new RunSaveData { Map = "River", Distance = 8, Stats = new RunSaveData.Statistics { Resources = 777 } },
        Seconds = seconds, BaseCap = 10, StartingDistance = 2, RestartSeconds = 5, MaxHealth = 100,
        Samples = new[] { new BackgroundRunSample { Seconds = 100, Distance = 100, EndDistance = 100, Tasks = 100, Kills = 50 } },
        Pressure = (_, _) => 0
    };
    private static void Write(string name, BackgroundRunEstimator.Input input)
    {
        var forecast = BackgroundRunEstimator.Forecast(input);
        var timeline = WatchForecastTimeline.Create(input, forecast, 2, 3);
        BackgroundRunPrototype.Publish(input.Run.Map, forecast, input.Run.Distance, forecast.Tasks * 2 + forecast.Kills * 3, timeline);
        var payload = JObject.Parse(File.ReadAllText(Path.Combine(Root, "distance-v3-base-real.json")));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        payload["generatedAt"] = now; payload["state"] = "run"; payload["backgroundProgressEnabled"] = true;
        payload["forecast"] = JValue.CreateNull(); payload["timeline"] = timeline == null ? JValue.CreateNull() : JToken.FromObject(timeline);
        payload["run"] = JObject.FromObject(new { mapId = input.Run.Map, mapTitle = input.Run.Map == "Haloween" ? "Hollow" : input.Run.Map,
            distance = (double)input.Run.Distance, cap = forecast.Steps.FirstOrDefault()?.Cap ?? input.BaseCap,
            distanceReaping = input.DistanceReaping, healthFraction = 1, dead = false, reaped = false,
            durationSeconds = 0, tasks = 0, kills = 0, resources = input.Run.Stats.Resources });
        var json = payload.ToString(Formatting.None);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 32768) throw new InvalidOperationException("Fixture exceeds native payload cap");
        File.WriteAllText(Path.Combine(Root, "distance-v3-managed-" + name + ".json"), json);
        File.WriteAllText(Path.Combine(Root, "distance-v3-estimator-" + name + ".json"), JsonConvert.SerializeObject(new {
            description = "Explicit fixture: actual BackgroundRunEstimator output, not a real gameplay/transport event",
            input = new { input.Seconds, input.BaseCap, input.StartingDistance, input.RestartSeconds, input.DistanceReaping, input.MaxHealth,
                startingDistance = input.Run.Distance, actualRunResources = input.Run.Stats.Resources, resourcesPerTask = 2, resourcesPerKill = 3 },
            forecast, timeline }, Formatting.Indented));
    }
}
#endif
