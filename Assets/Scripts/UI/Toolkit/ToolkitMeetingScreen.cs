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
        private IReadOnlyList<string> lines;
        private Action finished;
        private bool initialized, completed;
        private Rect lastArea;
        private int lastIndex = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Visible.Clear();

        public void Init(Sprite npcPortrait, IReadOnlyList<string> dialogue, Action onFinished)
        {
            portrait = npcPortrait;
            lines = dialogue;
            finished = onFinished;
            initialized = true;
            if (isActiveAndEnabled) Show();
        }

        private void OnEnable() { if (initialized && !completed) Show(); }

        private void Show()
        {
            if (!theme || !template || !runtimeTheme || !textSettings || !definition || !definition.portraitFrame || !definition.shadow)
                throw new InvalidOperationException("Native meeting assets are not configured.");
            if (!panel)
            {
                panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings);
                panel.sortingOrder = 90;
            }
            var document = GetComponent<UIDocument>();
            document.panelSettings = panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            view ??= new ToolkitMeetingView(theme, template, portrait, definition, lines, Finish);
            document.rootVisualElement.Add(view.Root);
            if (!Visible.Contains(this)) Visible.Add(this);
            lastIndex = -1;
            ApplyLayout();
        }

        private void Finish()
        {
            if (completed) return;
            completed = true;
            try { finished?.Invoke(); }
            finally { Destroy(gameObject); }
        }

        private void Update()
        {
            if (view != null) ApplyLayout();
        }

        private void ApplyLayout()
        {
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height),
                Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);
            var index = Visible.IndexOf(this);
            if (area == lastArea && index == lastIndex) return;
            lastArea = area; lastIndex = index;
            view.Root.style.left = area.x + index * 34;
            view.Root.style.top = area.y + 44;
        }

        private void OnDisable()
        {
            // Keep the view/state across parent visibility changes; UIDocument detaches its tree.
            Visible.Remove(this);
        }

        private void OnDestroy()
        {
            Visible.Remove(this);
            view?.Dispose();
            if (panel) Destroy(panel);
        }
    }
}
