using Screen = UnityEngine.Device.Screen;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

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
        private IReadOnlyList<Sprite> portraitFrames;
        private Image animatedPortrait, meetHand;
        private float portraitFramesPerSecond, portraitAnimationStartedAt;
        private IReadOnlyList<string> lines, lineKeys;
        private Action finished;
        private bool initialized, completed;
        private bool continueAfterDialogue;
        private ToolkitNavigationScreen lessonNavigation;
        private IDisposable conversationWait;
        private Rect lastArea;
        private int lastIndex = -1;
        private bool lastPortrait;
        public static bool HasVisibleMeeting => Visible.Count > 0;
        public static bool HasOpenDialogue => Visible.Exists(meeting => meeting && meeting.view != null && !meeting.view.AwaitingMeet);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Visible.Clear();

        public void Init(Sprite npcPortrait, IReadOnlyList<string> dialogue, Action onFinished, IReadOnlyList<string> localizedLineKeys = null, bool continueAfterDialogue = false)
        {
            portrait = npcPortrait;
            lines = dialogue;
            lineKeys = localizedLineKeys;
            this.continueAfterDialogue = continueAfterDialogue;
            finished = onFinished;
            initialized = true;
            if (isActiveAndEnabled) Show();
        }

        private void OnEnable() { if (initialized && !completed) Show(); }
        public void BeginDialogue() => view?.Advance();

        public void GuideNavigation(ToolkitNavigationScreen navigation) => lessonNavigation = navigation;

        public void SetPortraitAnimation(IReadOnlyList<Sprite> frames, float framesPerSecond)
        {
            if (frames == null || frames.Count == 0 || view == null) return;
            portraitFrames = frames;
            portraitFramesPerSecond = Mathf.Max(1, framesPerSecond);
            portraitAnimationStartedAt = Time.unscaledTime;
            animatedPortrait = view.Root.Q<Image>("portrait");
            animatedPortrait.sprite = portraitFrames[0];
        }

        private void Show()
        {
            ToolkitResponsiveLayout.SetFocusView(false);
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;
            if (!theme || !template || !runtimeTheme || !textSettings || !definition || !definition.portraitFrame || !definition.shadow)
                throw new InvalidOperationException("Native meeting assets are not configured.");
            conversationWait ??= TimelessEchoes.Hero.HeroController.Instance?.BeginNpcConversation();
            if (!panel)
            {
                panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                panel.sortingOrder = 180;
            }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            view ??= new ToolkitMeetingView(theme, template, portrait, definition, lines, Finish, lineKeys, continueAfterDialogue);
            document.rootVisualElement.Add(view.Root);
            if (meetHand == null)
            {
                meetHand = new Image { name = "meeting-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
                meetHand.style.position = Position.Absolute;
                meetHand.style.width = meetHand.style.height = 24;
                view.Root.Add(meetHand);
            }
            if (!Visible.Contains(this)) Visible.Add(this);
            lastIndex = -1;
            ApplyLayout();
        }

        private void Finish()
        {
            if (completed) return;
            completed = true;
            conversationWait?.Dispose(); conversationWait = null;
            try { finished?.Invoke(); }
            finally { Destroy(gameObject); }
        }

        private void Update()
        {
            if (view != null)
            {
                ApplyLayout();
                if (ToolkitResponsiveLayout.IsPortrait)
                    view.FitPortrait(Mathf.Min(220, ToolkitResponsiveLayout.SafeScreenArea.height - 100), lessonNavigation ? 32 : 0);
                if (lessonNavigation)
                {
                    // Dropdowns and their tutorial hand must remain visible and clickable.
                    // Restore the explanation once the player selects a destination.
                    view.Root.style.display = lessonNavigation.HasOpenMenu ? DisplayStyle.None : DisplayStyle.Flex;
                    view.AdvanceButton.SetEnabled(lessonNavigation.RefresherDestinationOpen);
                }
                meetHand.style.display = view.AwaitingMeet && definition.tutorialHand ? DisplayStyle.Flex : DisplayStyle.None;
                if (view.AwaitingMeet)
                {
                    var aboveButton = ToolkitResponsiveLayout.IsPortrait;
                    var buttonBounds = view.AdvanceButton.worldBound;
                    var anchor = view.Root.WorldToLocal(new Vector2(buttonBounds.center.x, aboveButton ? buttonBounds.yMin : buttonBounds.yMax));
                    var bob = 4 * (1 + Mathf.Cos(Time.unscaledTime * Mathf.PI * 2 / 1.2f));
                    meetHand.style.left = Mathf.Round(anchor.x - 12);
                    meetHand.style.top = Mathf.Round(aboveButton ? anchor.y - 26 - bob : anchor.y + 2 + bob);
                    meetHand.style.rotate = new Rotate(Angle.Degrees(aboveButton ? 180 : 0));
                }
            }
            if (animatedPortrait != null && portraitFrames != null && portraitFrames.Count > 0)
            {
                var index = Mathf.FloorToInt((Time.unscaledTime - portraitAnimationStartedAt) * portraitFramesPerSecond) % portraitFrames.Count;
                var frame = portraitFrames[index];
                if (frame && animatedPortrait.sprite != frame) animatedPortrait.sprite = frame;
            }
        }

        private void ApplyLayout()
        {
            var portraitMode = ToolkitResponsiveLayout.IsPortrait;
            var area = portraitMode ? ToolkitResponsiveLayout.WorldArea :
                ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height),
                    Screen.safeArea, UnityEngine.Device.Application.isMobilePlatform ? 1 : SafeAreaRatio);
            var index = Visible.IndexOf(this);
            // A single active conversation owns the action in every orientation.
            // Stacked desktop cards can cover the earlier Meet/Next button.
            view.Root.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (area == lastArea && index == lastIndex && lastPortrait == portraitMode) return;
            lastArea = area; lastIndex = index; lastPortrait = portraitMode;
            view.LayoutPortrait(portraitMode);
            view.Root.style.width = portraitMode ? area.width - 16 : StyleKeyword.Null;
            view.Root.style.height = portraitMode ? Mathf.Min(168, area.height - 40) : StyleKeyword.Null;
            view.Root.style.left = Mathf.Round(area.xMin + 8 + (portraitMode ? 0 : index * 12));
            view.Root.style.top = Mathf.Round(area.yMin + (portraitMode ? 8 : 48 + index * 12));
        }

        private void OnDisable()
        {
            // Keep the view/state across parent visibility changes; UIDocument detaches its tree.
            Visible.Remove(this);
            conversationWait?.Dispose(); conversationWait = null;
        }

        private void OnDestroy()
        {
            Visible.Remove(this);
            conversationWait?.Dispose(); conversationWait = null;
            view?.Dispose();
            if (panel) Destroy(panel);
        }
    }
}
