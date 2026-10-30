using TimelessEchoes.NPC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitMildredPicker : MonoBehaviour
    {
        private static ToolkitMildredPicker active;
        private static int blockThroughFrame = -1;
        private ToolkitTheme theme;
        private ThemeStyleSheet runtimeTheme;
        private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root;
        private readonly Image[] previews = new Image[MildredColours.Count];
        private readonly Button[] choices = new Button[MildredColours.Count];
        private readonly string[] names = { "Original", "White", "Ginger", "Brown", "Silver" };
        public bool IsOpen => root != null;
        public static bool BlocksInput => (active && active.IsOpen) || Time.frameCount <= blockThroughFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { active = null; blockThroughFrame = -1; }

        public void Configure(ToolkitTheme theme, ThemeStyleSheet runtimeTheme, PanelTextSettings textSettings)
        { this.theme = theme; this.runtimeTheme = runtimeTheme; this.textSettings = textSettings; }

        public void Show()
        {
            if (IsOpen || !theme || !MildredAppearance.Colours) return;
            if (active && active != this) active.Hide();
            active = this;
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 300;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "mildred-colour-picker", focusable = true };
            theme.Apply(root); ToolkitGameplay.Apply(root, theme);
            root.styleSheets.Add(Resources.Load<StyleSheet>("UI/MildredColours"));
            root.AddToClassList("mildred-picker");
            document.rootVisualElement.Add(root);
            ToolkitGameplay.B(root, "", Hide, "mildred-dismiss").name = "mildred-dismiss";
            var frame = ToolkitGameplay.E(root, "surface mildred-dialog"); frame.name = "mildred-dialog";
            var header = ToolkitGameplay.E(frame, "row between mildred-header");
            var title = ToolkitGameplay.L(header, "Mildred", "heading");
            ToolkitLocalization.Bind(title, "mildred.colours.title", "Mildred");
            var close = ToolkitGameplay.B(header, "×", Hide, "mildred-close"); close.name = "mildred-close";
            close.tooltip = ToolkitLocalization.Text("common.close", "Close");
            var row = ToolkitGameplay.E(frame, "row mildred-choices");
            for (var i = 0; i < choices.Length; i++)
            {
                var colour = i;
                var button = choices[i] = ToolkitGameplay.B(row, "", () => Choose(colour), "mildred-choice");
                button.name = "mildred-colour-" + names[i].ToLowerInvariant();
                previews[i] = ToolkitGameplay.Icon(button, MildredAppearance.Colours.Frames(i)[0], 48);
                var label = ToolkitGameplay.L(button, names[i], "mildred-colour-name");
                ToolkitLocalization.Bind(label, "mildred.colours." + names[i].ToLowerInvariant(), names[i]);
                button.EnableInClassList("active", i == MildredAppearance.Selected);
            }
            root.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) { Hide(); evt.StopPropagation(); } });
            choices[MildredAppearance.Selected].Focus();
        }

        private void Choose(int colour)
        {
            MildredAppearance.Select(colour);
            for (var i = 0; i < choices.Length; i++) choices[i].EnableInClassList("active", i == colour);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (ToolkitMeetingScreen.HasOpenDialogue || WorldPresentation.IsBusy ||
                (TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow)) { Hide(); return; }
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Hide(); return; }
            var frame = Mathf.FloorToInt(Time.unscaledTime * 12) % 20;
            for (var i = 0; i < previews.Length; i++) previews[i].sprite = MildredAppearance.Colours.Frames(i)[frame];
        }

        public void Hide()
        {
            if (!IsOpen) return;
            blockThroughFrame = Time.frameCount + 1;
            root.RemoveFromHierarchy(); root = null;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref settings);
            if (active == this) active = null;
        }
        private void OnDisable() => Hide();
    }
}
