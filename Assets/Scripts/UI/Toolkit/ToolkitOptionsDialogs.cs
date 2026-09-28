using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitOptionsDialogs : MonoBehaviour
    {
        [SerializeField] private ToolkitOptionsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings panel;
        private VisualElement root, frame;
        private TextField input;
        private Label status;
        private string importText = string.Empty, exportText = string.Empty;
        private string mode;
        private readonly List<ToolkitTextBinding> bindings = new();
        public bool IsOpen => root != null;
        public event Action Imported;

        public void Open(string requested)
        {
            Hide(); mode = requested;
            if (!panel) { panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 300; }
            var document = GetComponent<UIDocument>(); document.panelSettings = panel; document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "options-dialog-root", pickingMode = PickingMode.Ignore };
            root.style.position = Position.Absolute; root.style.left = root.style.right = root.style.top = root.style.bottom = 0;
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            if (requested == "language")
            {
                var outside = new Button(Hide) { name = "language-dismiss" }; outside.AddToClassList("eov-options-modal-dismiss"); root.Add(outside);
            }
            frame = new VisualElement { name = requested + "-dialog" }; frame.AddToClassList("eov-options-dialog"); frame.AddToClassList("surface"); root.Add(frame);
            if (requested == "language") BuildLanguages(); else BuildTransfer(requested == "import");
            document.rootVisualElement.Add(root); Layout();
            if (requested == "export") Export();
        }
        private Label Text(VisualElement parent, string id, float size)
        {
            var label = new Label { name = id, pickingMode = PickingMode.Ignore }; label.AddToClassList("eov-options-label"); label.style.fontSize = size;
            parent.Add(label); bindings.Add(new ToolkitTextBinding(label, definition.Text(id))); return label;
        }
        private Button Button(VisualElement parent, string id, Action action, float height, bool label = true)
        {
            var button = new Button(action) { name = id }; button.AddToClassList("eov-options-button"); button.style.height = height;
            button.AddToClassList("button"); parent.Add(button);
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) Audio.AudioManager.Instance?.PlayUIButtonClick(); });
            button.RegisterCallback<NavigationSubmitEvent>(_ => Audio.AudioManager.Instance?.PlayUIButtonClick());
            if (label) Text(button, id + "-label", 8).style.unityTextAlign = TextAnchor.MiddleCenter;
            return button;
        }
        private void BuildTransfer(bool importing)
        {
            frame.AddToClassList("eov-options-transfer-dialog");
            var header = new VisualElement(); header.AddToClassList("eov-options-row"); header.style.height = 16; frame.Add(header);
            var title = Text(header, importing ? "import-title" : "export-title", 11.7f); title.style.flexGrow = 1; title.style.unityTextAlign = TextAnchor.MiddleCenter;
            var close = Button(header, "transfer-close", Hide, 16, false); close.style.width = 16; close.text="×";
            var inset = new VisualElement(); inset.AddToClassList("eov-options-input-frame");  frame.Add(inset);
            input = new TextField { name = "transfer-input", multiline = true, value = importing ? importText : exportText };
            input.AddToClassList("eov-options-input"); input.textEdition.placeholder = definition.Text("input-placeholder").fallback;
            input.RegisterValueChangedCallback(e => { if (importing) importText = e.newValue; else exportText = e.newValue; }); inset.Add(input);
            status = new Label { name = "transfer-status", enableRichText = false }; status.AddToClassList("eov-options-transfer-status"); frame.Add(status);
            var actions = new VisualElement(); actions.AddToClassList("eov-options-row"); frame.Add(actions);
            if (importing)
            {
                var paste = Button(actions, "paste", Paste, 18); paste.style.flexGrow = 1; paste.style.flexBasis = 0; paste.style.marginRight = 2;
                var import = Button(actions, "import-confirm", Import, 18); import.style.flexGrow = 1; import.style.flexBasis = 0;
            }
            else Button(actions, "copy", Copy, 18).style.flexGrow = 1;
        }
        private void Export()
        {
            try
            {
                var value = SaveImportExport.ExportCurrentSlot(copyToClipboard: true, out var durable);
                if (string.IsNullOrEmpty(value)) throw new InvalidOperationException("No in-memory save data is available to export.");
                input.value = value;
                status.text = durable ? "Exported to clipboard" : "Rescue export copied from memory; the disk save failed";
            }
            catch (Exception ex) { status.text = $"Export failed: {ex.Message}"; }
        }
        private void Import()
        {
            try
            {
                var source = !string.IsNullOrWhiteSpace(input.value) ? input.value : GUIUtility.systemCopyBuffer;
                if (string.IsNullOrWhiteSpace(source)) { status.text = "Nothing to import"; return; }
                if (SaveImportExport.TryImportToCurrentSlot(source, out var error, out var committed)) { Imported?.Invoke(); Hide(); }
                else if (committed) { Imported?.Invoke(); status.text = "Imported safely to disk; restart the game or use Retry to finish loading it"; }
                else status.text = $"Import failed: {error}";
            }
            catch (Exception ex) { if (status != null) status.text = $"Import exception: {ex.Message}"; }
        }
        private void Copy() { try { GUIUtility.systemCopyBuffer = input.value ?? string.Empty; status.text = "Copied export string"; } catch (Exception ex) { status.text = $"Copy failed: {ex.Message}"; } }
        private void Paste() { try { input.value = GUIUtility.systemCopyBuffer ?? string.Empty; } catch (Exception ex) { status.text = $"Paste failed: {ex.Message}"; } }
        private void BuildLanguages()
        {
            frame.style.width = 140.14f; frame.style.paddingLeft = frame.style.paddingRight = frame.style.paddingTop = frame.style.paddingBottom = 3;
            var inset = new VisualElement(); inset.style.paddingLeft = inset.style.paddingRight = inset.style.paddingTop = inset.style.paddingBottom = 2;
             frame.Add(inset);
            foreach (var code in new[] { "en", "ru" })
            {
                var button = Button(inset, "locale-" + code, () =>
                {
                    Hide(); FindAnyObjectByType<LocalizationManager>()?.SelectLocale(code);
                    if (code == "ru") ShowTranslationWarning();
                }, 16.54f);
                button.Q<Label>().style.fontSize = 7; if (code == "en") button.style.marginBottom = 2;
            }
        }
        private void ShowTranslationWarning()
        {
            var document = GetComponent<UIDocument>();
            root = new VisualElement { name = "translation-warning-root", pickingMode = PickingMode.Ignore }; theme.Apply(root);ToolkitGameplay.Apply(root,theme);
            var button = new Button(Hide) { name = "translation-warning" }; frame = button;
            frame.AddToClassList("eov-options-dialog"); frame.style.width = 188.96f;
            frame.style.paddingLeft = frame.style.paddingRight = frame.style.paddingTop = frame.style.paddingBottom = 3;
            frame.AddToClassList("surface");
            var inset = new VisualElement(); inset.style.paddingLeft = inset.style.paddingRight = inset.style.paddingTop = inset.style.paddingBottom = 2;
             frame.Add(inset);
            var label = Text(inset, "translation-warning-label", 7); label.style.whiteSpace = WhiteSpace.Normal; label.style.color = StyleKeyword.Null; label.style.letterSpacing = .14f; label.style.unityFontStyleAndWeight = FontStyle.Normal; label.style.unityTextOutlineWidth = 0; label.style.textShadow = new TextShadow(); label.style.minHeight = 47.68f;
            root.Add(frame); document.rootVisualElement.Add(root); Layout();
        }
        private void Layout()
        {
            if (frame == null) return;
            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea,
                Application.isMobilePlatform ? 1 : Blindsided.SaveData.StaticReferences.SafeAreaRatio);
            frame.style.left = area.center.x; frame.style.top = area.center.y; frame.style.maxWidth = area.width; frame.style.maxHeight = area.height;
        }
        private void Update() { if (IsOpen) Layout(); }
        public void Hide()
        {
            foreach (var binding in bindings) binding.Dispose(); bindings.Clear();
            root?.RemoveFromHierarchy(); root = null; frame = null; input = null; status = null;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (panel) Destroy(panel); }
    }
}


