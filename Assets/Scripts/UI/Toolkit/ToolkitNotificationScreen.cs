using UnityEngine;
using UnityEngine.UIElements;
namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitNotificationScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, fill, frame;
        private Image icon;
        private Label text;
        private float expires, duration;
        private string localizedKey, localizedEnglish;
        private object[] localizedArguments;
        private int localizationRevision = -1;
        public bool IsConfigured => theme && runtimeTheme && textSettings;
        public void Show(Sprite sprite, string message, Sprite background, float seconds)
        {
            if (!IsConfigured) return;
            if (root == null)
            {
                settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 200;
                var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;
                root = new VisualElement { pickingMode = PickingMode.Ignore }; root.AddToClassList("eov-toast"); theme.Apply(root);ToolkitGameplay.Apply(root,theme); root.AddToClassList("surface");root.AddToClassList("compact-overlay"); document.rootVisualElement.Add(root);
                var row = new VisualElement { pickingMode = PickingMode.Ignore }; row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; root.Add(row);
                frame = new VisualElement { pickingMode = PickingMode.Ignore }; frame.style.width = frame.style.height = 26; row.Add(frame);
                icon = ToolkitControls.Icon(null); icon.style.width = icon.style.height = 24; frame.Add(icon);
                text = ToolkitControls.Text(""); text.style.flexShrink = 1; row.Add(text);
                fill = new VisualElement { pickingMode = PickingMode.Ignore }; fill.style.height = 2; fill.style.backgroundColor = new Color(.47f,.72f,.26f); root.Add(fill);
            }
            localizedKey = null; localizedArguments = null;
            icon.sprite = sprite;  text.text = message;
            duration = Mathf.Max(.01f, seconds); expires = Time.unscaledTime + duration; root.style.display = DisplayStyle.Flex;
        }
        /// <summary>Show a table-backed message without changing the notification lifetime or gameplay.</summary>
        public void ShowLocalized(Sprite sprite, string key, string english, Sprite background, float seconds, params object[] arguments)
        {
            Show(sprite, ToolkitLocalization.Text(key, english, arguments), background, seconds);
            localizedKey = key; localizedEnglish = english; localizedArguments = arguments;
        }
        private void Update()
        {
            if (root == null || root.style.display.value == DisplayStyle.None) return;
            if (localizedKey != null && localizationRevision != ToolkitLocalization.Revision)
            {
                localizationRevision = ToolkitLocalization.Revision;
                text.text = ToolkitLocalization.Text(localizedKey, localizedEnglish, localizedArguments);
            }
            var area = ToolkitWindowLayout.SafeArea; float width = Mathf.Min(240, area.width);
            root.style.left = area.center.x - width / 2; root.style.top = area.y + 44; root.style.width = width;
            float remaining = expires - Time.unscaledTime;
            fill.style.width = Length.Percent(Mathf.Clamp01(remaining / duration) * 100);
            if (remaining <= 0) root.style.display = DisplayStyle.None;
        }
        private void OnDisable() { root?.RemoveFromHierarchy(); root = null; if(settings)Destroy(settings); settings=null; }
    }
}
