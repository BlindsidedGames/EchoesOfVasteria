using System.Collections.Generic;

using System.Linq;
using System.Text.RegularExpressions;
using TimelessEchoes.Upgrades;

using TimelessEchoes.Skills;

using UnityEngine;

using UnityEngine.UIElements;

using static Blindsided.SaveData.StaticReferences;



namespace TimelessEchoes.UI.Toolkit

{

    [RequireComponent(typeof(UIDocument))]

    public sealed class ToolkitSkillsScreen : MonoBehaviour

    {

        [SerializeField] private ToolkitSkillsDefinition definition;

        [SerializeField] private ToolkitTheme theme;

        [SerializeField] private ThemeStyleSheet runtimeTheme;

        [SerializeField] private PanelTextSettings textSettings;

        private PanelSettings settings;

        private SkillController controller;

        private VisualElement root, xpFill, setFrame;

        private Label summaryTitle;
        private VisualElement active, totals, allTotals;
        private Label level, xp, slots, primaryName, primaryEffect, secondaryName, secondaryEffect;

        private ScrollView milestoneList;

        private readonly List<Label> selectorLevels = new();

        private readonly List<VisualElement> selections = new(), highlights = new();

        private ToolkitTextBinding titleBinding;

        private int selected;

        private bool dirty;

        private Skill Current => definition.skills.Length > 0 ? definition.skills[Mathf.Clamp(selected, 0, definition.skills.Length - 1)] : null;

        public bool IsOpen => root != null;

        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;



        public bool Show()

        {

            if (IsOpen) return true;

            if (!IsConfigured) return false;

            controller = SkillController.Instance; if (!controller) return false;

            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }

            var document = GetComponent<UIDocument>(); document.panelSettings = settings; document.rootVisualElement.pickingMode = PickingMode.Ignore;

            root = new VisualElement { name = "skills" }; root.AddToClassList("eov-skills"); theme.Apply(root);ToolkitGameplay.Apply(root,theme); document.rootVisualElement.Add(root);

            var left = new VisualElement(); left.AddToClassList("eov-skills-left"); root.Add(left);

            var header = Frame(left, definition.frame); header.AddToClassList("eov-skills-header");

            var selectorRow = new VisualElement(); selectorRow.AddToClassList("eov-skills-selectors"); header.Add(selectorRow);

            for (var i = 0; i < definition.skills.Length; i++)

