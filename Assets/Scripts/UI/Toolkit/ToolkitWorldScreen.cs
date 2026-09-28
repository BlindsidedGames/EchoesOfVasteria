using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitWorldScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root;
        private readonly Dictionary<ToolkitWorldAnchor, VisualElement[]> views = new();
        private readonly List<ToolkitWorldAnchor> retired = new();
        private readonly Vector3[] corners = new Vector3[4];
        public int VisibleAnchorCount => views.Count;
        private void OnEnable()
        {
            if (!theme || !runtimeTheme || !textSettings) return;
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 10;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            root = document.rootVisualElement; root.pickingMode = PickingMode.Ignore; theme.Apply(root);
        }
        private void LateUpdate()
        {
            var camera = Camera.main; if (root == null || !camera) return;
            foreach (var pair in views) if (!pair.Key || !ToolkitWorldAnchor.Active.Contains(pair.Key)) retired.Add(pair.Key);
            foreach (var anchor in retired) { foreach (var node in views[anchor]) node.RemoveFromHierarchy(); views.Remove(anchor); } retired.Clear();
            foreach (var anchor in ToolkitWorldAnchor.Active)
            {
                // Test world bounds before allocating any visual nodes for pooled/off-screen content.
                var position = camera.WorldToViewportPoint(anchor.transform.position);
                bool inView = position.z > 0 && position.x > -.15f && position.x < 1.15f && position.y > -.2f && position.y < 1.2f;
                if (!views.TryGetValue(anchor, out var nodes))
                {
                    if (!inView) continue;
                    nodes = new VisualElement[anchor.elements.Length];
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        var data = anchor.elements[i];
                        VisualElement node = data.fontSize > 0 ? ToolkitControls.Text("") : data.sliced ? new VisualElement() : ToolkitControls.Icon(data.sprite);
                        node.style.position = Position.Absolute; node.pickingMode = PickingMode.Ignore;
                        if (node is Image image) image.tintColor = data.color;
                        else if (node is Label label) { label.style.color = data.color; label.style.unityTextAlign = TextAnchor.MiddleCenter; label.style.whiteSpace = WhiteSpace.NoWrap; }
                        else { ToolkitTheme.Background(node, data.sprite); node.style.unityBackgroundImageTintColor = data.color; if (!data.sprite) node.style.backgroundColor = data.color; }
                        nodes[i] = node; root.Add(node);
                    }
                    views.Add(anchor, nodes);
                }
                for (int i = 0; i < nodes.Length; i++)
                {
                    var data = anchor.elements[i]; var node = nodes[i];
                    bool visible = inView && data.enabled && data.anchor && data.anchor.gameObject.activeInHierarchy;
                    node.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; if (!visible) continue;
                    data.anchor.GetWorldCorners(corners);
                    if (data.rowGroup)
                    {
                        int visibleCount = 0, ordinal = 0;
                        foreach (var sibling in anchor.elements)
                            if (sibling.rowGroup == data.rowGroup && sibling.enabled && sibling.anchor.gameObject.activeInHierarchy)
                            { if (ReferenceEquals(sibling, data)) ordinal = visibleCount; visibleCount++; }
                        var centered = data.rowGroup.TransformPoint(new Vector3(data.rowGroup.rect.center.x + (ordinal - (visibleCount - 1) * .5f) * data.anchor.rect.width, data.anchor.localPosition.y, 0));
                        var shift = centered - (corners[0] + corners[2]) * .5f;
                        for (int k = 0; k < corners.Length; k++) corners[k] += shift;
                    }
                    var lower = camera.WorldToScreenPoint(corners[0]); var upper = camera.WorldToScreenPoint(corners[2]);
                    var a = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(lower.x, Screen.height - upper.y));
                    var b = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(upper.x, Screen.height - lower.y));
                    float width = Mathf.Abs(b.x - a.x), height = Mathf.Abs(b.y - a.y);
                    node.style.left = Mathf.Min(a.x,b.x); node.style.top = Mathf.Min(a.y,b.y);
                    node.style.width = width * anchor.Fill(data.value); node.style.height = height;
                    if (node is Label text) { text.text = anchor.Text(data); text.style.fontSize = data.fontSize * height / Mathf.Max(.01f,data.anchor.rect.height); }
                    else if ((data.value == ToolkitWorldAnchor.Value.HealthFill && anchor.health) || (data.value == ToolkitWorldAnchor.Value.EchoFill && anchor.echo))
                    {
                        var sprite = data.value == ToolkitWorldAnchor.Value.EchoFill ? anchor.echo.LifetimeSprite : anchor.health.GetHealthBarSprite(data.sprite);
                        if (node is Image image) image.sprite = sprite; else ToolkitTheme.Background(node, sprite);
                    }
                }
            }
        }
        private void OnDisable()
        {
            foreach(var nodes in views.Values) foreach(var node in nodes) node.RemoveFromHierarchy();
            views.Clear(); root=null; if(settings)Destroy(settings); settings=null;
        }
    }
}
