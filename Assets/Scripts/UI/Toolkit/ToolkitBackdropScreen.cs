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
        private void OnEnable()
        {
            if (!theme || !runtimeTheme || !textSettings) return;
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 85;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "window-backdrop" };
            root.style.position = Position.Absolute;
            root.style.backgroundColor = new Color(0, 0, 0, .6f);
            root.style.backdropFilter = Blur();
            document.rootVisualElement.Add(root); Refresh();
        }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (root == null) return;
            root.style.display = TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow ? DisplayStyle.Flex : DisplayStyle.None;
            // Only controls use SafeArea. Dimming and input blocking cover the full viewport.
            root.style.left = root.style.top = root.style.right = root.style.bottom = 0;
        }
        private void OnDisable() { root?.RemoveFromHierarchy(); root = null; if (settings) Destroy(settings); settings = null; }
    }
}
