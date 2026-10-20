using Screen = UnityEngine.Device.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided;
using TimelessEchoes.Characters;
using UnityEngine;
using UnityEngine.UIElements;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ToolkitCharacterCreator : MonoBehaviour
    {
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings panel;
        private GameObject panelObject;
        private VisualElement root, sheet;
        private VisualElement colourPopup;
        private Button colourAnchor;
        private CharacterImage preview;
        private CharacterProfile owner, draft;
        private TextField nameInput;
        private Label status;
        private readonly Button[] presetLoad = new Button[3];
        private readonly List<Action> refreshChoices = new();
        private static readonly string[] HairColourOrder = {
            "Black", "Dark_Brown", "Brown", "Light_Brown", "Auburn", "Ginger",
            "Blonde", "Grey", "White", "Pink", "Purple", "Blue"
        };
        private static readonly string[] ClothingColourOrder = {
            "Black", "Grey", "White", "White_and_Brown", "Dark_Brown", "Brown", "Light_Brown",
            "Auburn", "Red", "Orange", "Ginger", "Blonde", "Green", "Blue", "Purple", "Pink"
        };
        private string pose = "Idle_Down";
        private int previewFacing;
        private float nextFrame;
        private bool optional;
        public bool IsOpen => root != null;
        public static bool BlocksWorldInput { get; private set; }

        private void Update()
        {
            if (!Oracle.oracle || !Oracle.oracle.HasLoadedCurrentSlotData) { if (IsOpen) Hide(); return; }
            if (IsOpen && owner != CharacterCreation.Current) Hide();
            if (!IsOpen && CharacterCreation.Required) Show(false);
            if (!IsOpen) return;
            var safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0 || safe.xMax > Screen.width + 1 || safe.yMax > Screen.height + 1)
                safe = new Rect(0, 0, Screen.width, Screen.height);
            var scale = ToolkitResponsiveLayout.Scale(new Vector2Int(Screen.width, Screen.height));
            var area = new Rect(safe.x * scale, (Screen.height - safe.yMax) * scale, safe.width * scale, safe.height * scale);
            sheet.style.width = Mathf.Min(600, area.width - 20);
            sheet.style.height = Mathf.Min(ToolkitResponsiveLayout.IsPortrait ? 720 : 400, area.height - 16);
            sheet.style.left = area.x + (area.width - Mathf.Min(600, area.width - 20)) / 2;
            sheet.style.top = area.y + (area.height - Mathf.Min(ToolkitResponsiveLayout.IsPortrait ? 720 : 400, area.height - 16)) / 2;
            sheet.EnableInClassList("character-narrow", area.width < 540);
            if (Time.unscaledTime < nextFrame) return;
            nextFrame = Time.unscaledTime + 1f / 12;
            preview.Set(draft, CharacterCatalog.Load().Frame(pose, Mathf.FloorToInt(Time.unscaledTime * 12)));
            var appearance = sheet.Q("character-image");
            if (appearance != null) appearance.style.scale = new Scale(new Vector3(previewFacing == 3 ? -1 : 1, 1, 1));
        }
        public bool Show(bool allowCancel = false)
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
            if (IsOpen) return true;
            var catalog = CharacterCatalog.Load();
            if (!theme || !runtimeTheme || !textSettings || !catalog || !Oracle.oracle || !Oracle.oracle.HasLoadedCurrentSlotData) return false;
            owner = CharacterCreation.Current; optional = allowCancel && !CharacterCreation.Required;
            draft = owner is { Modular: true } ? owner.Copy() : catalog.NewProfile();
            // Fresh profiles start with the catalog's authored default outfit.
            if (string.IsNullOrEmpty(draft.Chest)) {
                var savedName = draft.Name; draft = catalog.NewProfile(); draft.Name = savedName;
            }
            NormalizeAppearance();
            panel = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); panel.sortingOrder = 1000;
            // A child UIDocument inherits its parent's panel; this modal owns a separate top-level panel.
            panelObject = new GameObject("Character creation panel", typeof(UIDocument));
            var document = panelObject.GetComponent<UIDocument>(); document.panelSettings = panel;
            root = new VisualElement { name = "character-creator" }; root.AddToClassList("character-overlay");
            theme.Apply(root); ToolkitGameplay.Apply(root, theme); document.rootVisualElement.Add(root);
            BlocksWorldInput = true;
            sheet = E(root, "surface character-sheet");
            L(sheet, optional ? "Your character" : "Create your character", "heading");
            var content = E(sheet, "character-content");
            var left = E(content, "character-preview-column");
            var portraitRow = E(left, "character-portrait-row");
            var actions = new[] { "Idle", "Run", "Fish", "Mine", "Water", "Combat" };
            var rows = new[] { "Idle", "Running", "Fishing_Cast", "Mine", "Watering", "SwordAttack_1" };
            var actionIndex = 0; previewFacing = 0; pose = "Idle_Down";
            void UpdatePose() { pose = rows[actionIndex] + "_" + (previewFacing == 0 ? "Down" : previewFacing == 2 ? "Up" : "Right"); nextFrame = 0; }
            var turnLeft = B(portraitRow, "‹", () => { previewFacing = (previewFacing + 3) % 4; UpdatePose(); }, "inventory-character-turn character-turn");
            turnLeft.name = "character-preview-left"; turnLeft.tooltip = "Turn left";
            var previewBox = E(portraitRow, "character-preview"); preview = new CharacterImage(previewBox, 1.5f);
            var turnRight = B(portraitRow, "›", () => { previewFacing = (previewFacing + 1) % 4; UpdatePose(); }, "inventory-character-turn character-turn");
            turnRight.name = "character-preview-right"; turnRight.tooltip = "Turn right";
            var previewControls = E(left, "character-preview-controls");
            Label actionLabel = null;
            void ChangeAction(int step) { actionIndex = (actionIndex + step + actions.Length) % actions.Length; actionLabel.text = actions[actionIndex]; UpdatePose(); }
            var previousAction = Arrow(previewControls, false, () => ChangeAction(-1));
            previousAction.name = "character-preview-previous-action"; previousAction.tooltip = "Previous animation";
            actionLabel = L(previewControls, "Idle", "character-animation-label"); actionLabel.name = "character-preview-pose";
            var nextAction = Arrow(previewControls, true, () => ChangeAction(1));
            nextAction.name = "character-preview-next-action"; nextAction.tooltip = "Next animation";
            var right = E(content, "character-options");
            nameInput = new TextField("Name") { name = "character-name", value = draft.Name, maxLength = 24 };
            right.Add(nameInput); nameInput.RegisterValueChangedCallback(e => { draft.Name = e.newValue; status.text = ""; });
            var scroll = ToolkitControls.RecessedScroll(right, "character-options-scroll", theme);
            refreshChoices.Clear();
            foreach (var slot in new[] { CharacterSlot.Head, CharacterSlot.Chest, CharacterSlot.Legs, CharacterSlot.Feet })
            {
                var choices = catalog.parts.Where(p => p.slot == slot && !p.IsArmour).ToList();
                if (slot == CharacterSlot.Head) choices.Insert(0, null);
                var row = E(scroll.contentContainer, "character-option-row"); row.name = "character-option-" + slot;
                var heading = E(row, "row character-option-heading");
                L(heading, slot == CharacterSlot.Head ? "Hair" : slot == CharacterSlot.Chest ? "Top" : slot == CharacterSlot.Legs ? "Trousers" : "Shoes", "strong");
                var selectors = E(row, "row character-selectors");
                var styles = choices.Select(p => p?.Style ?? "").Distinct().ToList();
                var styleBox = E(selectors, "character-selector");
                var controls = E(styleBox, "row character-choice");
                Label label = null;
                Button colourButton = null;
                Label colourLabel = null;
                VisualElement selectedSwatch = null;
                List<CharacterCatalog.Part> Variants() => choices.Where(p => p != null && p.Style == catalog.Find(draft.Get(slot))?.Style).ToList();
                void Refresh() {
                    var selected = catalog.Find(draft.Get(slot));
                    label.text = selected?.StyleLabel ?? "None";
                    label.tooltip = "Style " + (styles.IndexOf(selected?.Style ?? "") + 1) + " of " + styles.Count;
                    colourLabel.text = selected?.Colour.Replace('_', ' ') ?? "";
                    PaintSwatch(selectedSwatch, selected?.Colour);
                    colourButton.style.display = string.IsNullOrEmpty(selected?.Colour) ? DisplayStyle.None : DisplayStyle.Flex;
                    colourButton.SetEnabled(Variants().Count > 1);
                    nextFrame = 0;
                }
                void ChangeStyle(int delta) {
                    CloseColourPicker();
                    var selected = catalog.Find(draft.Get(slot));
                    var index = styles.IndexOf(selected?.Style ?? "");
                    var style = styles[(Mathf.Max(0, index) + delta + styles.Count) % styles.Count];
                    var variants = choices.Where(p => p != null && p.Style == style).ToList();
                    var fallbackColour = catalog.Find(catalog.defaults.Get(slot))?.Colour;
                    var replacement = variants.FirstOrDefault(p => p.Colour == selected?.Colour)
                        ?? variants.FirstOrDefault(p => p.Colour == fallbackColour)
                        ?? variants.FirstOrDefault(p => p.Colour is "Brown" or "Black" or "Iron")
                        ?? variants.FirstOrDefault();
                    draft.Set(slot, replacement?.id ?? ""); Refresh();
                }
                var previous = Arrow(controls, false, () => ChangeStyle(-1)); previous.name = "character-previous-" + slot; previous.tooltip = "Previous style";
                label = L(controls, "", "character-choice-label"); label.name = "character-style-" + slot;
                var next = Arrow(controls, true, () => ChangeStyle(1)); next.name = "character-next-" + slot; next.tooltip = "Next style";
                previous.SetEnabled(styles.Count > 1); next.SetEnabled(styles.Count > 1);
                if (styles.Count == 1) { previous.style.display = DisplayStyle.None; next.style.display = DisplayStyle.None; }
                colourButton = B(selectors, "", () => OpenColourPicker(colourButton, slot, Variants(), draft.Get(slot), part => {
                    draft.Set(slot, part.id); Refresh();
                }), "character-colour-button");
                colourButton.name = "character-colour-picker-" + slot;
                selectedSwatch = E(colourButton, "character-colour-swatch");
                colourLabel = L(colourButton, "", "character-choice-label"); colourLabel.name = "character-colour-" + slot;
                L(colourButton, "▾", "small");
                refreshChoices.Add(Refresh); Refresh();
            }
            var presets = E(sheet, "character-presets"); L(presets, "Saved characters", "strong");
            var presetRow = E(presets, "row character-preset-row");
            for (var i = 0; i < 3; i++) {
                var index = i; var box = E(presetRow, "character-preset");
                presetLoad[i] = B(box, "Preset " + (i + 1), () => LoadPreset(index)); presetLoad[i].name = "character-load-preset-" + i;
                presetLoad[i].enableRichText = false;
                presetLoad[i].AddToClassList("character-load-preset");
                var save = B(box, "Save", () => SavePreset(index)); save.name = "character-save-preset-" + i;
                save.tooltip = "Save current character to slot " + (i + 1);
            }
            status = L(sheet, "", "character-status"); status.name = "character-status";
            var footer = E(sheet, "row character-footer");
            if (optional) B(footer, "Cancel", Hide);
            var confirm = B(footer, optional ? "Save character" : "Start game", Confirm); confirm.name = "character-confirm";
            RefreshPresets(); nextFrame = 0; nameInput.Focus(); return true;
        }
        private void SavePreset(int index)
        {
            draft.Name = nameInput.value;
            status.text = CharacterPresetStore.TrySave(index, draft, out var error) ? "Saved to preset " + (index + 1) + "." : error;
            RefreshPresets();
        }
        private void LoadPreset(int index)
        {
            if (!CharacterPresetStore.TryLoad(index, out var preset, out var error)) { status.text = error; return; }
            if (preset == null) return;
            // Preview a copy without changing the active save or replacing the current UI tree.
            draft = preset.Copy();
            CloseColourPicker(); NormalizeAppearance();
            nameInput.SetValueWithoutNotify(draft.Name);
            status.text = "";
            foreach (var refresh in refreshChoices) refresh();
            nextFrame = 0;
        }
        private void RefreshPresets()
        {
            for (var i = 0; i < 3; i++) {
                if (!CharacterPresetStore.TryLoad(i, out var preset, out var error)) { status.text = error; presetLoad[i].SetEnabled(false); continue; }
                presetLoad[i].text = string.IsNullOrWhiteSpace(preset?.Name) ? "Empty " + (i + 1) : preset.Name;
                presetLoad[i].tooltip = preset == null ? "No character saved" : "Load " + preset.Name;
                presetLoad[i].SetEnabled(preset != null);
            }
        }
        private void Confirm()
        {
            if (!CharacterProfile.TryName(nameInput.value, out var name, out var error)) { status.text = error; return; }
            NormalizeAppearance(); draft.Name = name; draft.Created = draft.Modular = true;
            Oracle.oracle.saveData.Character = draft.Copy();
            Blindsided.EventHandler.SaveData();
            if (!Oracle.oracle.SaveToSlot(Oracle.oracle.CurrentSlot)) {
                Oracle.oracle.saveData.Character = owner;
                status.text = "Your character could not be saved. Please try again."; return;
            }
            Hide();
        }
        public void Hide()
        {
            var wasOpen = root != null;
            if (root != null) BlocksWorldInput = false;
            CloseColourPicker();
            preview?.Set(null, 0);
            root?.RemoveFromHierarchy(); root = null; preview = null; owner = null;
            refreshChoices.Clear();
            sheet = null; nameInput = null; status = null; draft = null;
            Array.Clear(presetLoad, 0, presetLoad.Length);
            if (panelObject) Destroy(panelObject); panelObject = null;
            if (panel) Destroy(panel); panel = null;
            if (wasOpen) CharacterCatalog.Load().ReleaseUnused(CharacterEquipmentAppearance.Resolve(CharacterCreation.Current));
        }
        private void OnDisable() => Hide();

        private static Button Arrow(VisualElement parent, bool right, Action action, string classes = "")
        {
            var button = B(parent, "", action, "character-arrow " + classes);
            var icon = ToolkitControls.Icon(Resources.Load<Sprite>("UI/Icons/" + (right ? "ArrowRight" : "ArrowLeft")));
            icon.AddToClassList("character-arrow-icon"); button.Add(icon);
            return button;
        }

        private void CloseColourPicker()
        {
            colourPopup?.RemoveFromHierarchy(); colourPopup = null;
            colourAnchor?.Focus(); colourAnchor = null;
        }

        private void OpenColourPicker(Button anchor, CharacterSlot slot, List<CharacterCatalog.Part> variants, string selected, Action<CharacterCatalog.Part> choose)
        {
            if (colourAnchor == anchor) { CloseColourPicker(); return; }
            CloseColourPicker(); colourAnchor = anchor;
            colourPopup = E(root, "character-colour-popup"); colourPopup.name = "character-colour-popup";
            colourPopup.RegisterCallback<PointerDownEvent>(e => { if (e.target == colourPopup) { CloseColourPicker(); e.StopPropagation(); } });
            colourPopup.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { CloseColourPicker(); e.StopPropagation(); } });
            var menu = E(colourPopup, "surface character-colour-menu");
            var grid = E(menu, "character-colour-grid"); grid.name = "character-colours-" + slot;
            var order = slot == CharacterSlot.Head ? HairColourOrder : ClothingColourOrder;
            var colours = variants.Where(p => !string.IsNullOrEmpty(p.Colour))
                .OrderBy(p => { var index = Array.IndexOf(order, p.Colour); return index < 0 ? int.MaxValue : index; })
                .ThenBy(p => p.Colour, StringComparer.Ordinal).ToList();
            Button selectedButton = null;
            foreach (var part in colours) {
                var button = B(grid, "", () => { choose(part); CloseColourPicker(); }, "character-swatch-button");
                button.name = "character-colour-" + slot + "-" + part.Colour;
                button.tooltip = part.Colour.Replace('_', ' ');
                button.EnableInClassList("selected", part.id == selected);
                PaintSwatch(E(button, "character-colour-swatch"), part.Colour);
                if (part.id == selected) selectedButton = button;
            }
            void PositionMenu() {
                var bounds = root.worldBound;
                var point = root.WorldToLocal(anchor.worldBound.position);
                const float width = 130;
                var height = Mathf.Ceil(colours.Count / 4f) * 30 + 6;
                menu.style.width = width; menu.style.height = height;
                menu.style.left = Mathf.Clamp(point.x + anchor.worldBound.width - width, 6, Mathf.Max(6, bounds.width - width - 6));
                var below = point.y + anchor.worldBound.height;
                menu.style.top = below + height <= bounds.height - 6 ? below : Mathf.Max(6, point.y - height);
            }
            PositionMenu(); colourPopup.RegisterCallback<GeometryChangedEvent>(_ => PositionMenu());
            selectedButton?.Focus();
        }

        private void NormalizeAppearance()
        {
            draft.Accessory = "";
            var catalog = CharacterCatalog.Load();
            foreach (var slot in new[] { CharacterSlot.Head, CharacterSlot.Chest, CharacterSlot.Legs })
            {
                var part = catalog.Find(draft.Get(slot));
                if (part?.IsArmour != true) continue;
                var fallback = catalog.Find(catalog.defaults.Get(slot));
                var replacement = catalog.parts.FirstOrDefault(p => p.slot == slot && !p.IsArmour &&
                    p.Style == fallback?.Style && p.Colour == part.Colour) ?? fallback;
                draft.Set(slot, replacement?.id ?? "");
            }
        }

        private static void PaintSwatch(VisualElement swatch, string colour)
        {
            uint rgb = colour switch {
                "Black" => 0x363636, "Blonde" => 0xffeb57, "Brown" => 0x704643,
                "Dark_Brown" => 0x49302b, "Light_Brown" => 0xb58058, "Auburn" => 0xa14d36,
                "Ginger" => 0xffa214, "Grey" => 0x989898, "Blue" => 0x0098dc,
                "Green" => 0x1e6f50, "Orange" => 0xed7614, "Pink" => 0xf389f5,
                "Purple" => 0x93388f, "Red" => 0xea323c, "White" or "White_and_Brown" => 0xf9e6cf,
                _ => 0x514749
            };
            swatch.Clear(); swatch.style.backgroundColor = (Color)new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            if (colour == "White_and_Brown") {
                var second = E(swatch, "character-colour-secondary"); second.style.backgroundColor = (Color)new Color32(112, 70, 67, 255);
            }
        }
    }
}
