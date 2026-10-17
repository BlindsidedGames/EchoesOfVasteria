#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TimelessEchoes.Hero;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.Editor
{
    // Review-only UI. Closing the window restores the shipping HUD; no save or gameplay values are changed.
    public sealed class RunHudPrototypes : EditorWindow
    {
        [SerializeField] private int option;
        [SerializeField] private bool expanded;
        private VisualElement original, preview, drawer, healthFill;
        private Label healthText;
        private readonly Dictionary<StatIconLookup.StatKey, Label> values = new();
        private double nextRefresh;
        private readonly Color foreground = (Color)new Color32(234, 221, 209, 255);

        [MenuItem("Tools/UI/Run HUD Prototypes")]
        public static void Open() => GetWindow<RunHudPrototypes>("Run HUD Prototypes");
        public static void ShowOption(int selected, bool expand = false)
        {
            var window = GetWindow<RunHudPrototypes>("Run HUD Prototypes");
            window.option = selected; window.expanded = expand; window.RemovePreview(); window.Tick(); window.Repaint();
        }
        private void OnEnable() => EditorApplication.update += Tick;
        private void OnDisable() { EditorApplication.update -= Tick; RemovePreview(); }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("In-run HUD options", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enter Play mode, then select an option. These previews use live character stats. Closing this window restores the current HUD.", MessageType.Info);
            var selected = GUILayout.Toolbar(option, new[] { "Current", "Compact pop-out" });
            if (selected != option) { option = selected; RemovePreview(); Tick(); }
            if (option != 0)
            {
                var show = EditorGUILayout.Toggle("Expand details", expanded);
                if (show != expanded) { expanded = show; RemovePreview(); Tick(); }
            }
            if (GUILayout.Button("Capture Game view"))
            {
                Directory.CreateDirectory("docs/ui-prototypes");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath($"docs/ui-prototypes/run-hud-{option}-{(expanded ? "expanded" : "compact")}.png"));
            }
        }
        private void RemovePreview()
        {
            if (original != null) original.style.visibility = Visibility.Visible;
            preview?.RemoveFromHierarchy(); preview = null; original = null; values.Clear();
        }
        private void Tick()
        {
            if (!EditorApplication.isPlaying) { RemovePreview(); return; }
            if (EditorApplication.timeSinceStartup < nextRefresh) return;
            nextRefresh = EditorApplication.timeSinceStartup + .1;
            if (option == 0) { if (preview != null) RemovePreview(); return; }
            if (preview == null || preview.panel == null)
            {
                RemovePreview();
                var run = UnityEngine.Object.FindAnyObjectByType<ToolkitRunScreen>();
                if (!run) return;
                original = run.GetComponent<UIDocument>().rootVisualElement.Q(className: "hero-corner");
                if (original?.parent == null) return;
                Build();
            }
            original.style.visibility = Visibility.Hidden;
            var snapshot = HeroStatSystem.GetSnapshot();
            var hero = HeroController.Instance;
            var health = hero ? hero.GetComponent<HeroHealth>() : null;
            var maximum = health ? health.MaxHealth : snapshot.maxHealth;
            var current = health ? health.CurrentHealth : maximum;
            healthText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(maximum)}";
            healthFill.style.width = Length.Percent(maximum > 0 ? Mathf.Clamp01(current / maximum) * 100 : 0);
            Value(StatIconLookup.StatKey.Damage, (hero ? hero.Damage : snapshot.damage).ToString("0.##"));
            Value(StatIconLookup.StatKey.AttackRate, snapshot.attacksPerSecond.ToString("0.##") + "/s");
            Value(StatIconLookup.StatKey.CritChance, snapshot.critChancePercent.ToString("0.#") + "%");
            Value(StatIconLookup.StatKey.CritDamage, snapshot.critDamagePercent.ToString("0.#") + "%");
            Value(StatIconLookup.StatKey.Defense, ((1 - Combat.ApplyDefense(1, snapshot.defense)) * 100).ToString("0.#") + "%");
            Value(StatIconLookup.StatKey.Regen, snapshot.healthRegenPerSecond.ToString("0.##") + "/s");
            Value(StatIconLookup.StatKey.MoveSpeed, (snapshot.movementSpeed / 3 * 100).ToString("0.#") + "%");
        }
        private void Value(StatIconLookup.StatKey key, string text) { if (values.TryGetValue(key, out var label)) label.text = text; }
        private VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            parent.Add(row); return row;
        }
        private Label Text(VisualElement parent, string text, int size = 8)
        {
            var label = new Label(text); label.style.color = foreground; label.style.fontSize = size;
            label.style.marginTop = label.style.marginBottom = label.style.marginLeft = label.style.marginRight = 0;
            parent.Add(label); return label;
        }
        private void Metric(VisualElement parent, StatIconLookup.StatKey key, string name)
        {
            var row = Row(parent); row.style.height = 13;
            StatIconLookup.TryGetIcon(key, out var sprite);
            var icon = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.style.width = icon.style.height = 9; icon.style.marginRight = 5; row.Add(icon);
            Text(row, name).style.flexGrow = 1;
            var value = Text(row, "—"); value.style.unityTextAlign = TextAnchor.MiddleRight;
            values[key] = value;
        }
        private void Surface(VisualElement element)
        {
            element.style.backgroundColor = (Color)new Color32(43, 36, 37, 255);
            element.style.borderLeftWidth = element.style.borderRightWidth = element.style.borderTopWidth = element.style.borderBottomWidth = .5f;
            element.style.borderLeftColor = element.style.borderRightColor = element.style.borderTopColor = element.style.borderBottomColor = (Color)new Color32(126, 104, 78, 255);
        }
        private void Build()
        {
            preview = new VisualElement { name = "run-hud-prototype" };
            preview.style.position = Position.Absolute; preview.style.left = 12; preview.style.bottom = 12;
            preview.style.width = 154; preview.style.height = 24;
            preview.style.flexDirection = FlexDirection.Row; preview.style.alignItems = Align.Center;
            preview.style.paddingLeft = 5; preview.style.paddingRight = 3;
            Surface(preview); original.parent.Add(preview);

            StatIconLookup.TryGetIcon(StatIconLookup.StatKey.Health, out var heartSprite);
            var heart = new Image { sprite = heartSprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            heart.style.width = heart.style.height = 10; heart.style.marginRight = 5; preview.Add(heart);
            var health = new VisualElement(); health.style.flexGrow = 1; health.style.marginRight = 6; preview.Add(health);
            healthText = Text(health, "—", 8);
            var track = new VisualElement(); track.style.height = 3; track.style.marginTop = 2;
            track.style.backgroundColor = (Color)new Color32(104, 76, 63, 255); health.Add(track);
            healthFill = new VisualElement(); healthFill.style.height = Length.Percent(100);
            healthFill.style.backgroundColor = (Color)new Color32(101, 185, 68, 255); track.Add(healthFill);
            var toggle = new Button(() => { expanded = !expanded; drawer.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None; Repaint(); }) { text = "Stats" , name = "prototype-stats-toggle" };
            toggle.tooltip = "Show combat stats"; toggle.style.fontSize = 8; toggle.style.color = foreground;
            toggle.style.width = 31; toggle.style.height = 18; toggle.style.marginLeft = toggle.style.marginRight = toggle.style.marginTop = toggle.style.marginBottom = 0;
            toggle.style.paddingLeft = toggle.style.paddingRight = toggle.style.paddingTop = toggle.style.paddingBottom = 0;
            Surface(toggle); preview.Add(toggle);

            drawer = new VisualElement { name = "prototype-stats-drawer" };
            drawer.style.position = Position.Absolute; drawer.style.left = 0; drawer.style.bottom = 28;
            drawer.style.width = 154; drawer.style.paddingLeft = drawer.style.paddingRight = 7;
            drawer.style.paddingTop = drawer.style.paddingBottom = 5;
            drawer.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            Surface(drawer); preview.Add(drawer);
            Metric(drawer, StatIconLookup.StatKey.Damage, "Damage");
            Metric(drawer, StatIconLookup.StatKey.AttackRate, "Attack speed");
            Metric(drawer, StatIconLookup.StatKey.CritChance, "Crit chance");
            Metric(drawer, StatIconLookup.StatKey.CritDamage, "Crit damage");
            var line = new VisualElement(); line.style.height = .5f; line.style.marginTop = line.style.marginBottom = 3;
            line.style.backgroundColor = (Color)new Color32(112, 86, 73, 255); drawer.Add(line);
            Metric(drawer, StatIconLookup.StatKey.Defense, "Damage reduction");
            Metric(drawer, StatIconLookup.StatKey.Regen, "Health regen");
            Metric(drawer, StatIconLookup.StatKey.MoveSpeed, "Movement speed");
        }
    }
}
#endif
