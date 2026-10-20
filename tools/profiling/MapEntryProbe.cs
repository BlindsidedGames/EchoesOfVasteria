#if EOV_STANDALONE_PROFILE || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using Newtonsoft.Json;
using TimelessEchoes;
using TimelessEchoes.MapGeneration;
using UnityEngine;
using UnityEngine.Profiling;

public sealed class MapEntryProbe : MonoBehaviour
{
    static string output;
    readonly List<object> results = new();
    int scans, frames;
    float maximumFrame;
    long peak;
    bool measuring;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Application.isEditor && Environment.GetEnvironmentVariable("EOV_EDITOR_MAP_PROBE") != "1") return;
        output = Environment.GetEnvironmentVariable("EOV_PLAYER_PROFILE");
        if (string.IsNullOrEmpty(output) || Application.companyName != "Blindsided Profiling")
            throw new Exception("Explicit isolated profiling identity/output required.");
        Directory.CreateDirectory(output);
        typeof(SaveManager).GetField("rootPathOverride", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, Path.Combine(output, "isolated-progress"));
        PlayerPrefs.DeleteAll();
        Application.runInBackground = true;
        DontDestroyOnLoad(new GameObject("Map entry probe", typeof(MapEntryProbe)));
    }
    void Update()
    {
        if (measuring)
        {
            frames++;
            maximumFrame = Mathf.Max(maximumFrame, Time.unscaledDeltaTime * 1000);
            peak = Math.Max(peak, Profiler.GetTotalAllocatedMemoryLong());
        }
        if (Time.realtimeSinceStartup > 180) { File.WriteAllText(Path.Combine(output,"timeout.txt"),"Map entry watchdog"); Stop(); }
    }
    void Scanned(AstarPath path) => scans++;
    IEnumerator Start()
    {
        while (!GameManager.Instance || !Oracle.oracle || !GameManager.Instance.IsInTown) yield return null;
        yield return new WaitForSecondsRealtime(2);
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        AstarPath.OnPostScan += Scanned;
        foreach (var name in new[] { "Farmlands", "Farmlands", "River", "Woods", "Mines", "Haloween", "Haloween" })
        {
            var map = Resources.FindObjectsOfTypeAll<MapGenerationConfig>().First(x => x.name == name);
            map.tilemapChunkSettings.randomizeSeed = false;
            map.tilemapChunkSettings.seed = 9173;
            UnityEngine.Random.InitState(9173);
            scans = frames = 0; maximumFrame = 0;
            peak = Profiler.GetTotalAllocatedMemoryLong();
            var start = Time.realtimeSinceStartup;
            var previousMap = GameManager.Instance.CurrentMap;
            measuring = true;
            if (!GameManager.Instance.BeginAdventure(map)) throw new Exception("Map entry denied: " + name);
            var loadingVisibleImmediately = GameManager.Instance.RunLoadingActive;
            do { yield return null; }
            while (GameManager.Instance.RunLoadingActive || GameManager.Instance.IsInTown || !GameManager.Instance.CurrentMap || GameManager.Instance.CurrentMap == previousMap ||
                !GameManager.Instance.CurrentMap.GetComponentInChildren<SegmentedMapGenerator>().InitialGenerationComplete);
            measuring = false;
            var generator = GameManager.Instance.CurrentMap.GetComponentInChildren<SegmentedMapGenerator>();
            if (!generator || !generator.InitialGenerationComplete) throw new Exception("Incomplete map: " + name);
            var controller = GameManager.Instance.CurrentMap.GetComponentInChildren<TimelessEchoes.Tasks.TaskController>();
            var settings = map.taskGeneratorSettings;
            var hasAuthoredTasks = new[] { settings.woodcutting, settings.mining, settings.farming, settings.fishing, settings.looting }
                .Any(category => category.weight > 0 && category.tasks.Count > 0) || settings.npcTasks.Count > 0;
            if (!controller || !controller.hero || (hasAuthoredTasks && controller.tasks.Count == 0) || !controller.hero.MovementController.LogicActive)
                throw new Exception("Map did not resume active gameplay: " + name);
            results.Add(new { map = name, seconds = Time.realtimeSinceStartup - start, frames, scans, maximumFrameMs = maximumFrame, peakAllocated = peak,
                loadingVisibleImmediately, tasks = controller.tasks.Count, gameplayActive = controller.hero.MovementController.LogicActive });
            File.WriteAllText(Path.Combine(output,"map-entry.json"), JsonConvert.SerializeObject(results, Formatting.Indented));
            yield return new WaitForSecondsRealtime(1);
        }
        AstarPath.OnPostScan -= Scanned;
        File.WriteAllText(Path.Combine(output,"complete.json"),JsonConvert.SerializeObject(new { unity=Application.unityVersion, source=Environment.GetEnvironmentVariable("EOV_PROFILE_SOURCE"), development=Debug.isDebugBuild, graphics=SystemInfo.graphicsDeviceName }));
        Stop();
    }
    static void Stop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
#endif
