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
        private ToolkitShopScreen shop;
        private VisualElement root;
        public bool IsOpen => root != null;
        public static bool BlocksInput => (active && active.IsOpen) || Time.frameCount <= blockThroughFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { active = null; blockThroughFrame = -1; }

        public void Configure(ToolkitTheme theme, ThemeStyleSheet runtimeTheme, PanelTextSettings textSettings)
        { this.theme = theme; this.runtimeTheme = runtimeTheme; this.textSettings = textSettings; }

        public void Show(ToolkitShopScreen sourceShop = null)
        {
            if (IsOpen || !theme || !MildredAppearance.Colours) return;
            if (active && active != this) active.Hide();
            active = this;
            shop = sourceShop;
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
            var header = ToolkitGameplay.E(frame, "row between mildred-header companion-header");
            var title = ToolkitGameplay.L(header, "Mildred", "heading");
            ToolkitLocalization.Bind(title, "companion.title", "Companion");
            var preview = shop && !ToolkitCompanionChoices.QuestComplete;
            var choices = new ToolkitCompanionChoices(header, preview, shop ? Hide : null);
            var close = ToolkitGameplay.B(header, "×", Hide, "mildred-close"); close.name = "mildred-close";
            close.tooltip = ToolkitLocalization.Text("common.close", "Close");
            frame.Add(choices);
            root.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Escape) { Hide(); evt.StopPropagation(); } });
            (preview ? close : root.Q<Button>("companion-choice-" + MildredAppearance.Selected) ?? close).Focus();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (ToolkitMeetingScreen.HasOpenDialogue || WorldPresentation.IsBusy ||
                (shop ? !shop.IsOpen : TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow) ||
                Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                Mouse.current?.rightButton.wasReleasedThisFrame == true) Hide();
        }

        public void Hide()
        {
            if (!IsOpen) return;
            blockThroughFrame = Time.frameCount + 1;
            root.RemoveFromHierarchy(); root = null;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref settings);
            if (active == this) active = null;
            if (shop) shop.FocusCompanionPreview();
            shop = null;
        }
        private void OnDisable() => Hide();
    }
}
