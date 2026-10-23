#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Blindsided;
using Blindsided.SaveData;
using Sirenix.Serialization;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.UI;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace TimelessEchoes.Editor.Capture
{
    /// <summary>Editor-only, opt-in capture sandbox. Never shares a save root with a normal Play session.</summary>
    [InitializeOnLoad]
    public static class ScreenshotSession
    {
        internal const string ActiveKey = "EOV.ScreenshotStudio.Active";
        private const string FolderKey = "EOV.ScreenshotStudio.Folder";
        private const string RootKey = "EOV.ScreenshotStudio.Root";
        private const string SceneKey = "EOV.ScreenshotStudio.Scenes";
        private const string SeedKey = "EOV.ScreenshotStudio.SeedBackup";
        internal static readonly FieldInfo SaveRootField = typeof(SaveManager).GetField("rootPathOverride", BindingFlags.NonPublic | BindingFlags.Static);
        private static ScreenshotScenario scenario;
        private static string folder, output;
        private static double deadline;
        private static int phase, frames, settling;
        private static int oldCaptureRate;
        private static float oldTimeScale;
        private static uint oldWidth, oldHeight;
        private static readonly List<(Behaviour component, bool enabled)> disabled = new();
        private static readonly List<(Animator animator, float speed)> animators = new();
        private static readonly List<(Hero.HeroBase hero, bool active)> actors = new();
        private static readonly Queue<(ScreenshotSize size, bool expanded)> queue = new();
        private static readonly List<CaptureRecord> captures = new();
        private static (ScreenshotSize size, bool expanded) current;
        private static string pendingFile;
        private static bool oldLight;
        public static bool Active => SessionState.GetBool(ActiveKey, false);
        public static bool Ready { get; private set; }
        public static bool Busy => phase != 0;
        public static string Status { get; private set; } = "No capture session. Open a scenario or create one from progress.";

        [Serializable] private class SceneRecord { public string path; public bool loaded, active; }
        [Serializable] private class Scenes { public SceneRecord[] entries; }
        [Serializable] private class SeedBackup { public string guid; public int seed; public bool random; }
        [Serializable] private class Manifest { public string scenario, source, unity, progressSha256, language, captureToolHash, runHudSourceHash; public bool lightMode; public int seed, runFrames; public CaptureRecord[] captures; }
        [Serializable] private class CaptureRecord { public string file, preset, view, sha256; public int width, height; public Rect viewport; }

        static ScreenshotSession()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && Active) Cleanup();
            };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void BeforeScene()
        {
            if (!Active) return;
            var root = SessionState.GetString(RootKey, "");
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) throw new InvalidOperationException("Missing private screenshot save root.");
            SaveRootField.SetValue(null, root);
            if (Path.GetFullPath(root).Equals(Path.GetFullPath(Application.persistentDataPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Screenshot capture cannot use the player's save directory.");
            folder = SessionState.GetString(FolderKey, "");
            scenario = ScreenshotScenario.Read(folder);
            phase = 1; deadline = EditorApplication.timeSinceStartup + 120;
            oldTimeScale = Time.timeScale; oldCaptureRate = Time.captureFramerate;
            oldLight = SessionState.GetBool("EOV.ScreenshotStudio.OriginalLight", ToolkitColorTheme.IsLight);
            ToolkitColorTheme.SetLight(scenario.lightMode);
            PlayModeWindow.GetRenderingResolution(out oldWidth, out oldHeight);
            SessionState.SetFloat("EOV.ScreenshotStudio.TimeScale", oldTimeScale);
            SessionState.SetInt("EOV.ScreenshotStudio.CaptureRate", oldCaptureRate);
            SessionState.SetInt("EOV.ScreenshotStudio.Width", (int)oldWidth);
            SessionState.SetInt("EOV.ScreenshotStudio.Height", (int)oldHeight);
            Time.captureFramerate = 60;
            Status = "Loading the scenario's private save copyÃ¢â‚¬Â¦";
        }

        public static void Begin(string scenarioFolder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("End the current Play session before loading a scenario.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your scene edits before starting the capture sandbox.");
            scenario = ScreenshotScenario.Read(scenarioFolder); folder = Path.GetFullPath(scenarioFolder);
            var bytes = File.ReadAllBytes(Path.Combine(folder, "progress.bin"));
            if (Hash(bytes) != scenario.progressSha256) throw new InvalidDataException("Progress snapshot hash mismatch. The scenario was not loaded.");
            var data = SerializationUtility.DeserializeValue<GameData>(bytes, DataFormat.Binary);
            // Freeze elapsed-away progression at import; never change the source payload.
            data.DateQuitString = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            data.OfflineTime = 0;
            var root = Path.GetFullPath(Path.Combine("Library", "ScreenshotStudio", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(root);
            var previous = SaveRootField.GetValue(null);
            try
            {
                SaveRootField.SetValue(null, root);
                for (var slot = 1; slot <= 3; slot++)
                {
                    var result = SaveManager.Instance.ReplaceSlotDetailedAsync(CurrentSaveCodec.Clone(data), "Save" + slot).GetAwaiter().GetResult();
                    if (!result.Succeeded) throw new IOException(result.Error);
                }
            }
            finally { SaveRootField.SetValue(null, previous); }
            SessionState.SetString(SceneKey, JsonUtility.ToJson(new Scenes { entries = EditorSceneManager.GetSceneManagerSetup()
                .Select(s => new SceneRecord { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }));
            SessionState.SetString(FolderKey, folder); SessionState.SetString(RootKey, root);
            SessionState.SetBool("EOV.ScreenshotStudio.OriginalLight", ToolkitColorTheme.IsLight);
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity");
            EditorApplication.isPlaying = true;
        }
        public static void LoadOrRestart(string scenarioFolder)
        {
            if (!EditorApplication.isPlaying) { Begin(scenarioFolder); return; }
            if (!Active) throw new InvalidOperationException("End the normal Play session before loading a screenshot scenario.");
            SessionState.SetString("EOV.ScreenshotStudio.Next", Path.GetFullPath(scenarioFolder));
            End();
        }

        private static void Tick()
        {
            if (!Active || !EditorApplication.isPlaying || scenario == null) return;
            try
            {
                if (phase != 0 && EditorApplication.timeSinceStartup > deadline)
                    throw new TimeoutException("Capture stopped: game/menu/viewport did not reach the requested state. Check tutorial state and Console.");
                if (phase == 1)
                {
                    if (!Oracle.oracle || !Oracle.oracle.HasLoadedCurrentSlotData || !GameManager.Instance || !TownWindowManager.Instance) return;
                    if (DismissReturningPrompt()) return;
                    if (HasBlockingDialog()) { Status = "Dismiss the introduction before staging screenshots."; return; }
                    Random.InitState(scenario.seed);
                    TimelessEchoes.Platform.EditorCaptureRandom.Reset(scenario.seed);
                    if (!string.IsNullOrEmpty(scenario.mapGuid))
                    {
                        var map = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(AssetDatabase.GUIDToAssetPath(scenario.mapGuid));
                        if (!map) throw new InvalidDataException("Scenario map is missing.");
                        SessionState.SetString(SeedKey, JsonUtility.ToJson(new SeedBackup { guid = scenario.mapGuid, seed = map.tilemapChunkSettings.seed, random = map.tilemapChunkSettings.randomizeSeed }));
                        map.tilemapChunkSettings.seed = scenario.seed; map.tilemapChunkSettings.randomizeSeed = false;
                        if (!GameManager.Instance.BeginAdventure(map)) throw new InvalidOperationException("Map entry was refused. Use a snapshot with its lessons and map unlocked.");
                    }
                    phase = 2; frames = 0; Status = "Preparing real game sceneÃ¢â‚¬Â¦";
                }
                else if (phase == 2)
                {
                    if (GameManager.Instance.RunLoadingActive) return;
                    if (!string.IsNullOrEmpty(scenario.mapGuid) && !GameManager.Instance.CurrentMap) return;
                    var generation = Object.FindAnyObjectByType<SegmentedMapGenerator>();
                    if (generation && !generation.InitialGenerationComplete) return;
                    if (DismissReturningPrompt() || HasBlockingDialog()) return;
                    RestoreSeed();
                    Random.InitState(scenario.seed);
                    phase = 3; frames = 0; Status = "Advancing the saved scene recipeÃ¢â‚¬Â¦";
                }
                else if (phase == 3)
                {
                    // Count rendered game frames, not editor-update calls.
                    if (lastFrame == Time.frameCount) return;
                    lastFrame = Time.frameCount;
                    if (++frames < scenario.runFrames) return;
                    Freeze(); Ready = true; phase = 0;
                    ApplyMenu(); RestoreCheckpoint();
                    Status = "Ready. Simulation is frozen; menus and layout remain live.";
                }
                else if (phase == 4)
                {
                    if (lastFrame == Time.frameCount) return;
                    lastFrame = Time.frameCount;
                    if (--frames > 0) return;
                    Freeze(); phase = 0; Ready = true; Status = "Ready. Save composition to retain this frame.";
                }
                else if (phase == 5)
                {
                    if (lastFrame == Time.frameCount) return;
                    lastFrame = Time.frameCount;
                    // Open/reopen after the new viewport has propagated through the
                    // responsive dock. Opening at the old size can preserve stale UI state.
                    if (settling == 4)
                    {
                        ApplyMenu();
                        ToolkitResponsiveLayout.SetFocusView(current.expanded);
                    }
                    if (++settling < 12) return;
                    // Resizing the editor's drawable can lag a fast batch by more
                    // than twelve frames. Retry within the existing deadline.
                    if (UnityEngine.Device.Screen.width != current.size.width || UnityEngine.Device.Screen.height != current.size.height)
                    {
                        if (settling % 12 == 0)
                            PlayModeWindow.SetCustomRenderingResolution((uint)current.size.width, (uint)current.size.height, "Store screenshot");
                        return;
                    }
                    ValidateViewport(current.size, current.expanded);
                    RestoreCheckpoint();
                    ScreenCapture.CaptureScreenshot(pendingFile);
                    phase = 6;
                }
                else if (phase == 6 && File.Exists(pendingFile))
                {
                    var image = new Texture2D(2, 2);
                    try
                    {
                        if (!image.LoadImage(File.ReadAllBytes(pendingFile)) || image.width != current.size.width || image.height != current.size.height)
                            throw new InvalidDataException("Captured pixels do not match the requested size.");
                        captures.Add(new CaptureRecord { file = Path.GetFileName(pendingFile), preset = current.size.id,
                            view = current.size.Portrait ? (current.expanded ? "expanded" : "compact") : "landscape",
                            width = image.width, height = image.height, sha256 = Hash(File.ReadAllBytes(pendingFile)), viewport = ToolkitResponsiveLayout.WorldPixels(new Vector2Int(image.width, image.height), UnityEngine.Device.Screen.safeArea) });
                    }
                    finally { Object.DestroyImmediate(image); }
                    WriteManifest(); NextCapture();
                }
            }
            catch (Exception e)
            {
                RestoreSeed(); queue.Clear(); phase = 0; Ready = false;
                Status = e.Message; Debug.LogException(e);
            }
        }
        private static int lastFrame = -1;
        private static bool HasBlockingDialog() => Object.FindObjectsByType<ToolkitDialogScreen>().Any(d => d.IsVisible) ||
            Object.FindObjectsByType<ToolkitIntroScreen>().Any(d => d.IsVisible) || ToolkitMeetingScreen.HasVisibleMeeting;
        private static bool DismissReturningPrompt()
        {
            var general = Oracle.oracle?.saveData?.General;
            if (general == null || !general.ReturningPlayerIntroductionComplete || general.ReturningPlayerLessonsChoiceMade) return false;
            foreach (var dialog in Object.FindObjectsByType<ToolkitDialogScreen>())
            {
                if (!dialog.IsVisible) continue;
                var button = dialog.GetComponent<UIDocument>().rootVisualElement.Q<Button>("dialog-secondary");
                if (button == null || !button.enabledInHierarchy) continue;
                Click(button);
                return true;
            }
            return false;
        }

        private static void Freeze()
        {
            Time.timeScale = 0;
            disabled.Clear(); animators.Clear();
            foreach (var animator in Object.FindObjectsByType<Animator>())
            { animators.Add((animator, animator.speed)); animator.speed = 0; }
            // Do not disable heroes/tasks: their OnDisable methods release claims and
            // engagements. Only stop the known unscaled growth owner and buff autocast.
            foreach (var farm in Object.FindObjectsByType<TimelessEchoes.Farming.FarmService>())
                if (farm.enabled) { disabled.Add((farm, true)); farm.enabled = false; }
            TimelessEchoes.Buffs.BuffManager.Instance?.Pause();
            actors.Clear();
            foreach (var hero in Object.FindObjectsByType<Hero.HeroBase>())
                if (hero.MovementController)
                {
                    actors.Add((hero, hero.MovementController.LogicActive));
                    hero.MovementController.SetActiveState(false, hero.Animator);
                }
        }
        private static void Thaw()
        {
            foreach (var item in disabled) if (item.component) item.component.enabled = item.enabled;
            foreach (var item in animators) if (item.animator) item.animator.speed = item.speed;
            foreach (var item in actors) if (item.hero && item.hero.MovementController) item.hero.MovementController.SetActiveState(item.active, item.hero.Animator);
            actors.Clear();
            disabled.Clear(); animators.Clear(); Time.timeScale = 1;
            TimelessEchoes.Buffs.BuffManager.Instance?.Resume();
        }
        public static void Advance(int count)
        {
            RequireReady(); Thaw(); Ready = false; phase = 4; frames = count; scenario.runFrames += count;
            deadline = EditorApplication.timeSinceStartup + 60;
        }
        private static void RequireReady()
        { if (!Active || !Ready || Busy) throw new InvalidOperationException("Wait for an isolated scenario to become ready."); }

        private static void ApplyMenu()
        {
            TownWindowManager.Instance.CloseAllWindows();
            if (scenario.window != "None")
                TownWindowManager.Instance.OpenWindow((TownWindowManager.Window)Enum.Parse(typeof(TownWindowManager.Window), scenario.window));
        }
        public static void Preview(ScreenshotSize size, bool expanded)
        {
            RequireReady();
            PlayModeWindow.SetCustomRenderingResolution((uint)size.width, (uint)size.height, "Store screenshot");
            if (expanded && (!size.Portrait || scenario.window != "None" || !GameManager.Instance.CurrentMap))
                throw new InvalidOperationException("Expanded portrait is only valid for an adventure with no menu open.");
            if (expanded) TownWindowManager.Instance.CloseAllWindows();
            ToolkitResponsiveLayout.SetFocusView(expanded);
        }
        internal static void ValidateViewport(ScreenshotSize size, bool expanded)
        {
            if (HasBlockingDialog()) throw new InvalidOperationException("A dialog or tutorial is covering the shot. No image was exported.");
            if (UnityEngine.Device.Screen.width != size.width || UnityEngine.Device.Screen.height != size.height)
                throw new InvalidOperationException("A simulator/device override is preventing the requested Game-view size.");
            if (size.Portrait && GameManager.Instance && GameManager.Instance.CurrentMap && ToolkitResponsiveLayout.FocusView != expanded)
                throw new InvalidOperationException("Portrait focus was reset by gameplay/tutorial state; refusing a mislabeled capture.");
            var expected = ToolkitResponsiveLayout.WorldPixels(new Vector2Int(size.width, size.height), UnityEngine.Device.Screen.safeArea);
            foreach (var box in Object.FindObjectsByType<AspectRatioBox>())
            {
                if (!box.targetCamera || !box.targetCamera.isActiveAndEnabled) continue;
                var actual = box.targetCamera.pixelRect;
                if (size.Portrait && (Mathf.Abs(actual.y - expected.y) > 1 || Mathf.Abs(actual.height - expected.height) > 1))
                    throw new InvalidOperationException("Camera viewport has not settled to the requested layout.");
            }
        }

        public static void CaptureSelected(ScreenshotSize size, bool expanded)
        { RequireReady(); StartBatch(); queue.Enqueue((size, expanded)); NextCapture(); }
        public static void CaptureAll(bool bothPortraitViews)
        {
            RequireReady(); StartBatch();
            foreach (var size in ScreenshotSizes.All)
            {
                queue.Enqueue((size, false));
                if (size.Portrait && bothPortraitViews && scenario.window == "None" && GameManager.Instance.CurrentMap)
                    queue.Enqueue((size, true));
            }
            NextCapture();
        }
        private static void StartBatch()
        {
            queue.Clear(); captures.Clear();
            output = Path.Combine(ScreenshotStudio.OutputRoot, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
        }
        private static void NextCapture()
        {
            phase = 0;
            if (queue.Count == 0) { Status = "Captured " + captures.Count + " images in " + output; return; }
            current = queue.Dequeue();
            Preview(current.size, current.expanded);
            pendingFile = Path.Combine(output, current.size.id + (current.size.Portrait ? (current.expanded ? "-expanded" : "-compact") : "") + ".png");
            settling = 0; phase = 5; deadline = EditorApplication.timeSinceStartup + 30;
            Status = "Capturing " + Path.GetFileName(pendingFile);
        }
        private static void WriteManifest()
        {
            string source;
            try { source = File.ReadAllText(".git/HEAD").Trim(); if (source.StartsWith("ref: ")) source = File.ReadAllText(Path.Combine(".git", source.Substring(5))).Trim(); }
            catch { source = "unknown (see checkout)"; }
            File.WriteAllText(Path.Combine(output, "manifest.json"), JsonUtility.ToJson(new Manifest { scenario = scenario.name,
                source = source, unity = Application.unityVersion, progressSha256 = scenario.progressSha256,
                seed = scenario.seed, runFrames = scenario.runFrames, lightMode = scenario.lightMode,
                language = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale?.Identifier.Code,
                captureToolHash = Hash(File.ReadAllBytes("Assets/Editor/ScreenshotStudio/ScreenshotSession.cs")),
                runHudSourceHash = Hash(File.ReadAllBytes("Assets/Scripts/UI/Toolkit/ToolkitRunScreen.cs")),
                captures = captures.ToArray() }, true));
            File.WriteAllText(Path.Combine(output, "index.html"), "<!doctype html><meta charset='utf-8'><title>Screenshot review</title><style>body{background:#252123;color:#eee;font:16px sans-serif}main{display:flex;flex-wrap:wrap;gap:16px}figure{margin:0}img{max-width:480px;max-height:720px}figcaption{padding:8px}</style><h1>" + System.Net.WebUtility.HtmlEncode(scenario.name) + "</h1><main>" +
                string.Join("", captures.Select(c => "<figure><a href='" + c.file + "'><img src='" + c.file + "'></a><figcaption>" + c.preset + " Ã‚Â· " + c.view + " Ã‚Â· " + c.width + " Ãƒâ€” " + c.height + "</figcaption></figure>")) + "</main>");
        }

        public static void SaveCheckpoint()
        {
            RequireReady(); scenario.poses.Clear(); scenario.scrolls.Clear(); scenario.selections.Clear();
            var recorded = new HashSet<Transform>();
            foreach (var sprite in Object.FindObjectsByType<SpriteRenderer>())
            {
                if (!sprite.gameObject.activeInHierarchy || !sprite.enabled) continue;
                for (var parent = sprite.transform.parent; parent; parent = parent.parent)
                    if (recorded.Add(parent)) scenario.poses.Add(new ShotPose { path = ObjectPath(parent), position = parent.localPosition, rotation = parent.localRotation, scale = parent.localScale });
                recorded.Add(sprite.transform);
                scenario.poses.Add(new ShotPose { path = ObjectPath(sprite.transform), hasSprite = true, position = sprite.transform.localPosition,
                    rotation = sprite.transform.localRotation, scale = sprite.transform.localScale, flipX = sprite.flipX, flipY = sprite.flipY,
                    color = sprite.color, spriteId = sprite.sprite ? GlobalObjectId.GetGlobalObjectIdSlow(sprite.sprite).ToString() : "" });
            }
            foreach (var document in Object.FindObjectsByType<UIDocument>())
            {
                document.rootVisualElement.Query<ScrollView>().ForEach(view => scenario.scrolls.Add(new ShotScroll { document = ObjectPath(document.transform), path = ElementPath(view), offset = view.scrollOffset }));
                document.rootVisualElement.Query<Button>().ForEach(button =>
                {
                    if ((button.ClassListContains("selected") || button.ClassListContains("active")) && IsVisible(button))
                        scenario.selections.Add(new ShotSelection { document = ObjectPath(document.transform), path = ElementPath(button), name = button.name });
                });
            }
            scenario.Write(folder); Status = "Saved composition (" + scenario.poses.Count + " sprite poses).";
        }
        public static void RestoreCheckpoint()
        {
            if (scenario == null) return;
            var transforms = Object.FindObjectsByType<Transform>().GroupBy(ObjectPath).ToDictionary(g => g.Key, g => g.First());
            foreach (var pose in scenario.poses)
            {
                if (!transforms.TryGetValue(pose.path, out var transform)) throw new InvalidOperationException("Stale scene checkpoint: missing " + pose.path + ". Re-stage and save the composition.");
                transform.localPosition = pose.position; transform.localRotation = pose.rotation; transform.localScale = pose.scale;
                if (!pose.hasSprite) continue;
                var sprite = transform.GetComponent<SpriteRenderer>();
                if (!sprite) throw new InvalidOperationException("Missing checkpoint sprite: " + pose.path);
                sprite.flipX = pose.flipX; sprite.flipY = pose.flipY; sprite.color = pose.color;
                if (GlobalObjectId.TryParse(pose.spriteId, out var id) && GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is Sprite image) sprite.sprite = image;
            }
            foreach (var document in Object.FindObjectsByType<UIDocument>())
            {
                foreach (var selected in scenario.selections.Where(s => s.document == ObjectPath(document.transform)))
                {
                    var button = !string.IsNullOrEmpty(selected.name) ? document.rootVisualElement.Q<Button>(selected.name) : null;
                    button ??= document.rootVisualElement.Query<Button>().ToList().FirstOrDefault(b => ElementPath(b) == selected.path);
                    if (button != null && IsVisible(button) && !button.ClassListContains("selected") && !button.ClassListContains("active"))
                    {
                        Click(button);
                    }
                }
                foreach (var saved in scenario.scrolls.Where(s => s.document == ObjectPath(document.transform)))
                    document.rootVisualElement.Query<ScrollView>().ForEach(view => { if (ElementPath(view) == saved.path) view.scrollOffset = saved.offset; });
            }
        }
        private static bool IsVisible(VisualElement element)
        {
            for (var e = element; e != null; e = e.parent) if (e.resolvedStyle.display == DisplayStyle.None) return false;
            return true;
        }
        private static void Click(Button button)
        {
            // Use the same pointer path as the real UI and its input regression tests.
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = button.worldBound.center })) button.SendEvent(move);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = button.worldBound.center })) button.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = button.worldBound.center })) button.SendEvent(up);
        }
        private static string ObjectPath(Transform t)
        {
            var scene = t.gameObject.scene.name;
            var parts = new List<string>();
            for (; t; t = t.parent)
            {
                var occurrence = 0;
                if (t.parent)
                {
                    for (var i = 0; i < t.GetSiblingIndex(); i++) if (t.parent.GetChild(i).name == t.name) occurrence++;
                }
                else foreach (var root in t.gameObject.scene.GetRootGameObjects())
                { if (root == t.gameObject) break; if (root.name == t.name) occurrence++; }
                parts.Add(Uri.EscapeDataString(t.name) + "[" + occurrence + "]");
            }
            parts.Reverse(); return scene + ":" + string.Join("/", parts);
        }
        private static string ElementPath(VisualElement e)
        {
            var parts = new List<string>();
            for (; e?.parent != null; e = e.parent) parts.Add(e.name + "[" + e.parent.IndexOf(e) + "]");
            parts.Reverse(); return string.Join("/", parts);
        }
        private static void RestoreSeed()
        {
            var json = SessionState.GetString(SeedKey, ""); if (string.IsNullOrEmpty(json)) return;
            var saved = JsonUtility.FromJson<SeedBackup>(json);
            var map = AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(AssetDatabase.GUIDToAssetPath(saved.guid));
            if (map) { map.tilemapChunkSettings.seed = saved.seed; map.tilemapChunkSettings.randomizeSeed = saved.random; }
            SessionState.EraseString(SeedKey);
        }
        public static void End() { if (Active) EditorApplication.isPlaying = false; }
        private static void Cleanup()
        {
            // Exiting Play mode reloads statics before this callback. Restore
            // captured editor values from SessionState, not zeroed static fields.
            RestoreSeed();
            Time.timeScale = SessionState.GetFloat("EOV.ScreenshotStudio.TimeScale", 1);
            Time.captureFramerate = SessionState.GetInt("EOV.ScreenshotStudio.CaptureRate", 0);
            oldWidth = (uint)SessionState.GetInt("EOV.ScreenshotStudio.Width", 0);
            oldHeight = (uint)SessionState.GetInt("EOV.ScreenshotStudio.Height", 0);
            oldLight = SessionState.GetBool("EOV.ScreenshotStudio.OriginalLight", false);
            if (oldWidth > 0 && oldHeight > 0) PlayModeWindow.SetCustomRenderingResolution(oldWidth, oldHeight, "Restored viewport");
            // Only the private capture session may have changed the appearance preference.
            if (ToolkitColorTheme.IsLight != oldLight) ToolkitColorTheme.SetLight(oldLight);
            SaveRootField.SetValue(null, null);
            SessionState.SetBool(ActiveKey, false); Ready = false; phase = 0; disabled.Clear(); animators.Clear(); actors.Clear(); queue.Clear();
            var json = SessionState.GetString(SceneKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var records = JsonUtility.FromJson<Scenes>(json).entries ?? Array.Empty<SceneRecord>();
                var scenes = records.Where(r => !string.IsNullOrEmpty(r.path)).Select(r => new SceneSetup { path = r.path, isLoaded = r.loaded, isActive = r.active }).ToArray();
                if (scenes.Length > 0 && scenes.Any(s => s.isLoaded) && scenes.Any(s => s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(scenes);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Status = "Capture session ended. Original scenes restored; private saves retained under Library/ScreenshotStudio.";
            var next = SessionState.GetString("EOV.ScreenshotStudio.Next", "");
            SessionState.EraseString("EOV.ScreenshotStudio.Next");
            if (!string.IsNullOrEmpty(next)) EditorApplication.delayCall += () => Begin(next);
        }
        internal static string Hash(byte[] data)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant(); }
        internal static void CopyDirectory(string source, string target)
        {
            if (Path.GetFullPath(target).StartsWith(Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("The private copy cannot be inside its source folder.");
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), false);
            foreach (var dir in Directory.GetDirectories(source))
            {
                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked directories are not valid screenshot save inputs.");
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
            }
        }
    }
}
#endif
