using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitMeetingScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private VisualTreeAsset template;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private ToolkitMeetingDefinition definition;
        private static readonly List<ToolkitMeetingScreen> Visible = new();
        private PanelSettings panel;
        private ToolkitMeetingView view;
        private Sprite portrait;
        private Quests.QuestSpeaker speakerProfile;
        private string npcId;
        private IReadOnlyList<Sprite> portraitFrames;
        private Image animatedPortrait, meetHand;
        private float portraitFramesPerSecond, portraitAnimationStartedAt;
        private bool townOverview;
        private IReadOnlyList<string> lines, lineKeys;
        private Action finished;
        private bool initialized, completed;
        private bool continueAfterDialogue;
        private ToolkitNavigationScreen lessonNavigation;
        private bool guidanceStarted;
        private string navigationShortcut;
        private IDisposable conversationWait;
        private Transform worldSubject;
        private WorldPresentation.DialogueSession presentation;
        private WorldPresentationWind wind;
        private WorldSpeakerPerformance performance;
        private bool dialogueRequested, entrancePending, closing;
        private GameManager contextGame;
        private GameObject contextMap;
        private bool hadGameContext, hadMapContext, contextStartedDead, contextInvalidated, watchingLoads;
        private bool ContextValid => !contextInvalidated && (!hadGameContext || contextGame &&
            GameManager.Instance == contextGame && (!hadMapContext || contextMap) &&
            contextGame.CurrentMap == contextMap && (contextStartedDead || !contextGame.HeroIsDead));
        public static bool HasVisibleMeeting => Visible.Count > 0;
        private static ToolkitMeetingScreen ActiveMeeting => Visible.Find(meeting => meeting && !meeting.completed && meeting.view != null);
        private static ToolkitMeetingScreen ActiveDialogue => ActiveMeeting is { } meeting && !meeting.view.AwaitingMeet ? meeting : null;
        public static bool HasOpenDialogue => ActiveDialogue != null;

        public static bool AllowsNavigation(Func<string, bool> matches) => ActiveDialogue?.view.FindNavigationRoute(matches) != null;
        public static bool AllowsNavigation(string route) => AllowsNavigation(candidate => candidate == route);
        public static bool TryFollowNavigation(string route) => TryFollowNavigation(candidate => candidate == route);

        public static bool TryFollowNavigation(Func<string, bool> matches)
        {
            var meeting = ActiveDialogue;
            var route = meeting?.view.FindNavigationRoute(matches);
            return route != null && meeting.view.FollowNavigation(route);
        }

        public void SetNavigationShortcut(string route)
        {
            navigationShortcut = route;
            view?.SetNavigationShortcut(route);
        }

        public static bool AdvanceRefresherNavigation(ToolkitNavigationScreen navigation)
        {
            var meeting = ActiveDialogue;
            if (!meeting || meeting.lessonNavigation != navigation) return false;
            if (!meeting.guidanceStarted || navigation.RefresherDestinationOpen) meeting.view.Advance();
            return meeting.completed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Visible.Clear();

        public void Init(Sprite npcPortrait, IReadOnlyList<string> dialogue, Action onFinished, IReadOnlyList<string> localizedLineKeys = null, bool continueAfterDialogue = false, string npcId = null, Transform worldSubject = null, bool townOverview = false)
        {
            this.npcId = npcId;
            this.worldSubject = worldSubject;
            this.townOverview = townOverview;
            performance = worldSubject ? worldSubject.GetComponent<WorldSpeakerPerformance>() : null;
            contextGame = GameManager.Instance;
            hadGameContext = contextGame != null;
            contextMap = contextGame ? contextGame.CurrentMap : null;
            hadMapContext = contextMap != null;
            contextStartedDead = contextGame && contextGame.HeroIsDead;
            contextInvalidated = false;
            if (!watchingLoads)
            {
                Blindsided.EventHandler.OnLoadData += Loaded;
                watchingLoads = true;
            }
            speakerProfile = npcId is "Orin" or "OrinHubIntroduction" ? definition.orinSpeaker : null;
            portrait = speakerProfile ? speakerProfile.portrait : npcPortrait;
            lines = dialogue;
            lineKeys = localizedLineKeys;
            this.continueAfterDialogue = continueAfterDialogue;
            finished = onFinished;
            initialized = true;
            if (isActiveAndEnabled) Show();
        }

        private void OnEnable() { if (initialized && !completed) Show(); }
        public void SetEntrancePending(bool pending)
        {
            entrancePending = pending;
            ApplyLayout();
        }

        public void BeginDialogue()
        {
            dialogueRequested = true;
            if (ActiveMeeting == this && view?.AwaitingMeet == true) view.Advance();
        }

        public void GuideNavigation(ToolkitNavigationScreen navigation)
        {
            lessonNavigation = navigation;
            view.AddSkipAction(Finish);
            if (!navigation) return;
            view.BeforeAdvance = () =>
            {
                if (guidanceStarted) return !navigation.RefresherDestinationOpen;
                guidanceStarted = true;
                navigation.SetRefresherGuidanceActive(true);
                return true;
            };
        }

        public void SetPortraitAnimation(IReadOnlyList<Sprite> frames, float framesPerSecond, float? startedAt = null)
        {
            if (frames == null || frames.Count == 0 || view == null) return;
            portraitFrames = frames;
            portraitFramesPerSecond = Mathf.Max(1, framesPerSecond);
            portraitAnimationStartedAt = startedAt ?? Time.unscaledTime;
            animatedPortrait = view.Root.Q<Image>("portrait");
            animatedPortrait.sprite = portraitFrames[0];
        }

        private void Show()
        {
            if (!ContextValid) { CancelStaleMeeting(); return; }
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;
            if (!theme || !template || !runtimeTheme || !textSettings || !definition || !definition.portraitFrame || !definition.shadow)
                throw new InvalidOperationException("Native meeting assets are not configured.");
            if (!panel)
            {
                panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                panel.sortingOrder = 180;
            }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            view ??= new ToolkitMeetingView(theme, template, portrait, definition, lines, Finish, lineKeys, continueAfterDialogue,
                speakerProfile ? speakerProfile.dialoguePixelScale : 1);
            view.BeforeStart = TryBeginPresentation;
            if (speakerProfile)
            {
                view.Root.Q<Image>("portrait").AddToClassList("quest-speaker-orin");
                SetPortraitAnimation(speakerProfile.frames, speakerProfile.framesPerSecond);
            }
            view.SetNavigationShortcut(navigationShortcut);
            view.SetSpeaker(npcId);
            document.rootVisualElement.Add(view.Root);
            if (meetHand == null)
            {
                meetHand = new Image { name = "meeting-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
                meetHand.style.position = Position.Absolute;
                meetHand.style.width = meetHand.style.height = 24;
                view.Root.Add(meetHand);
            }
            if (!Visible.Contains(this)) Visible.Add(this);
            if (ActiveMeeting == this)
            {
                conversationWait ??= TimelessEchoes.Hero.HeroController.Instance?.BeginNpcConversation(worldSubject);
                // Frame a physical encounter before Meet, not only after it.
                if (worldSubject) TryBeginPresentation();
            }
            SetEntrancePending(entrancePending);
            ApplyLayout();
        }

        private bool TryBeginPresentation()
        {
            if (!ContextValid || ActiveMeeting != this || completed || (presentation == null && WorldPresentation.IsBusy) ||
                TownWindowManager.Instance && TownWindowManager.Instance.IsWindowOpen(TownWindowManager.Window.Options)) return false;
            presentation ??= WorldPresentation.BeginDialogue(this, npcId, worldSubject, townOverview);
            if (presentation == null) return false;
            conversationWait ??= TimelessEchoes.Hero.HeroController.Instance?.BeginNpcConversation(worldSubject);
            if (speakerProfile && GameManager.Instance && GameManager.Instance.CurrentMap && !wind)
            {
                wind = gameObject.AddComponent<WorldPresentationWind>();
                wind.Setup(speakerProfile);
            }
            return true;
        }

        private void ReleasePresentation()
        {
            performance?.Cancel();
            wind?.SetVisible(false);
            if (wind) { Destroy(wind); wind = null; }
            presentation?.Dispose(); presentation = null;
            conversationWait?.Dispose(); conversationWait = null;
        }

        private void Loaded()
        {
            // Remain subscribed while disabled: a hidden scene panel must not
            // resume an old encounter after another slot has been loaded.
            contextInvalidated = true;
            CancelStaleMeeting();
        }

        private void CancelStaleMeeting()
        {
            completed = true;
            Visible.Remove(this);
            if (view != null) view.Root.style.display = DisplayStyle.None;
            ReleasePresentation();
            Destroy(gameObject);
        }

        private void Finish()
        {
            if (completed || closing) return;
            if (performance && performance.Playing)
            {
                closing = true;
                view.Root.style.display = DisplayStyle.None;
                StartCoroutine(FinishPerformance());
                return;
            }
            CompleteMeeting();
        }

        private System.Collections.IEnumerator FinishPerformance()
        {
            yield return performance.Finish();
            if (ContextValid) CompleteMeeting();
            else CancelStaleMeeting();
        }

        private void CompleteMeeting()
        {
            if (completed) return;
            completed = true;
            Visible.Remove(this);
            // Release before completion can synchronously enqueue another speaker.
            ReleasePresentation();
            try { finished?.Invoke(); }
            finally { Destroy(gameObject); }
        }

        private void Update()
        {
            if (!ContextValid || presentation?.Interrupted == true)
            {
                CancelStaleMeeting();
                return;
            }
            if (view != null)
            {
                var active = ActiveMeeting == this;
                var settingsOpen = TownWindowManager.Instance && TownWindowManager.Instance.IsWindowOpen(TownWindowManager.Window.Options);
                if (active)
                {
                    conversationWait ??= TimelessEchoes.Hero.HeroController.Instance?.BeginNpcConversation(worldSubject);
                    if (!settingsOpen && dialogueRequested && view.AwaitingMeet) view.Advance();
                    if ((!view.AwaitingMeet || worldSubject) && presentation == null) TryBeginPresentation();
                }
                var guided = lessonNavigation && guidanceStarted;
                presentation?.SetSuspended(settingsOpen);
                presentation?.SetGuidance(guided);
                if (!closing && performance && presentation?.SubjectFramed == true && !settingsOpen)
                    performance.Begin(TimelessEchoes.Hero.HeroController.Instance?.transform);
                ApplyLayout();
                // Settings temporarily covers the lesson without completing or replacing it.
                var visible = active && !closing && !settingsOpen && (!lessonNavigation || !guidanceStarted || lessonNavigation.RefresherDestinationOpen);
                view.Root.style.display = visible
                    ? DisplayStyle.Flex : DisplayStyle.None;
                wind?.SetVisible(visible && !guided);
                if (lessonNavigation)
                {
                    // Dropdowns and their tutorial hand must remain visible and clickable.
                    // Restore the explanation once the player selects a destination.
                    view.AdvanceButton.SetEnabled(true);
                }
                meetHand.style.display = active && view.AwaitingMeet && definition.tutorialHand ? DisplayStyle.Flex : DisplayStyle.None;
                if (active && view.AwaitingMeet)
                {
                    // Bottom-mounted dialogue leaves the HUD beneath the card.
                    var buttonBounds = view.AdvanceButton.worldBound;
                    var anchor = view.Root.WorldToLocal(new Vector2(buttonBounds.center.x, buttonBounds.yMin));
                    var bob = 4 * (1 + Mathf.Cos(Time.unscaledTime * Mathf.PI * 2 / 1.2f));
                    meetHand.style.left = Mathf.Round(anchor.x - 12);
                    meetHand.style.top = Mathf.Round(anchor.y - 26 - bob);
                    meetHand.style.rotate = new Rotate(Angle.Degrees(180));
                }
            }
            if (animatedPortrait != null && speakerProfile)
            {
                var frame = speakerProfile.PortraitAt(Time.unscaledTime - portraitAnimationStartedAt);
                if (animatedPortrait.sprite != frame) animatedPortrait.sprite = frame;
            }
            else if (animatedPortrait != null && portraitFrames != null && portraitFrames.Count > 0)
            {
                var index = Mathf.FloorToInt((Time.unscaledTime - portraitAnimationStartedAt) * portraitFramesPerSecond) % portraitFrames.Count;
                var frame = portraitFrames[index];
                if (frame && animatedPortrait.sprite != frame) animatedPortrait.sprite = frame;
            }
        }

        private void ApplyLayout()
        {
            if (view == null) return;
            var portraitMode = ToolkitResponsiveLayout.IsPortrait;
            view.LayoutBottomDialogue(portraitMode);
            var maximum = Mathf.Min(portraitMode ? 220 : 180, ToolkitResponsiveLayout.SafeScreenArea.height - 140);
            var area = WorldPresentation.DialogueRect(view.PreferredHeight(Mathf.Max(76, maximum)));
            view.Root.style.left = Mathf.Round(area.xMin);
            view.Root.style.top = Mathf.Round(area.yMin);
            view.Root.style.width = area.width;
            view.Root.style.height = area.height;
            if (ActiveMeeting == this && presentation != null) presentation.SetDialogueRect(area);
            // Keep layout measurable while the camera settles, but don't cover
            // the actors or accept clicks through an invisible Meet prompt.
            var hidden = entrancePending || view.AwaitingMeet && worldSubject &&
                (presentation == null || !presentation.SubjectFramed || performance && !performance.Ready);
            view.Root.style.visibility = hidden ? Visibility.Hidden : Visibility.Visible;
            view.Root.SetEnabled(!hidden);
        }

        private void OnDisable()
        {
            // Keep the view/state across parent visibility changes; UIDocument detaches its tree.
            Visible.Remove(this);
            ReleasePresentation();
        }

        private void OnDestroy()
        {
            if (watchingLoads) Blindsided.EventHandler.OnLoadData -= Loaded;
            Visible.Remove(this);
            if (lessonNavigation) lessonNavigation.EndRefresherNavigation();
            ReleasePresentation();
            view?.Dispose();
            if (panel) Destroy(panel);
        }
    }
}
