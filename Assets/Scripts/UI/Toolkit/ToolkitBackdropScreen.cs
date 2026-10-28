using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitBackdropScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        [SerializeField] private Sprite background;
        internal const float BlurRadius = 3f;
        internal static List<FilterFunction> Blur()
        {
            var blur = new FilterFunction(FilterFunctionType.Blur);
            blur.AddParameter(new FilterParameter(BlurRadius));
            return new List<FilterFunction> { blur };
        }
        private PanelSettings settings;
        private VisualElement root;
        private ToolkitNavigationScreen navigation;
        private List<FilterFunction> desktopBlur;
        private void OnEnable()
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return;
            if (!theme || !runtimeTheme || !textSettings) return;
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 85;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "window-backdrop" };
            root.style.position = Position.Absolute;
            root.style.backgroundColor = new Color(0, 0, 0, .6f);
            desktopBlur = Blur();
            root.style.backdropFilter = desktopBlur;
            ToolkitTapDismiss.Bind(root, () => TownWindowManager.Instance?.CloseAllWindows(),
                () => !ToolkitDimScreen.BlocksInput && (!navigation || navigation.CanDismissWindow));
            document.rootVisualElement.Add(root); Refresh();
        }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (root == null) return;
            if (!navigation) navigation = FindAnyObjectByType<ToolkitNavigationScreen>();
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            var visible = !ToolkitResponsiveLayout.FocusView && TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow;
            root.style.backdropFilter = visible && !portrait ? desktopBlur : StyleKeyword.None;
            if (!visible) { root.RemoveFromHierarchy(); return; }
            root.style.display = DisplayStyle.Flex;
            root.style.backgroundColor = portrait && ToolkitResponsiveLayout.TownView ?
                ToolkitColorTheme.Surface : new Color(0, 0, 0, .6f);
            // Town sheets cover the world; in-run sheets leave the HUD unobscured.
            root.style.left = root.style.right = root.style.bottom = 0;
            root.style.top = portrait && !ToolkitResponsiveLayout.TownView ?
                ToolkitWindowLayout.SafeArea.y + (navigation && navigation.HasOpenMenu ? ToolkitResponsiveLayout.MenuTop : ToolkitResponsiveLayout.WindowTop) : 0;
            if (root.parent == null) GetComponent<UIDocument>().rootVisualElement.Add(root);
        }
        private void OnDisable() { root?.RemoveFromHierarchy(); root = null; if (settings) Destroy(settings); settings = null; }
    }
}