            {

                var index = i; var button = ToolkitBuffsScreen.MakeButton("skill-" + definition.skills[i].name, () => Select(index), definition.slot);

                button.AddToClassList("eov-skills-selector"); if (i == definition.skills.Length - 1) button.style.marginRight = 0; selectorRow.Add(button);

                var icon = new Image { sprite = definition.icons[i], scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; icon.AddToClassList("skill-selector-icon"); button.Add(icon);

                var selection = Overlay(button, definition.selection); selections.Add(selection);

                var highlight = Overlay(button, definition.highlight); highlight.style.display = DisplayStyle.None; highlights.Add(highlight);

                button.RegisterCallback<PointerEnterEvent>(_ => highlight.style.display = DisplayStyle.None);

                var label = Text(button, "", 6); label.AddToClassList("eov-skill-selector-level"); label.style.color = new Color(.918f, .831f, .667f); selectorLevels.Add(label);

            }

            var info = Frame(header, definition.inset); info.AddToClassList("eov-skill-info");

            level = Text(info, "", 5); level.name = "skill-level"; xp = Text(info, "", 5); xp.name = "skill-xp";

            level.style.color = xp.style.color = new Color(.918f, .831f, .667f);

            var track = new VisualElement(); track.AddToClassList("eov-skill-xp-track"); track.AddToClassList("track"); header.Add(track);

            xpFill = new VisualElement(); xpFill.style.height = 4; xpFill.style.overflow = Overflow.Hidden; track.Add(xpFill);

            var xpSprite = new VisualElement { pickingMode = PickingMode.Ignore }; xpSprite.style.position = Position.Absolute; xpSprite.style.height = 4;

            xpSprite.AddToClassList("fill"); xpFill.Add(xpSprite);

            track.RegisterCallback<GeometryChangedEvent>(_ => xpSprite.style.width = track.contentRect.width);

            setFrame = Frame(left, definition.frame); setFrame.AddToClassList("eov-skill-set");

            var setInset = Frame(setFrame, definition.inset); setInset.style.paddingTop = setInset.style.paddingBottom = setInset.style.paddingLeft = setInset.style.paddingRight = 0;

            primaryName = Text(setInset, "", 7); primaryEffect = Text(setInset, "", 5); secondaryName = Text(setInset, "", 7); secondaryEffect = Text(setInset, "", 5);

            var summaryFrame = Frame(left, definition.frame); summaryFrame.style.flexGrow = 1; summaryFrame.style.minHeight = 0; summaryFrame.style.marginTop = 0;

            var summaryInset = Frame(summaryFrame, definition.inset); summaryInset.style.paddingTop = summaryInset.style.paddingBottom = summaryInset.style.paddingLeft = summaryInset.style.paddingRight = 0; summaryInset.style.flexGrow = 1; summaryInset.style.minHeight = 0;

            var summaryScroll = Scroll(summaryInset, "skill-summary"); summaryScroll.contentContainer.AddToClassList("eov-skill-summary-content");

            slots = Text(summaryScroll, "", 7); slots.name = "active-slots"; active = ToolkitGameplay.E(summaryScroll, "skill-summary-lines"); active.name = "active-milestones";

            var title = summaryTitle = Text(summaryScroll, "", 7); title.AddToClassList("skill-summary-title");

            totals = ToolkitGameplay.E(summaryScroll, "skill-summary-lines"); totals.name = "skill-totals";
            var allButton = ToolkitGameplay.B(summaryScroll, ToolkitLocalization.Text("skills.all-bonuses", "All bonuses"), () =>
            {
                var expanded = allTotals.style.display != DisplayStyle.None;
                allTotals.style.display = expanded ? DisplayStyle.None : DisplayStyle.Flex;
                summaryScroll.Q<Button>("all-bonuses").EnableInClassList("active", !expanded);
            }, "skill-all-bonuses"); allButton.name = "all-bonuses"; ToolkitLocalization.Bind(allButton, "skills.all-bonuses", "All bonuses");
            allTotals = ToolkitGameplay.E(summaryScroll, "skill-summary-lines"); allTotals.style.display = DisplayStyle.None;

            foreach (var label in new VisualElement[] { slots, active, title, totals }) label.style.marginBottom = 4;

            var right = Frame(root, definition.rightFrame); right.AddToClassList("eov-skills-right");

            var inset = Frame(right, definition.inset); inset.AddToClassList("eov-skills-milestone-inset"); milestoneList = Scroll(inset, "milestones");

            controller.OnExperienceGained += Experience; controller.OnLevelUp += LevelUp;

            controller.OnMilestoneDataChanged += Changed; controller.OnActiveSlotsChanged += ActiveChanged;

            ToolkitLocalization.Changed += Changed;
            ShowLevelTextChanged += Changed; Blindsided.EventHandler.OnLoadData += Changed;

            Refresh(); Layout(); return true;

        }

        private static VisualElement Overlay(VisualElement parent, Sprite sprite)

        {

            var element = new VisualElement { pickingMode = PickingMode.Ignore }; element.style.position = Position.Absolute;

            element.style.left = element.style.right = element.style.top = element.style.bottom = -1; element.AddToClassList("selection-outline"); element.style.unitySliceScale = .5f; parent.Add(element); return element;

        }

        private static VisualElement Frame(VisualElement parent, Sprite sprite)

        {

            var frame = new VisualElement(); frame.AddToClassList("eov-skill-frame");  parent.Add(frame); return frame;

        }

        private static Label Text(VisualElement parent, string text, float size)

        {

            var label = new Label(text) { pickingMode = PickingMode.Ignore }; label.AddToClassList("eov-skill-text"); label.style.fontSize = Mathf.Max(7,size);

            label.RegisterCallback<GeometryChangedEvent>(_ => FitLineHeight(label)); parent.Add(label); return label;

        }

