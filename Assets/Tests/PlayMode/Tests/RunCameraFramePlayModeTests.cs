#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TimelessEchoes;
using TimelessEchoes.Platform;
using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    [PrebuildSetup(typeof(RunCameraFrameOfflineSession))]
    [PostBuildCleanup(typeof(RunCameraFrameOfflineSession))]
    public sealed class RunCameraFramePlayModeTests
    {
        [UnityTest]
        public IEnumerator DialogueOverridesPreserveUserViewAndCancelWhenMapChanges()
        {
            Assert.IsTrue(StartupDiagnostics.SkipOnlineServices);
            var services = System.Type.GetType("Unity.Services.Core.UnityServices, Unity.Services.Core", true);
            Assert.AreEqual("Uninitialized", services.GetProperty("State").GetValue(null).ToString());
            Assert.IsNull(GameManager.Instance);
            Assert.IsNull(Object.FindAnyObjectByType<WorldPresentation>(), "This fixture requires an empty presentation scene.");
            UnityEditor.PlayModeWindow.GetRenderingResolution(out var previousWidth, out var previousHeight);
            var owner = new GameObject("Isolated presentation lifecycle");
            WorldPresentation.DialogueSession session = null;
            var generationConfig = typeof(GameManager).GetProperty(nameof(GameManager.CurrentGenerationConfig), BindingFlags.Public | BindingFlags.Static);
            var previousConfig = GameManager.CurrentGenerationConfig;
            try
            {
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1080, 1920, "Portrait presentation lifecycle");
                for (var i = 0; i < 60 && (UnityEngine.Device.Screen.width != 1080 || UnityEngine.Device.Screen.height != 1920); i++)
                    yield return null;
                Assert.AreEqual(1080, UnityEngine.Device.Screen.width);
                Assert.AreEqual(1920, UnityEngine.Device.Screen.height);

                var game = owner.AddComponent<GameManager>(); game.enabled = false;
                var map = new GameObject("Original run"); map.transform.SetParent(owner.transform);
                typeof(GameManager).GetProperty("CurrentMap").SetValue(game, map);
                typeof(GameManager).GetProperty("IsInTown").SetValue(game, false);
                var outputObject = new GameObject("Output", typeof(Camera)); outputObject.transform.SetParent(owner.transform);
                outputObject.tag = "MainCamera";
                var output = outputObject.GetComponent<Camera>();
                output.orthographic = true; output.orthographicSize = 9; output.cullingMask = 0;
                var brain = outputObject.AddComponent<CinemachineBrain>();
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
                var normalObject = new GameObject("Ordinary run camera", typeof(CinemachineCamera)); normalObject.transform.SetParent(map.transform);
                normalObject.transform.position = new Vector3(30, 9, -10);
                var normal = normalObject.GetComponent<CinemachineCamera>(); normal.Lens.OrthographicSize = 9; normal.Priority = 10;
                var boundary = normalObject.AddComponent<CameraClampExtension>();
                typeof(CameraClampExtension).GetField("yLevel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boundary, 9f);
                RunCameraFrame.Attach(normal);
                ToolkitResponsiveLayout.SetFocusView(false);
                brain.ManualUpdate();
                yield return null;

                var position = output.transform.position;
                session = WorldPresentation.BeginDialogue(game, "Orin");
                Assert.IsNotNull(session);
                yield return null;
                brain.ManualUpdate();
                Assert.IsFalse(ToolkitResponsiveLayout.UserFocusView);
                Assert.IsTrue(ToolkitResponsiveLayout.FocusView, "Orin frames the player in tall portrait, just like a physical speaker.");
                Assert.AreSame(normal, brain.ActiveVirtualCamera);
                Assert.That(Vector3.Distance(output.transform.position, position), Is.LessThan(.001f));
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Orin landscape");
                for (var i = 0; i < 60 && UnityEngine.Device.Screen.width != 1920; i++) yield return null;
                yield return null;
                brain.ManualUpdate();
                Assert.IsFalse(ToolkitResponsiveLayout.FocusView);
                Assert.AreSame(normal, brain.ActiveVirtualCamera, "A missing world subject must leave the normal camera usable.");
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1080, 1920, "Portrait presentation lifecycle");
                for (var i = 0; i < 60 && UnityEngine.Device.Screen.width != 1080; i++) yield return null;
                session.Dispose(); session = null;
                Assert.IsFalse(WorldPresentation.IsBusy);
                yield return null;

                var speaker = new GameObject("Physical NPC"); speaker.transform.SetParent(map.transform);
                speaker.transform.position = new Vector3(3, 5.125f, 0);
                speaker.AddComponent<WorldSpeakerAnchor>().speakerId = "Ivan1";
                var farmlands = AssetDatabase.LoadAssetAtPath<TimelessEchoes.MapGeneration.MapGenerationConfig>("Assets/Scriptables/MapSettings/Farmlands.asset");
                Assert.IsNotNull(farmlands);
                Assert.That(farmlands.taskGeneratorSettings.minX, Is.GreaterThan(speaker.transform.position.x),
                    "Exercise an NPC before task spawning starts: that threshold is not the camera boundary.");
                generationConfig.SetValue(null, farmlands);
                var promptObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/CharacterMeeting.prefab"), owner.transform);
                var prompt = promptObject.GetComponent<ToolkitMeetingScreen>();
                prompt.Init(null, new[] { "Hello.", "A longer conversation line needs more space for its words, but the camera should stay in exactly the same place while the player reads it." }, () => { }, npcId: "Gill1", worldSubject: speaker.transform);
                yield return null;
                Assert.IsFalse(ToolkitMeetingScreen.HasOpenDialogue, "This is the waiting Meet prompt, not the conversation.");
                Assert.IsTrue(WorldPresentation.HasDialogue, "The waiting prompt must already own speaker framing.");
                yield return new WaitForSecondsRealtime(.8f);
                brain.ManualUpdate();
                var promptRoot = promptObject.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement.Q(className: "eov-meeting");
                Assert.AreEqual(UnityEngine.UIElements.Visibility.Visible, promptRoot.resolvedStyle.visibility);
                var waitingHeight = promptRoot.worldBound.height;
                Assert.AreNotSame(normal, brain.ActiveVirtualCamera, "Meet must not cover an unframed actor while it waits for a click.");
                prompt.BeginDialogue();
                yield return null;
                Assert.IsTrue(ToolkitMeetingScreen.HasOpenDialogue, "Meet must reuse its own camera session rather than deny the click as busy.");
                yield return null;
                Assert.That(promptRoot.worldBound.height, Is.EqualTo(waitingHeight).Within(.1f), "Reserve the whole conversation before Meet so starting it does not move the camera.");
                var promptView = (ToolkitMeetingView)typeof(ToolkitMeetingScreen).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(prompt);
                promptView.Advance();
                yield return null; yield return null;
                Assert.That(promptRoot.worldBound.height, Is.EqualTo(waitingHeight).Within(.1f), "Short and long messages must share the same framing space.");
                Object.DestroyImmediate(promptObject);
                yield return new WaitForSecondsRealtime(.55f);
                Assert.IsFalse(WorldPresentation.IsBusy);
                session = WorldPresentation.BeginDialogue(game, "Ivan1", speaker.transform);
                Assert.IsNotNull(session);
                // The real dialogue refreshes its bounds while the portrait dock slides.
                // Measure this camera-only fixture after that viewport transition.
                yield return new WaitForSecondsRealtime(.25f);
                var dialogueRect = WorldPresentation.DialogueRect(120);
                session.SetDialogueRect(dialogueRect);
                Assert.IsFalse(ToolkitResponsiveLayout.UserFocusView);
                Assert.IsTrue(ToolkitResponsiveLayout.FocusView);
                yield return null;
                brain.ManualUpdate();
                Assert.AreNotSame(normal, brain.ActiveVirtualCamera);
                var approachStarted = Time.unscaledTime;
                while (Time.unscaledTime - approachStarted < .75f) yield return null;
                brain.ManualUpdate();
                var viewport = output.WorldToViewportPoint(speaker.transform.position);
                Assert.That(viewport.x, Is.InRange(0f, 1f), "The left-edge NPC must remain visible despite the later task-spawn threshold.");
                Assert.That(viewport.y, Is.InRange(0f, 1f));
                var screen = output.WorldToScreenPoint(speaker.transform.position);
                var ui = new Vector2(screen.x, UnityEngine.Device.Screen.height - screen.y) *
                    ToolkitResponsiveLayout.Scale(new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height));
                Assert.That(ui.y, Is.LessThan(dialogueRect.yMin), "The low shoreline speaker must be above the dialogue card.");
                var beforeReflow = output.transform.position;
                var beforeReflowSize = output.orthographicSize;
                session.SetDialogueRect(WorldPresentation.DialogueRect(220));
                yield return null;
                brain.ManualUpdate();
                Assert.That(Vector3.Distance(output.transform.position, beforeReflow) + Mathf.Abs(output.orthographicSize - beforeReflowSize),
                    Is.LessThan(.2f), "Changing the dialogue height must ease the established camera, not snap to a new pose.");
                yield return new WaitForSecondsRealtime(.8f);
                session.SetDialogueRect(dialogueRect);
                yield return new WaitForSecondsRealtime(.8f);
                generationConfig.SetValue(null, previousConfig);

                session.SetSuspended(true);
                Assert.IsFalse(ToolkitResponsiveLayout.FocusView, "Settings temporarily releases the tall dialogue layout.");
                Assert.IsFalse(ToolkitResponsiveLayout.UserFocusView);
                yield return null;
                brain.ManualUpdate();
                Assert.AreSame(normal, brain.ActiveVirtualCamera);
                session.SetSuspended(false);
                Assert.IsTrue(ToolkitResponsiveLayout.FocusView);
                yield return null;
                brain.ManualUpdate();
                Assert.AreNotSame(normal, brain.ActiveVirtualCamera);

                session.SetGuidance(true);
                Assert.IsFalse(ToolkitResponsiveLayout.FocusView, "A tutorial handoff must allow the ordinary panel layout.");
                yield return null;
                brain.ManualUpdate();
                Assert.AreSame(normal, brain.ActiveVirtualCamera);
                session.Dispose(); session = null;
                Assert.IsFalse(WorldPresentation.IsBusy);
                Assert.IsFalse(ToolkitResponsiveLayout.FocusView);
                Assert.IsFalse(ToolkitResponsiveLayout.UserFocusView);
                yield return null;

                ToolkitResponsiveLayout.SetFocusView(true);
                session = WorldPresentation.BeginDialogue(game, "Ivan1", speaker.transform);
                Assert.IsNotNull(session);
                session.SetSuspended(true);
                Assert.IsTrue(ToolkitResponsiveLayout.UserFocusView);
                Assert.IsFalse(ToolkitResponsiveLayout.FocusView);
                session.SetSuspended(false);
                Assert.IsTrue(ToolkitResponsiveLayout.FocusView);
                session.SetGuidance(true);
                session.Dispose(); session = null;
                Assert.IsTrue(ToolkitResponsiveLayout.UserFocusView);
                Assert.IsTrue(ToolkitResponsiveLayout.FocusView, "Disposing a temporary override must retain the user's expanded choice.");
                yield return null;

                ToolkitResponsiveLayout.SetFocusView(false);
                session = WorldPresentation.BeginDialogue(game, "Ivan1", speaker.transform);
                Assert.IsNotNull(session);
                yield return null;
                brain.ManualUpdate();
                Assert.IsTrue(ToolkitResponsiveLayout.FocusView);
                var replacement = new GameObject("Replacement run"); replacement.transform.SetParent(owner.transform);
                var replacementCameraObject = new GameObject("Replacement normal camera", typeof(CinemachineCamera));
                replacementCameraObject.transform.SetParent(replacement.transform);
                replacementCameraObject.transform.position = new Vector3(80, 9, -10);
                var replacementCamera = replacementCameraObject.GetComponent<CinemachineCamera>();
                replacementCamera.Lens.OrthographicSize = 9; replacementCamera.Priority = 20;
                RunCameraFrame.Attach(replacementCamera);
                typeof(GameManager).GetProperty("CurrentMap").SetValue(game, replacement);
                map.SetActive(false);
                Assert.IsTrue(session.Interrupted);
                yield return null;
                brain.ManualUpdate();
                Assert.IsTrue(session.IsDisposed);
                Assert.IsFalse(WorldPresentation.IsBusy);
                Assert.IsFalse(ToolkitResponsiveLayout.FocusView);
                Assert.IsFalse(ToolkitResponsiveLayout.UserFocusView);
                Assert.AreSame(replacementCamera, brain.ActiveVirtualCamera);
                Assert.That(output.transform.position.x, Is.EqualTo(80).Within(.001f), "An old conversation must not restore the previous map's view.");
                Assert.AreEqual("Uninitialized", services.GetProperty("State").GetValue(null).ToString());
            }
            finally
            {
                session?.Dispose();
                var director = Object.FindAnyObjectByType<WorldPresentation>();
                if (director) Object.DestroyImmediate(director.gameObject);
                generationConfig.SetValue(null, previousConfig);
                ToolkitResponsiveLayout.SetFocusView(false);
                Object.DestroyImmediate(owner);
                if (previousWidth > 0 && previousHeight > 0)
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution(previousWidth, previousHeight, "Restored presentation test viewport");
            }
        }

        [UnityTest]
        public IEnumerator GillFacesTheListenerOnBothSidesAndResumesFishing()
        {
            Assert.IsTrue(StartupDiagnostics.SkipOnlineServices);
            var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/NPC/GillMeetingTask.prefab"));
            var listener = new GameObject("Conversation listener");
            try
            {
                actor.transform.position = Vector3.zero;
                listener.transform.position = Vector3.left * 2;
                var performance = actor.GetComponent<WorldSpeakerPerformance>();
                var artwork = actor.GetComponent<SpriteRenderer>();
                performance.Begin(listener.transform);
                yield return new WaitForSecondsRealtime(.8f);
                Assert.IsTrue(performance.Ready);
                // Gill's actual side sprite is authored looking right.
                Assert.IsTrue(artwork.flipX, "Gill must mirror his right-facing sheet when the listener is to his left.");
                listener.transform.position = Vector3.right * 2;
                yield return null;
                Assert.IsFalse(artwork.flipX, "Gill must use his unmirrored side when the listener is to his right.");
                yield return performance.Finish();
                Assert.IsTrue(actor.GetComponent<Animator>().enabled);
                Assert.IsFalse(performance.Playing);
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(listener); }
        }

        [UnityTest]
        public IEnumerator FirstReapPromptWaitsForOrinsExplanation()
        {
            Assert.IsTrue(StartupDiagnostics.SkipOnlineServices);
            var previousOracle = Blindsided.Oracle.oracle;
            var oracleObject = new GameObject("Private first-reap progress"); oracleObject.SetActive(false);
            var oracle = oracleObject.AddComponent<Blindsided.Oracle>(); oracle.enabled = false;
            oracle.saveData = new Blindsided.SaveData.GameData();
            Blindsided.Oracle.oracle = oracle;
            var owner = new GameObject("First-reap sequencing");
            try
            {
                var game = owner.AddComponent<GameManager>(); game.enabled = false;
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GameManager).GetField("heroDead", flags).SetValue(game, true);
                typeof(GameManager).GetField("runEndedByReaper", flags).SetValue(game, true);
                typeof(GameManager).GetProperty("IsInTown").SetValue(game, false);
                Assert.IsTrue(game.FirstReapDialoguePending);
                game.StartCoroutine((IEnumerator)typeof(GameManager).GetMethod("DeathWindowRoutine", flags).Invoke(game, null));
                yield return null;
                Assert.IsFalse(game.DeathPromptVisible, "The reaped prompt must remain hidden while Orin's first-reap explanation is pending.");
                oracle.saveData.CompletedNpcTasks.Add("FirstReapIntroduction");
                yield return null; yield return null;
                Assert.IsTrue(game.DeathPromptVisible, "Dismissal of Orin's explanation reveals the reaped prompt.");
                Assert.That(game.DeathPromptProgress, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(owner); Object.DestroyImmediate(oracleObject);
                Blindsided.Oracle.oracle = previousOracle;
            }
        }

        [UnityTest]
        public IEnumerator CompletedRegisteredNpcKeepsFishingInTheWorld()
        {
            Assert.IsTrue(StartupDiagnostics.SkipOnlineServices);
            var previousOracle = Blindsided.Oracle.oracle;
            var oracleObject = new GameObject("Private NPC progress"); oracleObject.SetActive(false);
            var oracle = oracleObject.AddComponent<Blindsided.Oracle>(); oracle.enabled = false;
            oracle.saveData = new Blindsided.SaveData.GameData();
            Blindsided.Oracle.oracle = oracle;
            var owner = new GameObject("Registered NPC completion");
            GameObject actor = null;
            try
            {
                var controller = owner.AddComponent<TimelessEchoes.Tasks.TaskController>();
                actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tasks/NPC/GillMeetingTask.prefab"), owner.transform);
                var task = actor.GetComponent<TimelessEchoes.Tasks.TalkToNpcTask>();
                task.StartTask();
                controller.AddRuntimeTaskObject(task);
                Assert.Contains(task, controller.tasks);
                task.OnArrival(null);
                yield return null;
                var meeting = Object.FindAnyObjectByType<ToolkitMeetingScreen>();
                Assert.IsNotNull(meeting);
                var view = (ToolkitMeetingView)typeof(ToolkitMeetingScreen).GetField("view", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(meeting);
                for (var i = 0; i < 20; i++) view.Advance();
                yield return null;
                Assert.IsTrue(actor && actor.activeInHierarchy, "Completing a registered NPC task must keep its world actor rather than pool it.");
                Assert.IsFalse(task.enabled, "The completed encounter must leave the selectable task list.");
                Assert.IsFalse(controller.tasks.Contains(task));
                Assert.IsTrue(actor.GetComponent<Animator>().enabled, "Gill must resume his normal fishing animation.");
                Assert.IsTrue(oracle.saveData.CompletedNpcTasks.Contains("Gill1"));
            }
            finally
            {
                foreach (var meeting in Object.FindObjectsByType<ToolkitMeetingScreen>()) Object.DestroyImmediate(meeting.gameObject);
                var director = Object.FindAnyObjectByType<WorldPresentation>();
                if (director) Object.DestroyImmediate(director.gameObject);
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(oracleObject);
                Blindsided.Oracle.oracle = previousOracle;
                Blindsided.SaveData.StaticReferences.ActiveNpcMeetings.Clear();
            }
        }

        [UnityTest]
        public IEnumerator PortraitViewSlidesBothWaysWithoutChangingLogicalViewport()
        {
            Assert.IsNull(GameManager.Instance);
            UnityEditor.PlayModeWindow.GetRenderingResolution(out var oldWidth, out var oldHeight);
            try
            {
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1080, 1920, "Portrait surface transition");
                for (var i = 0; i < 60 && UnityEngine.Device.Screen.width != 1080; i++) yield return null;
                ToolkitResponsiveLayout.SetFocusView(false);
                var size = new Vector2Int(1080, 1920);
                var safe = new Rect(0, 0, 1080, 1920);
                ToolkitResponsiveLayout.WorldPixels(size, safe);
                yield return new WaitForSecondsRealtime(.25f);
                var normal = ToolkitResponsiveLayout.WorldPixels(size, safe);
                ToolkitResponsiveLayout.SetFocusView(true);
                Assert.AreEqual(normal, ToolkitResponsiveLayout.WorldPixels(size, safe),
                    "Expansion should start at the existing viewport instead of jumping to the target.");
                Assert.That(ToolkitResponsiveLayout.WorldPixels(size, safe, true).height, Is.GreaterThan(normal.height),
                    "Gameplay's immediate user viewport must not inherit the visual tween.");
                yield return new WaitForSecondsRealtime(.25f);
                var expanded = ToolkitResponsiveLayout.WorldPixels(size, safe);
                Assert.AreEqual(ToolkitResponsiveLayout.WorldPixels(size, safe, true), expanded);
                ToolkitResponsiveLayout.SetFocusView(false);
                Assert.AreEqual(expanded, ToolkitResponsiveLayout.WorldPixels(size, safe),
                    "Restoring the dock should also animate from the existing viewport.");
                yield return new WaitForSecondsRealtime(.25f);
                Assert.AreEqual(normal, ToolkitResponsiveLayout.WorldPixels(size, safe));
            }
            finally
            {
                ToolkitResponsiveLayout.SetFocusView(false);
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(oldWidth, oldHeight, "Restored viewport");
            }
        }

        [UnityTest]
        public IEnumerator PresentationDoesNotMoveGameplayAreaAndNormalTrackingStaysLive()
        {
            Assert.IsTrue(StartupDiagnostics.SkipOnlineServices, "The offline guard must precede runtime initialization, not be installed by the test body.");
            var services = System.Type.GetType("Unity.Services.Core.UnityServices, Unity.Services.Core", true);
            Assert.AreEqual("Uninitialized", services.GetProperty("State").GetValue(null).ToString(), "The isolated fixture must not start initializing online services.");
            Assert.IsNull(GameManager.Instance, "Run this camera-only regression in the runner's empty scene.");
            UnityEditor.PlayModeWindow.GetRenderingResolution(out var previousWidth, out var previousHeight);
            var owner = new GameObject("Isolated camera separation");
            try
            {
                UnityEditor.PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "Run camera separation");
                for (var i = 0; i < 60 && (UnityEngine.Device.Screen.width != 1920 || UnityEngine.Device.Screen.height != 1080); i++)
                    yield return null;
                Assert.AreEqual(1920, UnityEngine.Device.Screen.width);
                Assert.AreEqual(1080, UnityEngine.Device.Screen.height);

                var outputObject = new GameObject("Output", typeof(Camera));
                outputObject.transform.SetParent(owner.transform);
                var output = outputObject.GetComponent<Camera>();
                output.orthographic = true; output.orthographicSize = 9; output.aspect = 16f / 9f;
                output.cullingMask = 0;
                var brain = outputObject.AddComponent<CinemachineBrain>();
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);

                var hero = new GameObject("Tracking target"); hero.transform.SetParent(owner.transform);
                hero.transform.position = new Vector3(30, 0, 0);
                var runObject = new GameObject("Normal run camera", typeof(CinemachineCamera));
                runObject.transform.SetParent(owner.transform);
                var normal = runObject.GetComponent<CinemachineCamera>();
                normal.Lens.OrthographicSize = 9; normal.Follow = hero.transform; normal.Priority = 10;
                var composer = runObject.AddComponent<CinemachinePositionComposer>();
                composer.Damping = Vector3.zero;
                var composition = composer.Composition; composition.DeadZone.Enabled = false;
                composer.Composition = composition;
                composer.Lookahead.Enabled = false;
                runObject.AddComponent<CameraClampExtension>();
                var frame = RunCameraFrame.Attach(normal);
                brain.ManualUpdate();
                yield return null;
                Assert.IsTrue(frame.HasState);
                Assert.IsTrue(RunActivityArea.TryGetBounds(output, out var beforeMin, out var beforeMax));
                Assert.That(beforeMax.x - beforeMin.x, Is.EqualTo(32).Within(.01f));

                var cinematicObject = new GameObject("Presentation camera", typeof(CinemachineCamera));
                cinematicObject.transform.SetParent(owner.transform);
                cinematicObject.transform.position = new Vector3(300, 100, -10);
                var cinematic = cinematicObject.GetComponent<CinemachineCamera>();
                cinematic.Lens.OrthographicSize = 3; cinematic.Priority = 100;
                output.aspect = .6f; // The rendered view becomes tall while the user's ordinary view stays wide.
                brain.ManualUpdate();
                yield return null;

                Assert.That(output.transform.position.x, Is.EqualTo(300).Within(.01f));
                Assert.That(output.orthographicSize, Is.EqualTo(3).Within(.01f));
                Assert.That(frame.State.Lens.Aspect, Is.EqualTo(16f / 9f).Within(.001f));
                Assert.IsTrue(RunActivityArea.TryGetBounds(output, out var duringMin, out var duringMax));
                Assert.That(duringMin.x, Is.EqualTo(beforeMin.x).Within(.01f));
                Assert.That(duringMax.x, Is.EqualTo(beforeMax.x).Within(.01f));
                Assert.IsTrue(RunActivityArea.Contains(output, hero.transform.position));
                Assert.IsFalse(RunActivityArea.Contains(output, new Vector3(300, 100, 0)));

                hero.transform.position += Vector3.right * 20;
                brain.ManualUpdate();
                yield return null;
                Assert.That(frame.VisibleBounds.center.x, Is.GreaterThan((beforeMin.x + beforeMax.x) * .5f + 10),
                    "The logical view must keep tracking combat movement rather than freeze its entry snapshot.");
                Assert.That(output.transform.position.x, Is.EqualTo(300).Within(.01f));
                Assert.IsTrue(RunCameraFrame.TryGetVisibleBounds(output, out var guardMin, out var guardMax));
                Assert.That(guardMax.x - guardMin.x, Is.EqualTo(32).Within(.01f));
                Assert.IsTrue(RunActivityArea.Contains(output, hero.transform.position));

                cinematicObject.SetActive(false);
                brain.ManualUpdate();
                yield return null;
                Assert.That(output.transform.position.x, Is.EqualTo(frame.VisibleBounds.center.x).Within(.01f));
                runObject.SetActive(false);
                Assert.IsFalse(RunCameraFrame.TryGetCurrent(out _), "An unloaded map must not leave its logical frame registered.");
                Assert.AreEqual("Uninitialized", services.GetProperty("State").GetValue(null).ToString());
            }
            finally
            {
                Object.DestroyImmediate(owner);
                if (previousWidth > 0 && previousHeight > 0)
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution(previousWidth, previousHeight, "Restored camera test viewport");
            }
        }
    }

    // Reuse the production editor-capture guard before runtime initializers run.
    // No Oracle is created and the capture worker waits harmlessly for one.
    public sealed class RunCameraFrameOfflineSession : IPrebuildSetup, IPostBuildCleanup
    {
        private const string Prefix = "EOV.ScreenshotStudio.";
        private const string SavedKey = "EOV.Tests.RunCameraFrame.Session";
        private const string DirectoryPrefix = "RunCameraFrame-";

        [System.Serializable]
        private sealed class SavedSession
        {
            public string directory, root, folder, scenes, seed, next, previousSaveRoot;
            public bool active, originalLight;
            public int width, height, captureRate, previousCaptureRate;
            public float timeScale, previousTimeScale;
        }

        private static FieldInfo SaveRootField => typeof(Blindsided.SaveData.SaveManager)
            .GetField("rootPathOverride", BindingFlags.NonPublic | BindingFlags.Static);

        public void Setup()
        {
            if (SessionState.GetBool(Prefix + "Active", false))
                throw new System.InvalidOperationException("End the existing screenshot session before running camera-only tests.");
            var saved = new SavedSession
            {
                directory = Path.Combine(Application.temporaryCachePath, DirectoryPrefix + System.Guid.NewGuid().ToString("N")),
                active = SessionState.GetBool(Prefix + "Active", false),
                root = SessionState.GetString(Prefix + "Root", ""),
                folder = SessionState.GetString(Prefix + "Folder", ""),
                scenes = SessionState.GetString(Prefix + "Scenes", ""),
                seed = SessionState.GetString(Prefix + "SeedBackup", ""),
                next = SessionState.GetString(Prefix + "Next", ""),
                originalLight = SessionState.GetBool(Prefix + "OriginalLight", ToolkitColorTheme.IsLight),
                timeScale = SessionState.GetFloat(Prefix + "TimeScale", Time.timeScale),
                captureRate = SessionState.GetInt(Prefix + "CaptureRate", Time.captureFramerate),
                width = SessionState.GetInt(Prefix + "Width", 0),
                height = SessionState.GetInt(Prefix + "Height", 0),
                previousSaveRoot = SaveRootField.GetValue(null) as string,
                previousCaptureRate = Time.captureFramerate,
                previousTimeScale = Time.timeScale
            };
            SessionState.SetString(SavedKey, JsonUtility.ToJson(saved));
            new IsolatedPlayModeScene().Setup();
            try
            {
                var root = Path.Combine(saved.directory, "private-saves");
                var scenario = Path.Combine(saved.directory, "scenario");
                Directory.CreateDirectory(root); Directory.CreateDirectory(scenario);
                File.WriteAllText(Path.Combine(scenario, "scenario.json"),
                    "{\"version\":1,\"name\":\"Camera isolation\",\"runFrames\":0,\"lightMode\":" + (ToolkitColorTheme.IsLight ? "true" : "false") + "}");
                SessionState.SetString(Prefix + "Root", root);
                SessionState.SetString(Prefix + "Folder", scenario);
                SessionState.SetString(Prefix + "Scenes", "");
                SessionState.SetString(Prefix + "SeedBackup", "");
                SessionState.SetString(Prefix + "Next", "");
                SessionState.SetBool(Prefix + "OriginalLight", ToolkitColorTheme.IsLight);
                SessionState.SetBool(Prefix + "Active", true);
            }
            catch { Cleanup(); throw; }
        }

        public void Cleanup()
        {
            var json = SessionState.GetString(SavedKey, "");
            if (string.IsNullOrEmpty(json)) return;
            var saved = JsonUtility.FromJson<SavedSession>(json);
            SessionState.SetBool(Prefix + "Active", false);
            Time.timeScale = saved.previousTimeScale;
            Time.captureFramerate = saved.previousCaptureRate;
            SaveRootField.SetValue(null, saved.previousSaveRoot);
            SessionState.SetString(Prefix + "Root", saved.root);
            SessionState.SetString(Prefix + "Folder", saved.folder);
            SessionState.SetString(Prefix + "Scenes", saved.scenes);
            SessionState.SetString(Prefix + "SeedBackup", saved.seed);
            SessionState.SetString(Prefix + "Next", saved.next);
            SessionState.SetBool(Prefix + "OriginalLight", saved.originalLight);
            SessionState.SetFloat(Prefix + "TimeScale", saved.timeScale);
            SessionState.SetInt(Prefix + "CaptureRate", saved.captureRate);
            SessionState.SetInt(Prefix + "Width", saved.width);
            SessionState.SetInt(Prefix + "Height", saved.height);
            SessionState.SetBool(Prefix + "Active", saved.active);
            new IsolatedPlayModeScene().Cleanup();
            var owned = Path.GetFullPath(saved.directory);
            var parent = Path.GetFullPath(Application.temporaryCachePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (owned.StartsWith(parent + DirectoryPrefix, System.StringComparison.OrdinalIgnoreCase) && Directory.Exists(owned))
                Directory.Delete(owned, true);
            SessionState.EraseString(SavedKey);
        }
    }
}
#endif
