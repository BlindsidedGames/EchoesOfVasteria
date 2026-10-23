#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using Sirenix.Serialization;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.UI;
using UnityEditor;
using UnityEngine;

namespace TimelessEchoes.Editor.Capture
{
    public sealed class ScreenshotStudio : EditorWindow
    {
        private string folder = "";
        private ScreenshotScenario scenario = new();
        private MapGenerationConfig map;
        private int target;
        private bool expanded, bothPortraitViews = true;
        private Vector2 scroll;
        private string error;

        [MenuItem("Tools/Screenshots/Studio")]
        public static void Open() => GetWindow<ScreenshotStudio>("Screenshot Studio");
        private void OnEnable()
        {
            folder = EditorPrefs.GetString("EOV.ScreenshotStudio.LastFolder", "");
            if (File.Exists(Path.Combine(folder, "scenario.json"))) Load(folder);
        }
        private void OnInspectorUpdate() => Repaint();
        private void Guard(Action action)
        {
            try { error = null; action(); }
            catch (Exception e) { error = e.Message; Debug.LogException(e); }
        }
        private void Load(string path)
        {
            scenario = ScreenshotScenario.Read(path); folder = path;
            map = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(AssetDatabase.GUIDToAssetPath(scenario.mapGuid));
            EditorPrefs.SetString("EOV.ScreenshotStudio.LastFolder", path);
        }
        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Screenshot scenarios", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Capture real game screens from a private save copy. Store targets only set the viewport; game UI proportions and camera rules are unchanged. No video or store uploads.", MessageType.Info);
            if (GUILayout.Button("Open scenario folder…")) Guard(() =>
            {
                var path = EditorUtility.OpenFolderPanel("Open screenshot scenario", DefaultRoot, "");
                if (!string.IsNullOrEmpty(path)) Load(path);
            });
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || Oracle.oracle == null || !Oracle.oracle.HasLoadedCurrentSlotData))
                if (GUILayout.Button("New scenario from current progress…")) Guard(CreateFromCurrent);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                if (GUILayout.Button("New scenario from a copied save slot…")) Guard(ImportSlot);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.IsNullOrEmpty(folder) ? "No scenario selected" : folder, EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(folder) || ScreenshotSession.Busy))
            {
                scenario.name = EditorGUILayout.TextField("Name", scenario.name);
                map = (MapGenerationConfig)EditorGUILayout.ObjectField("Map (empty = town)", map, typeof(MapGenerationConfig), false);
                scenario.seed = EditorGUILayout.IntField("Map seed", scenario.seed);
                scenario.runFrames = EditorGUILayout.IntSlider("Run frames at 60 fps", scenario.runFrames, 0, 1800);
                scenario.lightMode = EditorGUILayout.Toggle("Light mode", scenario.lightMode);
                var windows = new[] { "None" }.Concat(Enum.GetNames(typeof(TownWindowManager.Window))).ToArray();
                scenario.window = windows[EditorGUILayout.Popup("Open window", Math.Max(0, Array.IndexOf(windows, scenario.window)), windows)];
                if (GUILayout.Button("Save recipe")) Guard(SaveRecipe);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying && !ScreenshotSession.Active))
                    if (GUILayout.Button("Load / reload isolated scenario")) Guard(() => { SaveRecipe(); ScreenshotSession.LoadOrRestart(folder); });
                if (GUILayout.Button("Create the six-shot template set…")) Guard(CreateShotSet);
                if (GUILayout.Button("Clear saved composition (keep progress)")) Guard(() =>
                { scenario.poses.Clear(); scenario.scrolls.Clear(); scenario.selections.Clear(); scenario.Write(folder); });
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
            target = EditorGUILayout.Popup("Store / device", target, ScreenshotSizes.All.Select(s => s.label).ToArray());
            expanded = EditorGUILayout.Toggle("Expanded portrait", expanded);
            bothPortraitViews = EditorGUILayout.Toggle("Both portrait views in batch", bothPortraitViews);
            using (new EditorGUI.DisabledScope(!ScreenshotSession.Ready || ScreenshotSession.Busy))
            {
                if (GUILayout.Button("Apply viewport")) Guard(() => ScreenshotSession.Preview(ScreenshotSizes.All[target], expanded));
                if (GUILayout.Button("Save current composition / scroll positions")) Guard(() => { ScreenshotSession.SaveCheckpoint(); Load(folder); });
                if (GUILayout.Button("Reset to saved composition")) Guard(ScreenshotSession.RestoreCheckpoint);
                if (GUILayout.Button("Advance 1 frame")) Guard(() => ScreenshotSession.Advance(1));
                if (GUILayout.Button("Advance 30 frames")) Guard(() => ScreenshotSession.Advance(30));
                if (GUILayout.Button("Capture selected")) Guard(() => ScreenshotSession.CaptureSelected(ScreenshotSizes.All[target], expanded));
                if (GUILayout.Button("Capture all store sizes")) Guard(() => ScreenshotSession.CaptureAll(bothPortraitViews));
            }
            if (ScreenshotSession.Active && GUILayout.Button("End private session")) ScreenshotSession.End();
            if (GUILayout.Button("Open output folder")) { Directory.CreateDirectory(OutputRoot); EditorUtility.RevealInFinder(OutputRoot); }
            EditorGUILayout.HelpBox(ScreenshotSession.Status, MessageType.None);
            if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
            EditorGUILayout.EndScrollView();
        }
        internal static string DefaultRoot => Path.GetFullPath("Artifacts/ScreenshotStudio/scenarios");
        internal static string OutputRoot => Path.GetFullPath("Artifacts/ScreenshotStudio/captures");
        private void SaveRecipe()
        {
            scenario.mapGuid = map ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(map)) : "";
            scenario.Write(folder);
        }
        private string NewFolder()
        {
            Directory.CreateDirectory(DefaultRoot);
            var path = EditorUtility.SaveFolderPanel("New screenshot scenario", DefaultRoot, "scenario");
            if (string.IsNullOrEmpty(path)) return null;
            if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
                throw new IOException("Choose an empty scenario folder. Existing snapshots are never overwritten.");
            Directory.CreateDirectory(path); return path;
        }
        private void CreateFromCurrent()
        {
            var path = NewFolder(); if (path == null) return;
            Create(path, CurrentSaveCodec.Clone(Oracle.oracle.saveData));
        }
        private void ImportSlot()
        {
            var source = EditorUtility.OpenFolderPanel("Select a save slot containing snapshot.bin", "", "");
            if (string.IsNullOrEmpty(source)) return;
            if (!File.Exists(Path.Combine(source, "snapshot.bin"))) throw new IOException("Choose a save slot folder containing snapshot.bin.");
            var path = NewFolder(); if (path == null) return;
            // Read only a copy: recovery/migration must never write into the supplied slot.
            var copyRoot = Path.Combine(path, "import");
            ScreenshotSession.CopyDirectory(source, Path.Combine(copyRoot, "Saves", "Save1"));
            var field = ScreenshotSession.SaveRootField;
            var previous = field.GetValue(null);
            try
            {
                field.SetValue(null, copyRoot);
                var result = SaveManager.Instance.LoadDetailedAsync("Save1").GetAwaiter().GetResult();
                if (!result.Succeeded) throw new InvalidDataException(result.Status + ": " + result.Diagnostic);
                Create(path, result.Data);
            }
            finally { field.SetValue(null, previous); }
        }
        private void Create(string path, GameData data)
        {
            folder = path;
            scenario = new ScreenshotScenario { name = Path.GetFileName(path), capturedUnityVersion = Application.unityVersion, lightMode = TimelessEchoes.UI.Toolkit.ToolkitColorTheme.IsLight };
            var bytes = CurrentSaveCodec.Serialize(data);
            File.WriteAllBytes(Path.Combine(path, "progress.bin"), bytes);
            scenario.progressSha256 = ScreenshotSession.Hash(bytes);
            scenario.Write(path); Load(path);
        }
        private void CreateShotSet()
        {
            var destination = EditorUtility.SaveFolderPanel("New six-shot template set", DefaultRoot, "store-shots");
            if (string.IsNullOrEmpty(destination)) return;
            if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
                throw new IOException("Choose an empty folder for the shot set.");
            var shots = new[] {
                ("01-adventure", "Adventure with echoes", "River", "None"),
                ("02-forge", "Forge", "", "Forge"),
                ("03-fields", "Fields", "", "Farm"),
                ("04-cauldron", "Cauldron", "", "Cauldron"),
                ("05-equipment", "Equipment", "", "Inventory"),
                ("06-later-map", "Later-map adventure", "Haloween", "None") };
            foreach (var shot in shots)
            {
                var path = Path.Combine(destination, shot.Item1); Directory.CreateDirectory(path);
                File.Copy(Path.Combine(folder, "progress.bin"), Path.Combine(path, "progress.bin"));
                new ScreenshotScenario { name = shot.Item2, window = shot.Item4, progressSha256 = scenario.progressSha256,
                    lightMode = scenario.lightMode,
                    mapGuid = string.IsNullOrEmpty(shot.Item3) ? "" : AssetDatabase.AssetPathToGUID("Assets/Scriptables/MapSettings/" + shot.Item3 + ".asset") }.Write(path);
            }
            EditorUtility.RevealInFinder(destination);
        }
    }
}
#endif
