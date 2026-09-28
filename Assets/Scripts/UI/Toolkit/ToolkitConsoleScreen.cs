using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using QFSW.QC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    /// <summary>Native command input/output; Quantum Console remains the command parser, not a UI.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitConsoleScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        public static ToolkitConsoleScreen Instance { get; private set; }
        public bool IsActive => root != null;
        private PanelSettings settings;
        private VisualElement root;
        private ScrollView scroll;
        private Label output, suggestions;
        private TextField input;
        private readonly Queue<string> logs = new();
        private readonly List<string> history = new();
        private readonly List<Task> jobs = new();
        private string[] commands = Array.Empty<string>();
        private int historyIndex;
        private bool dirty;
        private void OnEnable() { Instance = this; Application.logMessageReceived += OnLog; }
        private void OnLog(string message, string stack, LogType type) => LogToConsole(message);
        public void LogToConsole(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            logs.Enqueue(value.Length > 8192 ? value.Substring(0, 8192) : value);
            while (logs.Count > 1024) logs.Dequeue();
            dirty = true;
        }
        public void Toggle() { if (IsActive) Hide(); else Show(); }
        public void Show()
        {
            if (IsActive || !theme || !runtimeTheme || !textSettings) return;
            if (!QuantumConsoleProcessor.TableGenerated) QuantumConsoleProcessor.GenerateCommandTable();
            commands = QuantumConsoleProcessor.GetAllCommands().Select(c => c.CommandName).Distinct().OrderBy(c => c).ToArray();
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 950;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement(); root.style.position = Position.Absolute; root.style.paddingTop = root.style.paddingBottom = root.style.paddingLeft = root.style.paddingRight = 4;
            theme.Apply(root);ToolkitGameplay.Apply(root,theme); root.AddToClassList("surface");root.AddToClassList("compact-overlay"); document.rootVisualElement.Add(root);
            var header = new VisualElement(); header.style.flexDirection = FlexDirection.Row; root.Add(header);
            var title = ToolkitControls.Text("Console", ToolkitControls.TextRole.Heading); title.style.flexGrow = 1; header.Add(title);
            var close = ToolkitControls.Button("console-close", Hide, theme.button); close.text = "Close"; ToolkitControls.SetTextRole(close, ToolkitControls.TextRole.Body); close.style.minWidth = 32; header.Add(close);
            scroll = ToolkitControls.RecessedScroll(root, "console-log", theme);
            output = ToolkitControls.Text(""); output.enableRichText = false; output.style.color=StyleKeyword.Null; output.style.whiteSpace = WhiteSpace.Normal; scroll.Add(output);
            suggestions = ToolkitControls.Text(""); root.Add(suggestions);
            var entry = new VisualElement(); entry.style.flexDirection = FlexDirection.Row; root.Add(entry);
            input = new TextField { name = "console-input" }; input.AddToClassList("eov-number-field"); input.style.flexGrow = 1; entry.Add(input);
            input.RegisterValueChangedCallback(e => suggestions.text = string.Join("   ", commands.Where(c => !string.IsNullOrEmpty(e.newValue) && c.StartsWith(e.newValue, StringComparison.OrdinalIgnoreCase)).Take(8)));
            input.RegisterCallback<KeyDownEvent>(e => {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) Submit();
                else if (e.keyCode == KeyCode.UpArrow) Recall(-1);
                else if (e.keyCode == KeyCode.DownArrow) Recall(1);
                else if (e.keyCode == KeyCode.Tab) { var match = commands.FirstOrDefault(c => c.StartsWith(input.value, StringComparison.OrdinalIgnoreCase)); if (match != null) input.value = match + " "; }
                else return;
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            var submit = ToolkitControls.Button("console-submit", Submit, theme.button); submit.text = "Run"; ToolkitControls.SetTextRole(submit, ToolkitControls.TextRole.Body); submit.style.minWidth = 32; entry.Add(submit);
            var clear = ToolkitControls.Button("console-clear", () => { logs.Clear(); dirty = true; }, theme.button); clear.text = "Clear"; ToolkitControls.SetTextRole(clear, ToolkitControls.TextRole.Body); clear.style.minWidth = 32; entry.Add(clear);
            historyIndex = history.Count; dirty = true; input.schedule.Execute(() => input?.Focus());
        }
        private void Recall(int step) { historyIndex = Mathf.Clamp(historyIndex + step, 0, history.Count); input.value = historyIndex < history.Count ? history[historyIndex] : ""; }
        private void Submit() { var command = input.value; input.value = ""; Execute(command); input.Focus(); }
        public void Execute(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            if (!QuantumConsoleProcessor.TableGenerated) QuantumConsoleProcessor.GenerateCommandTable();
            history.Add(command); if (history.Count > 1024) history.RemoveAt(0); historyIndex = history.Count;
            LogToConsole("> " + command);
            try {
                if (command.Trim() == "clear") { logs.Clear(); dirty = true; return; }
                var result = QuantumConsoleProcessor.InvokeCommand(command);
                if (result is Task job) jobs.Add(job);
                else if (result != null) LogToConsole(new QuantumSerializer().SerializeFormatted(result));
            } catch (TargetInvocationException e) { LogToConsole(e.InnerException?.Message ?? e.Message); }
            catch (Exception e) { LogToConsole(e.Message); }
        }
        public void Hide() { root?.RemoveFromHierarchy(); root = null; input = null; if (settings) Destroy(settings); settings = null; }
        private void Update()
        {
            if (Keyboard.current?.backquoteKey.wasPressedThisFrame == true) Toggle();
            if (IsActive && Keyboard.current?.escapeKey.wasPressedThisFrame == true) Hide();
            for (int i = jobs.Count - 1; i >= 0; i--) if (jobs[i].IsCompleted) { var job = jobs[i]; jobs.RemoveAt(i); try { job.GetAwaiter().GetResult(); var result = job.GetType().GetProperty("Result")?.GetValue(job); if (result != null) LogToConsole(result.ToString()); } catch (Exception e) { LogToConsole(e.Message); } }
            if (!IsActive) return;
            var area = ToolkitWindowLayout.SafeArea; root.style.left = area.x; root.style.top = area.y; root.style.width = area.width; root.style.height = area.height * .65f;
            if (dirty) { output.text = string.Join("\n", logs); dirty = false; scroll.schedule.Execute(() => { if (IsActive) scroll.ScrollTo(output); }); }
        }
        private void OnDisable() { Application.logMessageReceived -= OnLog; Hide(); if (Instance == this) Instance = null; }
    }
}