        private static void FitLineHeight(Label label)

        {

            if (label.ClassListContains("eov-skill-selector-level") || label.contentRect.width <= 0) return;

            label.style.height=StyleKeyword.Auto;

        }

        private ScrollView Scroll(VisualElement parent, string name)

        {

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = name, horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };

            scroll.AddToClassList("eov-buffs-scroll"); scroll.contentContainer.style.paddingTop = 0; theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); parent.Add(scroll); return scroll;

        }

        private void Select(int index) { selected = index; highlights[index].style.display = DisplayStyle.None; Refresh(); }

        private void Experience(Skill skill, float current, float required) { if (skill == Current) RefreshExperience(); }

        private void LevelUp(Skill skill, int value)

        {

            var index = System.Array.IndexOf(definition.skills, skill); if (index >= 0 && index != selected) highlights[index].style.display = DisplayStyle.Flex; dirty = true;

        }

        private void Changed() => dirty = true;

        private void ActiveChanged(int used, int total) => dirty = true;

        private void RefreshExperience()

        {

            var skill = Current; if (!skill) return; var progress = controller.GetProgress(skill); var value = progress?.Level ?? 1; var current = progress?.CurrentXP ?? 0;

            var required = skill.xpForFirstLevel * Mathf.Pow(value, skill.xpLevelMultiplier);

            level.text = ToolkitLocalization.Text("skills.level-heading", "<b><smallcaps>{0} | Lvl {1}</smallcaps></b>", ToolkitLocalization.Text("skill." + skill.name, skill.skillName), value); xp.text = $"<b><smallcaps>{current:N0} / {required:N0}</smallcaps></b>"; xpFill.style.width = Length.Percent(required > 0 ? Mathf.Clamp01(current / required) * 100 : 0);

        }

        private void Refresh()

        {

            if (!IsOpen || !controller) return;

            for (var i = 0; i < definition.skills.Length; i++)

            {

                var selectorLevel = controller.GetProgress(definition.skills[i])?.Level ?? 1;

                selectorLevels[i].text = ShowLevelText ? ToolkitLocalization.Text("skills.selector-level", "<b>Lvl: {0}</b>", selectorLevel) : "";

                selectorLevels[i].style.fontSize = 6;

                selections[i].style.display = i == selected ? DisplayStyle.Flex : DisplayStyle.None;

            }

            RefreshExperience(); BuildMilestones();

            slots.text = controller.TotalActiveSlots > 0 ? ToolkitLocalization.Text("skills.active-slots", "<b><smallcaps>Active Slots: {0}({1})</smallcaps></b>", controller.ActiveSlotsUsed, controller.TotalActiveSlots) : "";

            active.Clear();
            foreach (var item in controller.EnumerateActiveMilestones().Where(i => i.Definition != null))
                AddSummaryLine(active, item.Skill != null && item.Skill != Current ? $"{item.Definition.DisplayName} ({item.Skill.skillName})" : item.Definition.DisplayName, MilestoneIcon(item.Definition, item.Skill));

            summaryTitle.text = ToolkitLocalization.Text("skills.bonuses-heading", "<b>{0} bonuses</b>", ToolkitLocalization.Text("skill." + Current.name, Current.skillName));
            var bonusRows = new List<SkillTotalsPresentation.BonusLine>();
            SkillTotalsPresentation.BuildForSkill(controller, Current, bonusRows);
            BuildSummary(totals, bonusRows);
            SkillTotalsPresentation.Build(controller, definition.skills, rows: bonusRows);
            BuildSummary(allTotals, bonusRows);

            var sets = controller.EnumerateActiveSets().Where(s => s.Definition != null).OrderByDescending(s => s.ActiveCount).ToList();

            setFrame.style.display = sets.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            primaryName.text = primaryEffect.text = secondaryName.text = secondaryEffect.text = "";

            if (sets.Count > 0)

            {

                var first = sets[0]; primaryName.text = first.Definition.DisplayName;

                primaryEffect.text = string.Join("\n", new[] { first.ThreePieceActive ? first.Definition.ThreePieceDescription : "", first.SixPieceActive ? first.Definition.SixPieceDescription : "" }.Where(s => !string.IsNullOrEmpty(s)));

                var second = sets.Skip(1).FirstOrDefault(s => s.Definition != first.Definition && s.ThreePieceActive);

                if (second.Definition != null) { secondaryName.text = second.Definition.DisplayName; secondaryEffect.text = string.Join("\n", new[] { second.ThreePieceActive ? second.Definition.ThreePieceDescription : "", second.SixPieceActive ? second.Definition.SixPieceDescription : "" }.Where(s => !string.IsNullOrEmpty(s))); }

            }

            root.schedule.Execute(() => { if (root != null) foreach (var label in root.Query<Label>().ToList()) FitLineHeight(label); });

        }

        private void BuildMilestones()

        {

            var offset = milestoneList.scrollOffset; milestoneList.Clear(); var skill = Current; if (!skill) return;

            var milestones = skill.milestones.Where(m => m != null).Select(m => (level: m.UnlockLevel, milestone: m, unlock: (ResourceUnlockEntry)null));

            var unlocks = (skill.resourceUnlocks ?? new List<ResourceUnlockEntry>()).Where(u => u != null && u.task != null).Select(u => (level: u.requiredLevel, milestone: (MilestoneDefinition)null, unlock: u));

            foreach (var item in milestones.Concat(unlocks).OrderBy(i => i.level))

            {

                var row = Frame(milestoneList, definition.frame); row.AddToClassList("eov-milestone-row");
                var divider = ToolkitGameplay.E(row, "skill-entry-divider"); divider.pickingMode = PickingMode.Ignore;

                var body = new VisualElement(); body.AddToClassList("eov-milestone-body");

                var currentLevel = controller.GetProgress(skill)?.Level ?? 1;

                if (item.milestone != null)

                {

                    var milestone = item.milestone; row.name = "milestone-" + milestone.name;

                    var state = controller.GetMilestoneState(skill, milestone); var tier = state?.TierIndex ?? -1; var unlocked = tier >= 0;

                    row.EnableInClassList("locked",!unlocked); row.EnableInClassList("unlocked",unlocked);

                    row.EnableInClassList("milestone-enabled", unlocked && milestone.CanActivate && state?.IsActive == true);
                    var next = milestone.GetNextTierLevel(currentLevel); var title = milestone.DisplayName;

                    if (unlocked) title += " " + Mathf.Max(1, tier + 1);
                    AddEntryIcon(row, MilestoneIcon(milestone, skill));
                    row.Add(body);
                    var heading = new VisualElement(); heading.AddToClassList("skill-entry-heading"); body.Add(heading);
                    var name = Text(heading, title, 7); name.AddToClassList("skill-entry-name");
                    var nextText = unlocked ? (next > 0 ? ToolkitLocalization.Text("skills.improves-level", "Improves at level {0}", next) : "") : ToolkitLocalization.Text("skills.unlocks-level", "Unlocks at level {0}", milestone.UnlockLevel);
                    Text(heading, nextText, 7).AddToClassList("skill-entry-next");
                    var displayTier = unlocked ? tier : 0; if (milestone.TierCount > 0) displayTier = Mathf.Clamp(displayTier, 0, milestone.TierCount - 1);

                    var passive = Compact(milestone.GetPassiveDescriptionForTier(displayTier, skill.skillName)); var hasSlots = controller.TotalActiveSlots > 0;

                    Text(body, string.IsNullOrEmpty(passive) ? "" : hasSlots && milestone.HasActiveEffect ? ToolkitLocalization.Text("skills.passive", "Passive: {0}", passive) : passive, 5);

                    if (unlocked && milestone.HasActiveEffect && hasSlots) { var description = Compact(milestone.GetActiveDescriptionForTier(displayTier, skill.skillName)); Text(body, string.IsNullOrEmpty(description) ? "" : ToolkitLocalization.Text("skills.active", "Active: {0}", description), 5); }

                    var right = new VisualElement(); right.AddToClassList("eov-milestone-right"); row.Add(right);

                    var controls = new VisualElement(); controls.AddToClassList("eov-milestone-controls"); right.Add(controls);

                    var set = milestone.Set != MilestoneSet.None ? controller.GetSetDefinition(milestone.Set) : null;

                    if (hasSlots && unlocked && milestone.CanActivate)

                    {

                        var toggle = ToolkitBuffsScreen.MakeButton("toggle-" + milestone.name, () => { var current = controller.GetMilestoneState(skill, milestone); controller.TrySetMilestoneActive(skill, milestone, !(current?.IsActive ?? false)); dirty = true; }, state?.IsActive == true ? definition.toggleOn : definition.toggleOff);

                        ToolkitGameplay.SetToggle(toggle,state?.IsActive==true); controls.Add(toggle);

                    }

                    var setLabel = Text(right, milestone.Set != MilestoneSet.None ? ToolkitLocalization.Text("skills.set", "Set: {0}", set != null ? set.DisplayName : milestone.Set.ToString()) : "", 5); setLabel.style.unityTextAlign = TextAnchor.UpperRight; setLabel.style.minHeight = 5.82f;

                }

                else

                {

                    var unlock = item.unlock; var unlocked = currentLevel >= unlock.requiredLevel; row.name = "unlock-" + unlock.task.name; row.EnableInClassList("locked",!unlocked); row.EnableInClassList("unlocked",unlocked);

                    if (unlocked)

                    {

                        AddEntryIcon(row, unlock.useOverrideIcon ? unlock.overrideIcon : unlock.task.taskIcon, fitFrame: unlock.useOverrideIcon);
                    }

                    row.Add(body); Text(body, unlocked ? ToolkitLocalization.Text("skills.task-unlocked", "{0} | <size=80%>Unlocked at level {1}</size>", ToolkitLocalization.Text("task." + unlock.task.name, unlock.task.taskName), unlock.requiredLevel) : ToolkitLocalization.Text("skills.task-locked", "??? | <size=80%>Unlocks at level {0}</size>", unlock.requiredLevel), 6);

                    Text(body, unlocked && !string.IsNullOrEmpty(unlock.description) ? unlock.description : "", 5);

                }

            }

            milestoneList.scrollOffset = offset;

        }

        private Sprite MilestoneIcon(MilestoneDefinition milestone, Skill skill)
        {
            var milestoneIcon = milestone.SetIcon;
            if (!milestoneIcon && milestone.Set != MilestoneSet.None) milestoneIcon = controller.GetSetDefinition(milestone.Set)?.Icon;
            if (!milestoneIcon && milestone.PassiveEffect is MilestoneStatEffectDefinition statEffect && statEffect.Stat)
                StatIconLookup.TryGetIcon(statEffect.Stat.name, out milestoneIcon);
            if (!milestoneIcon && milestone.PassiveEffect is MilestoneExperienceBonusEffectDefinition xpEffect && xpEffect.TargetSkill)
            {
                var targetIndex = System.Array.IndexOf(definition.skills, xpEffect.TargetSkill);
                if (targetIndex >= 0) milestoneIcon = definition.icons[targetIndex];
            }
            if (!milestoneIcon && milestone.PassiveEffect is MilestoneSpawnEchoEffectDefinition)
            {
                var recipe = UnityEngine.Resources.Load<TimelessEchoes.Buffs.BuffRecipe>(skill == controller.CombatSkill ? "Buffs/Echo Combat" : "Buffs/Echo Tasks");
                if (recipe) milestoneIcon = recipe.buffIcon;
            }
            var index = System.Array.IndexOf(definition.skills, skill);
            return milestoneIcon ? milestoneIcon : index >= 0 ? definition.icons[index] : definition.icons[selected];
        }
        private static Sprite BuffIcon(string name)
        {
            var recipe = UnityEngine.Resources.Load<TimelessEchoes.Buffs.BuffRecipe>("Buffs/" + name);
            return recipe ? recipe.buffIcon : null;
        }
        private void BuildSummary(VisualElement parent, List<SkillTotalsPresentation.BonusLine> rows)
        {
            parent.Clear();
            foreach (var line in rows)
            {
                Sprite icon = null;
                if (line.Stat) StatIconLookup.TryGetIcon(line.Stat.name, out icon);
                if (!icon) icon = line.Kind switch
                {
                    "resources" => BuffIcon("Resource Doubler"),
                    "xp" => BuffIcon("Bonus Experience"),
                    "speed" => BuffIcon("Task Speed"),
                    "damage" => BuffIcon("Combat Enhancer"),
                    "echo" => BuffIcon(line.Skill == controller.CombatSkill ? "Echo Combat" : "Echo Tasks"),
                    _ => null
                };
                // Keep the original numbers and wording. Replace inline stat sprites only
                // when a native image is available, avoiding duplicate icons.
                var text = icon ? Regex.Replace(line.Text, @"<sprite[^>]*>\s*", "") : line.Text;
                AddSummaryLine(parent, text, icon);
            }
        }
        private static void AddSummaryLine(VisualElement parent, string text, Sprite sprite)
        {
            var row = ToolkitGameplay.E(parent, "skill-summary-line");
            var crop = ToolkitGameplay.E(row, "skill-summary-icon");
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.flexShrink = 0;
            if (sprite) { image.style.width = sprite.rect.width * 16 / sprite.pixelsPerUnit; image.style.height = sprite.rect.height * 16 / sprite.pixelsPerUnit; }
            crop.Add(image);
            var label = Text(row, text, 7); label.style.flexGrow = 1; label.style.flexShrink = 1; label.style.minWidth = 0;
        }
        private static string Compact(string description)
        {
            if (string.IsNullOrEmpty(description)) return description;
            var match = Regex.Match(description, @"^Increases (.+) by ([0-9.,]+%?)\.$");
            if (!match.Success) return description;
            var effect = match.Groups[1].Value.Replace("experience gained", "XP").Replace("resource drops", "resources");
            return $"+{match.Groups[2].Value} {effect}";
        }
        private static void AddEntryIcon(VisualElement row, Sprite sprite, bool fitFrame = false)
        {
            var frame = ToolkitGameplay.E(row, "skill-entry-icon-frame");
            var crop = ToolkitGameplay.E(frame, "skill-entry-icon-crop");
            crop.pickingMode = PickingMode.Ignore;
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.flexShrink = 0;
            // Match the original masked task view: a common reference PPU, never
            // independently resize ordinary task art. Authored overrides (tree thumbnails)
            // fit the frame so the whole tree remains recognizable.
            if (sprite)
            {
                image.style.width = fitFrame ? 16 : sprite.rect.width * 16 / sprite.pixelsPerUnit;
                image.style.height = fitFrame ? 16 : sprite.rect.height * 16 / sprite.pixelsPerUnit;
            }
            crop.Add(image);
        }
        private void Layout()

        {

            var area = ToolkitBookScreen.CalculateSafeArea(new Vector2Int(Screen.width, Screen.height), Screen.safeArea, Application.isMobilePlatform ? 1 : SafeAreaRatio);

            root.style.width = Mathf.Min(640, area.width-24); root.style.left = area.center.x; root.style.top = area.y + 44; root.style.height = Mathf.Max(0, area.height - 56);

        }

        private void Update() { if (!IsOpen) return; Layout(); if (dirty) { dirty = false; Refresh(); } }

        public void Hide()

        {

            if (controller) { controller.OnExperienceGained -= Experience; controller.OnLevelUp -= LevelUp; controller.OnMilestoneDataChanged -= Changed; controller.OnActiveSlotsChanged -= ActiveChanged; }

            ToolkitLocalization.Changed -= Changed;
            ShowLevelTextChanged -= Changed; Blindsided.EventHandler.OnLoadData -= Changed;

            titleBinding?.Dispose(); titleBinding = null; root?.RemoveFromHierarchy(); root = null; selectorLevels.Clear(); selections.Clear(); highlights.Clear();

        }

        private void OnDisable() => Hide();

        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }

    }

}



