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
        private UnityEngine.Localization.LocalizedString placeholderLocalized;
        private string importText = string.Empty, exportText = string.Empty;
        private string mode;
        private string statusKey, statusEnglish;
        private object[] statusArguments = Array.Empty<object>();
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
            {
                var outside = new Button(Hide) { name = "dialog-dismiss" }; outside.AddToClassList("eov-options-modal-dismiss"); root.Add(outside);
            }
            frame = new VisualElement { name = requested + "-dialog" }; frame.AddToClassList("eov-options-dialog"); frame.AddToClassList("surface"); root.Add(frame);
            if (requested == "language") BuildLanguages(); else BuildTransfer(requested == "import");
            document.rootVisualElement.Add(root); root.focusable=true; root.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Escape){Hide();e.StopPropagation();}});root.Focus(); Layout();
            ToolkitLocalization.Changed += RefreshLocalizedText;
            RefreshLocalizedText();
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
            var header = new VisualElement(); header.AddToClassList("eov-options-row"); header.style.height = 26; frame.Add(header);
            var title = Text(header, importing ? "import-title" : "export-title", 11.7f); title.style.flexGrow = 1; title.style.unityTextAlign = TextAnchor.MiddleLeft;
            var close = Button(header, "transfer-close", Hide, 22, false); close.style.width = 22; close.text="×";
            var inset = new VisualElement(); inset.AddToClassList("eov-options-input-frame");  frame.Add(inset);
            input = new TextField { name = "transfer-input", multiline = true, isReadOnly = !importing, value = importing ? importText : exportText };
            input.AddToClassList("eov-options-input");
            var placeholder = definition.Text("input-placeholder");
            placeholderLocalized = placeholder.localized;
            if (placeholderLocalized != null && !placeholderLocalized.IsEmpty)
                placeholderLocalized.StringChanged += SetPlaceholder;
            RefreshPlaceholder();
            input.RegisterValueChangedCallback(e => { if (importing) importText = e.newValue; else exportText = e.newValue; }); inset.Add(input);
            status = new Label { name = "transfer-status", enableRichText = false }; status.AddToClassList("eov-options-transfer-status"); frame.Add(status);
            var actions = new VisualElement(); actions.AddToClassList("eov-options-row"); frame.Add(actions);
            if (importing)
            {
                var paste = Button(actions, "paste", Paste, 22); paste.style.flexGrow = 1; paste.style.flexBasis = 0; paste.style.marginRight = 4;
                var import = Button(actions, "import-confirm", Import, 22); import.style.flexGrow = 1; import.style.flexBasis = 0;
            }
            else Button(actions, "copy", Copy, 22).style.flexGrow = 1;
        }
        private void Export()
        {
            try
            {
                var value = SaveImportExport.ExportCurrentSlot(copyToClipboard: true, out var durable);
                if (string.IsNullOrEmpty(value)) throw new InvalidOperationException("No in-memory save data is available to export.");
                input.value = value;
                if (durable) SetStatus("options.transfer.exported", "Exported to clipboard");
                else SetStatus("options.transfer.rescue-export", "Rescue export copied from memory; the disk save failed");
            }
            catch (Exception ex) { SetStatus("options.transfer.export-failed", "Export failed: {0}", ex.Message); }
        }
        private void Import()
        {
            try
            {
                var source = !string.IsNullOrWhiteSpace(input.value) ? input.value : GUIUtility.systemCopyBuffer;
                if (string.IsNullOrWhiteSpace(source)) { SetStatus("options.transfer.empty-import", "Nothing to import"); return; }
                if (SaveImportExport.TryImportToCurrentSlot(source, out var error, out var committed)) { Imported?.Invoke(); Hide(); }
                else if (committed) { Imported?.Invoke(); SetStatus("options.transfer.import-committed", "Imported safely to disk; restart the game or use Retry to finish loading it"); }
                else SetStatus("options.transfer.import-failed", "Import failed: {0}", error);
            }
            catch (Exception ex) { if (status != null) SetStatus("options.transfer.import-exception", "Import exception: {0}", ex.Message); }
        }
        private void Copy() { try { GUIUtility.systemCopyBuffer = input.value ?? string.Empty; SetStatus("options.transfer.copied", "Copied export string"); } catch (Exception ex) { SetStatus("options.transfer.copy-failed", "Copy failed: {0}", ex.Message); } }
        private void Paste() { try { input.value = GUIUtility.systemCopyBuffer ?? string.Empty; } catch (Exception ex) { SetStatus("options.transfer.paste-failed", "Paste failed: {0}", ex.Message); } }
        private void SetStatus(string key, string english, params object[] arguments)
        {
            statusKey = key; statusEnglish = english; statusArguments = arguments;
            RefreshLocalizedText();
        }
        private void SetPlaceholder(string value)
        {
            if (input != null) input.textEdition.placeholder = string.IsNullOrEmpty(value)
                ? definition.Text("input-placeholder").fallback : value;
        }
        private void RefreshPlaceholder()
        {
            if (input == null) return;
            var text = definition.Text("input-placeholder");
            if (placeholderLocalized == null || placeholderLocalized.IsEmpty)
                input.textEdition.placeholder = ToolkitLocalization.Text(text.key, text.fallback);
        }
        private void RefreshLocalizedText()
        {
            RefreshPlaceholder();
            if (status != null && !string.IsNullOrEmpty(statusKey))
                status.text = ToolkitLocalization.Text(statusKey, statusEnglish, statusArguments);
        }
        private void BuildLanguages()
        {
            frame.style.width = 160; frame.style.paddingLeft = frame.style.paddingRight = frame.style.paddingTop = frame.style.paddingBottom = 8;
            var inset = new VisualElement(); inset.style.paddingLeft = inset.style.paddingRight = inset.style.paddingTop = inset.style.paddingBottom = 2;
             frame.Add(inset);
            var languages = new[] { ("en", "English"), ("fr", "Français"), ("de", "Deutsch"),
                ("es-419", "Español (Latinoamérica)"), ("pt-BR", "Português (Brasil)"),
                ("zh-CN", "简体中文"), ("ru", "Русский"), ("ja", "日本語") };
            foreach (var language in languages)
            {
                var code = language.Item1;
                var button = Button(inset, "locale-" + code, () =>
                {
                    Hide(); FindAnyObjectByType<LocalizationManager>()?.SelectLocale(code);
                }, 24, false);
                var label = new Label(language.Item2) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("eov-options-label"); label.style.fontSize = 7;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                button.Add(label); button.style.marginBottom = 4;
            }
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
            ToolkitLocalization.Changed -= RefreshLocalizedText;
            if (placeholderLocalized != null && !placeholderLocalized.IsEmpty)
                placeholderLocalized.StringChanged -= SetPlaceholder;
            placeholderLocalized = null;
            statusKey = statusEnglish = null; statusArguments = Array.Empty<object>();
            foreach (var binding in bindings) binding.Dispose(); bindings.Clear();
            root?.RemoveFromHierarchy(); root = null; frame = null; input = null; status = null;
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (panel) Destroy(panel); }
    }
}

