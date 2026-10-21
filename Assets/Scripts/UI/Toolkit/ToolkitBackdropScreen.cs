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
            document.rootVisualElement.Add(root); Refresh();
        }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (root == null) return;
            if (!navigation) navigation = FindAnyObjectByType<ToolkitNavigationScreen>();
            var portrait = ToolkitResponsiveLayout.IsPortrait;
            root.style.display = !ToolkitResponsiveLayout.FocusView && TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow ? DisplayStyle.Flex : DisplayStyle.None;
            root.style.backdropFilter = portrait ? StyleKeyword.None : desktopBlur;
            root.style.backgroundColor = portrait && ToolkitResponsiveLayout.TownView ?
                (Color)new Color32(41, 36, 38, 255) : new Color(0, 0, 0, .6f);
            // Town sheets cover the world; in-run sheets leave the HUD unobscured.
            root.style.left = root.style.right = root.style.bottom = 0;
            root.style.top = portrait && !ToolkitResponsiveLayout.TownView ?
                ToolkitWindowLayout.SafeArea.y + (navigation && navigation.HasOpenMenu ? ToolkitResponsiveLayout.MenuTop : ToolkitResponsiveLayout.WindowTop) : 0;
        }
        private void OnDisable() { root?.RemoveFromHierarchy(); root = null; if (settings) Destroy(settings); settings = null; }
    }
}
