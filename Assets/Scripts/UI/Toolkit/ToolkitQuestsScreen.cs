using Screen = UnityEngine.Device.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Quests;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;
using static Blindsided.SaveData.StaticReferences;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitQuestsScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitQuestsDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private QuestManager manager;
        private VisualElement root;
        private ToolkitResourceInventoryScreen companionInventory;
        private ScrollView scroll;
        private VisualElement inventoryHost;
        private Button inventoryToggle;
        private VisualElement inventoryTriangle;
        private bool inventoryExpanded;
        private Button portraitAutoPin;
        private ToolkitTextBinding instructionBinding;
        private readonly Dictionary<string, Action<float>> progress = new();
        private readonly HashSet<string> dirtyProgress = new();
        private bool dirty;
        private string pendingRevealQuest;
        private Vector3 previousRevealLayout = Vector3.negativeInfinity;
        private Image acceptHand;
        private Button lessonAccept;
        private readonly Dictionary<QuestNoticeboardCategory,bool> expandedCategories=new();
        public bool IsOpen => root != null;
        public bool IsConfigured => definition && theme && runtimeTheme && textSettings;

        public bool Show() => Show(null);
        public bool Show(ToolkitResourceInventoryScreen inventoryTemplate)
        {
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipMainUi) return false;
            if (IsOpen) return true;
            if (!IsConfigured || !(manager = QuestManager.Instance)) return false;
            if (!settings) { settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 100; }
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { name = "quests" }; root.AddToClassList("eov-quests");
            theme.Apply(root);ToolkitGameplay.Apply(root,theme);root.AddToClassList("menu-surface");root.AddToClassList("quests-reviewed");  document.rootVisualElement.Add(root);
            var columns = new VisualElement { name = "quests-columns" };
            columns.AddToClassList("quests-columns"); root.Add(columns);
            var quests = new VisualElement { name = "quests-content" };
            quests.AddToClassList("quests-content"); columns.Add(quests);
            var heading = new VisualElement(); heading.style.flexDirection = FlexDirection.Row; quests.Add(heading);
            var title = Text(heading,ToolkitLocalization.Text("quests.heading", "Quests"),12); title.AddToClassList("quests-heading"); title.style.flexGrow = 1;
            inventoryToggle = new Button(() => SetInventoryExpanded(!inventoryExpanded)) { name = "quests-toggle-inventory" };
            inventoryToggle.AddToClassList("button");
            inventoryToggle.style.width = inventoryToggle.style.minWidth = 22; inventoryToggle.style.height = 22;
            inventoryToggle.style.alignItems = Align.Center; inventoryToggle.style.justifyContent = Justify.Center;
            inventoryTriangle = new VisualElement { pickingMode = PickingMode.Ignore };
            inventoryTriangle.style.width = inventoryTriangle.style.height = 9;
            inventoryTriangle.generateVisualContent += context =>
            {
                var rect = inventoryTriangle.contentRect; var painter = context.painter2D;
                painter.fillColor = inventoryTriangle.resolvedStyle.color; painter.BeginPath();
                var tip = inventoryExpanded ? rect.xMax : rect.xMin;
                var back = inventoryExpanded ? rect.xMin : rect.xMax;
                painter.MoveTo(new Vector2(tip, rect.center.y)); painter.LineTo(new Vector2(back, rect.yMin));
                painter.LineTo(new Vector2(back, rect.yMax)); painter.ClosePath(); painter.Fill();
            };
            inventoryToggle.Add(inventoryTriangle); heading.Add(inventoryToggle);
            var inventoryLabel = new Label("Inventory"); inventoryLabel.AddToClassList("quest-inventory-label"); inventoryLabel.pickingMode = PickingMode.Ignore; inventoryToggle.Add(inventoryLabel);
            portraitAutoPin = ToolkitGameplay.B(heading, "", () => {
                Blindsided.SaveData.StaticReferences.AutoPinActiveQuests = !Blindsided.SaveData.StaticReferences.AutoPinActiveQuests;
                RefreshPortraitAutoPin();
            });
            portraitAutoPin.name = "quests-auto-pin"; RefreshPortraitAutoPin();
            scroll = new ScrollView(ScrollViewMode.Vertical) { name = "quest-scroll", horizontalScrollerVisibility = ScrollerVisibility.Hidden, verticalScrollerVisibility = ScrollerVisibility.Auto };
            scroll.AddToClassList("eov-buffs-scroll"); theme.StyleScroll(scroll);ToolkitGameplay.StyleScroll(scroll); quests.Add(scroll);
            if (inventoryTemplate && inventoryTemplate.IsConfigured)
            {
                inventoryHost = new VisualElement { name = "quests-inventory" };
                inventoryHost.AddToClassList("quests-inventory"); columns.Add(inventoryHost);
                companionInventory = inventoryTemplate.CreateCompanionView();
                if (!ToolkitResponsiveLayout.IsPortrait && !companionInventory.ShowInQuests(inventoryHost))
                {
                    Destroy(companionInventory.gameObject); companionInventory = null;
                    inventoryHost.RemoveFromHierarchy();
                }
            }
            inventoryToggle.style.display = companionInventory ? DisplayStyle.Flex : DisplayStyle.None;
            SetInventoryExpanded(inventoryExpanded);
            manager.RefreshNoticeboard();
            manager.NoticeboardChanged += Changed; manager.QuestProgressChanged += ProgressChanged;
            LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
            ToolkitLocalization.Changed += LocalizationChanged;
            Rebuild(); Layout(); return true;
        }
        private void SetInventoryExpanded(bool expanded)
        {
            inventoryExpanded = expanded;
            if (ToolkitResponsiveLayout.IsPortrait && companionInventory)
            {
                if (inventoryHost != null) inventoryHost.style.display = DisplayStyle.None;
                if (expanded) companionInventory.ShowOverQuests();
                else companionInventory.Hide();
            }
            inventoryToggle?.EnableInClassList("active", expanded);
            if (inventoryHost != null && !ToolkitResponsiveLayout.IsPortrait) inventoryHost.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            if (inventoryToggle != null) inventoryToggle.tooltip = expanded ? ToolkitLocalization.Text("quests.hide-inventory", "Hide inventory") : ToolkitLocalization.Text("quests.show-inventory", "Show inventory");
            inventoryTriangle?.MarkDirtyRepaint();
        }
        private void LocaleChanged(UnityEngine.Localization.Locale _) { manager.RefreshNoticeboard(); dirty = true; }
        public bool TryHighlightResource(Upgrades.Resource resource, bool scrollToSlot)
        {
            if (!IsOpen || !companionInventory) return false;
            SetInventoryExpanded(true);
            companionInventory.HighlightResource(resource, scrollToSlot);
            return true;
        }
        public bool RevealQuest(string questId)
        {
            if (!IsOpen || string.IsNullOrEmpty(questId)) return false;
            var entry = manager.GetNoticeboardEntries().FirstOrDefault(e => e.Quest.questId == questId);
            if (!entry.Quest) return false;
            expandedCategories[entry.Category] = true;
            Rebuild(); dirty = false;
            pendingRevealQuest = questId;
            previousRevealLayout = Vector3.negativeInfinity;
            return true;
        }

        private void ApplyPendingQuestReveal()
        {
            if (string.IsNullOrEmpty(pendingRevealQuest)) return;
            var row = scroll.Q("quest-" + pendingRevealQuest);
            if (row == null || row.layout.height <= 0 || scroll.contentViewport.layout.height <= 0) return;
            var target = row.worldBound.yMin - scroll.contentViewport.worldBound.yMin + scroll.scrollOffset.y;
            var layout = new Vector3(target, scroll.contentViewport.layout.height, scroll.verticalScroller.highValue);
            // Opening/rebuilding a window lays out content and updates the scroller in separate passes.
            // Wait for settled geometry, then align the quest's top rather than merely making it visible.
            if (layout != previousRevealLayout) { previousRevealLayout = layout; return; }
            scroll.scrollOffset = new Vector2(0, Mathf.Clamp(target, 0, scroll.verticalScroller.highValue));
            pendingRevealQuest = null;
        }

        private void LocalizationChanged() { if (manager) manager.RefreshNoticeboard(); dirty = true; }
        private static string CategoryName(QuestNoticeboardCategory category) => category switch
        {
            QuestNoticeboardCategory.Ready => ToolkitLocalization.Text("quests.category-ready", "Complete"),
            QuestNoticeboardCategory.Completed => ToolkitLocalization.Text("quests.category-completed", "Quest History"),
            QuestNoticeboardCategory.Pinned => ToolkitLocalization.Text("quests.category-tracked", "Tracked"),
            _ => ToolkitLocalization.Text("quests.category-untracked", "Untracked")
        };
        private void Changed() => dirty = true;
        private void ProgressChanged(QuestData quest, float value) { if (quest) dirtyProgress.Add(quest.questId); }
        private void Rebuild()
        {
            var offset = scroll.scrollOffset; instructionBinding?.Dispose(); scroll.Clear(); progress.Clear(); dirtyProgress.Clear();
            var instructions = Text(scroll, "", 4.4f); instructions.AddToClassList("eov-quest-instructions");
            instructionBinding = new ToolkitTextBinding(instructions, definition.instructions);
            lessonAccept = null;
            var entries = manager.GetNoticeboardEntries().ToList();
            var lesson = entries.FirstOrDefault(e => e.Quest.questId == "Ivan's First Lesson" && manager.NeedsAcceptance(e.Quest));
            if (lesson.Quest && (GameManager.Instance == null || GameManager.Instance.IsInTown))
            {
                AddQuest(scroll, lesson); entries.Remove(lesson);
                lessonAccept = scroll.Q<Button>("turn-in-Ivan's First Lesson");
                scroll.schedule.Execute(() => { if (lessonAccept != null) scroll.ScrollTo(lessonAccept); });
            }
            foreach (var category in (QuestNoticeboardCategory[])Enum.GetValues(typeof(QuestNoticeboardCategory)))
            {
                var items = entries.Where(e => e.Category == category).ToArray(); if (items.Length == 0) continue;
                var group = new VisualElement(); group.AddToClassList("eov-quest-category"); scroll.Add(group);
                var header = new Button { name = "quest-category-" + category }; header.AddToClassList("eov-chapter-header"); header.AddToClassList("button");
                var title = new Label(ToolkitLocalization.Text("quests.category-count", "{0} · {1}", CategoryName(category), items.Length));
                title.text = "<b>" + title.text + "</b>"; title.AddToClassList("eov-chapter-title"); title.pickingMode = PickingMode.Ignore; header.Add(title);
                var icon = new Label { pickingMode = PickingMode.Ignore }; icon.AddToClassList("book-disclosure"); header.Add(icon);
                var content = new VisualElement(); content.AddToClassList("eov-quest-entries");
                var expanded = expandedCategories.TryGetValue(category,out var saved)?saved:category != QuestNoticeboardCategory.Completed;
                void Expand(bool value) { expanded = value; expandedCategories[category]=value; content.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; icon.text = value ? "−" : "+";header.EnableInClassList("expanded",value); }
                header.clicked += () => { Expand(!expanded); Audio.AudioManager.Instance?.PlayUIButtonClick(); };
                group.Add(header); group.Add(content); Expand(expanded);
                foreach (var entry in items) AddQuest(content, entry);
                content.ElementAt(content.childCount - 1).style.marginBottom = 0;
            }
            scroll.scrollOffset = offset;
        }
        private void AddQuest(VisualElement parent, QuestNoticeboardEntry entry)
        {
            var quest = entry.Quest;
            var row = new VisualElement { name = "quest-" + quest.questId }; row.AddToClassList("eov-quest-row"); 
            row.EnableInClassList("quest-completed",entry.Completed); row.EnableInClassList("quest-ready",!entry.Completed&&entry.Progress>=1); parent.Add(row);
            var top = new VisualElement(); top.AddToClassList("eov-quest-top"); row.Add(top);
            var texts = new VisualElement(); texts.AddToClassList("eov-quest-texts"); top.Add(texts);
            var pinned = Blindsided.Oracle.oracle?.saveData?.PinnedQuests?.Contains(quest.questId) == true;
            Text(texts, "<b>" + quest.questName.GetLocalizedString() + "</b>", 9).AddToClassList("quest-name");
            var description = Text(texts, quest.description.GetLocalizedString(), 6); description.AddToClassList("quest-description");
            description.style.fontSize = StyleKeyword.Null;
            Button turnIn = null;
            Label turnInLabel = null;
            var first = quest.requirements?.FirstOrDefault()?.type;
            if (!entry.Completed)
            {
                var controls = new VisualElement(); controls.AddToClassList("eov-quest-controls"); top.Add(controls);
                turnIn = ToolkitBuffsScreen.MakeButton("turn-in-" + quest.questId, () => manager.TryTurnInQuest(quest.questId), definition.button);
                turnIn.AddToClassList("eov-quest-turn-in");
                turnInLabel = Text(turnIn, "", 8);
                controls.Add(turnIn); turnIn.SetEnabled((manager.NeedsAcceptance(quest) || entry.Progress >= 1) && manager.CanTurnInAtCurrentLocation(quest));
                if (!manager.CanTurnInAtCurrentLocation(quest))
                    turnIn.tooltip = ToolkitLocalization.Text("quests.return-to-town", "Return to town to hand in this quest.");
            }
            var requirements = new List<(QuestData.Requirement req, Label text, ToolkitQuestRequirementRow resourceRow, VisualElement fill)>();
            var requirementGroup = new VisualElement(); requirementGroup.style.marginTop = 2; row.Add(requirementGroup);
            if (!entry.Completed && quest.requirements != null)
                foreach (var req in quest.requirements.Where(r => r != null && r.type != QuestData.RequirementType.Instant))
                {
                    var requirement = new VisualElement();requirement.AddToClassList("quest-requirement"); requirementGroup.Add(requirement);
                    ToolkitQuestRequirementRow resourceRow = null;
                    Label label;
                    if (req.type == QuestData.RequirementType.Resource && req.resource)
                    {
                        resourceRow = new ToolkitQuestRequirementRow(req.resource, 8);
                        requirement.Add(resourceRow); label = resourceRow.Text;
                        label.AddToClassList("eov-quest-text");
                    }
                    else label = Text(requirement, "", 8);
                    label.userData = 6.4f;
                    var track = new VisualElement(); track.style.height = 3; track.AddToClassList("track"); requirement.Add(track);
                    var fill = new VisualElement(); fill.style.height = 3; fill.style.overflow = Overflow.Hidden; track.Add(fill);
                    var sprite = new VisualElement { pickingMode = PickingMode.Ignore }; sprite.style.position = Position.Absolute; sprite.style.height = 3; sprite.AddToClassList("fill"); fill.Add(sprite);
                    track.RegisterCallback<GeometryChangedEvent>(_ => sprite.style.width = track.contentRect.width);
                    requirements.Add((req, label, resourceRow, fill));
                }
            var reward = Text(row, ToolkitLocalization.Text("quests.reward", "Reward: {0}", quest.rewardDescription.GetLocalizedString()), 7); reward.AddToClassList("quest-reward");
            reward.style.fontSize = StyleKeyword.Null;
            foreach (var equipment in quest.rewards.Where(r => r.type == QuestData.RewardType.Equipment && r.equipment))
            {
                var stats = new VisualElement { name = "equipment-reward-stats" };
                stats.style.flexDirection = FlexDirection.Row; stats.style.flexWrap = Wrap.Wrap;
                row.Add(stats);
                foreach (var affix in equipment.equipment.affixes)
                {
                    if (affix?.stat == null) continue;
                    var stat = new VisualElement { tooltip = affix.stat.GetName() };
                    stat.style.flexDirection = FlexDirection.Row; stat.style.alignItems = Align.Center; stat.style.marginRight = 6;
                    if (TimelessEchoes.Upgrades.StatIconLookup.TryGetIcon(affix.stat.heroMapping, out var sprite))
                    {
                        var icon = ToolkitControls.Icon(sprite); icon.style.width = 8; icon.style.height = 8;
                        icon.style.marginRight = 2; stat.Add(icon);
                    }
                    Text(stat, "+" + affix.value.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture)
                        + (affix.stat.isPercent ? "%" : ""), 7);
                    stats.Add(stat);
                }
            }
            var canPin = !entry.Completed && quest.requirements?.Any(r => r != null && r.type == QuestData.RequirementType.Instant) != true;
            if (canPin)
            {
                void Pin() { manager.TogglePinned(quest.questId); Audio.AudioManager.Instance?.PlayUIButtonClick(); }
                var pin=new Button(Pin){name="pin-"+quest.questId,text=pinned?ToolkitLocalization.Text("quests.pinned", "Pinned"):ToolkitLocalization.Text("quests.pin", "Pin")};pin.AddToClassList("button");pin.AddToClassList("quest-pin");pin.EnableInClassList("active",pinned);top.Q(className:"eov-quest-controls").Add(pin);
                texts.pickingMode = PickingMode.Position; reward.pickingMode = PickingMode.Position;
                texts.RegisterCallback<ClickEvent>(_ => Pin()); reward.RegisterCallback<ClickEvent>(_ => Pin());
                texts.focusable = true; texts.tabIndex = 0; texts.RegisterCallback<NavigationSubmitEvent>(evt => { Pin(); evt.StopPropagation(); });
            }
            void UpdateProgress(float value)
            {
                if (turnInLabel != null)
                    turnInLabel.text = "<b>" + (manager.NeedsAcceptance(quest) ? ToolkitLocalization.Text("quests.accept", "Accept") :
                        value < 1 ? ToolkitLocalization.Text("quests.incomplete", "Incomplete") :
                        first == QuestData.RequirementType.Instant ? ToolkitLocalization.Text("quests.okay", "Okay") :
                        first == QuestData.RequirementType.Meet ? ToolkitLocalization.Text("quests.done", "Done") :
                        ToolkitLocalization.Text("quests.turn-in", "Turn In")) + "</b>";
                turnIn?.SetEnabled((manager.NeedsAcceptance(quest) || value >= 1) && manager.CanTurnInAtCurrentLocation(quest));row.EnableInClassList("quest-ready",!entry.Completed&&value>=1);
                foreach (var item in requirements)
                {
                    var presentation = QuestRequirementPresentation.Build(quest, item.req);
                    if (item.resourceRow != null) item.resourceRow.SetText(presentation.text);
                    else item.text.text = presentation.text;
                    item.fill.style.width = Length.Percent(presentation.progress * 100);
                }
            }
            progress[quest.questId] = UpdateProgress; UpdateProgress(entry.Progress);
        }
        private static Label Text(VisualElement parent, string value, float size)
        {
            var label = new Label(value) { pickingMode = PickingMode.Ignore, enableRichText = true }; label.AddToClassList("eov-quest-text"); label.style.fontSize = Mathf.Max(7,size);
 parent.Add(label); return label;
        }
        private readonly ToolkitWindowLayout windowLayout = new();
        private void RefreshPortraitAutoPin()
        {
            if (portraitAutoPin == null) return;
            portraitAutoPin.style.display = ToolkitResponsiveLayout.IsPortrait ? DisplayStyle.Flex : DisplayStyle.None;
            portraitAutoPin.text = ToolkitLocalization.Text("quests.auto-pin", "Auto pin") + ": " +
                ToolkitLocalization.Text(Blindsided.SaveData.StaticReferences.AutoPinActiveQuests ? "common.on" : "common.off", Blindsided.SaveData.StaticReferences.AutoPinActiveQuests ? "On" : "Off");
        }
        private void Layout()
        {
            windowLayout.Fill(root, theme); RefreshPortraitAutoPin();
            var phone = ToolkitResponsiveLayout.IsPortrait;
            inventoryToggle.style.width = inventoryToggle.style.minWidth = phone ? 76 : 22;
            inventoryToggle.Q<Label>().style.display = phone ? DisplayStyle.Flex : DisplayStyle.None;
            inventoryTriangle.style.display = phone ? DisplayStyle.None : DisplayStyle.Flex;
        }
        private void Update()
        {
            if (!IsOpen) return; Layout();
            if (acceptHand == null && definition.tutorialHand)
            {
                acceptHand = new Image { name = "quest-accept-hand", sprite = definition.tutorialHand, pickingMode = PickingMode.Ignore };
                acceptHand.style.position = Position.Absolute; acceptHand.style.width = acceptHand.style.height = 24; root.Add(acceptHand);
            }
            if (acceptHand != null)
            {
                acceptHand.style.display = lessonAccept != null && lessonAccept.panel != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (lessonAccept != null && lessonAccept.panel != null)
                {
                    var p = root.WorldToLocal(new Vector2(lessonAccept.worldBound.center.x, lessonAccept.worldBound.yMax));
                    acceptHand.style.left = Mathf.Round(p.x - 12); acceptHand.style.top = Mathf.Round(p.y + 2 + 4 * (1 + Mathf.Cos(Time.unscaledTime * 5)));
                }
            }
            if (dirty) { dirty = false; root.Q<Label>(className: "quests-heading").text = ToolkitLocalization.Text("quests.heading", "Quests"); Rebuild(); }
            else if (dirtyProgress.Count > 0)
            {
                foreach (var entry in manager.GetNoticeboardEntries()) if (dirtyProgress.Contains(entry.Quest.questId) && progress.TryGetValue(entry.Quest.questId, out var update)) update(entry.Progress);
                dirtyProgress.Clear();
            }
            ApplyPendingQuestReveal();
        }
        public void Hide()
        {
            pendingRevealQuest = null;
            if (ToolkitResponsiveLayout.IsPortrait) inventoryExpanded = false;
            if (companionInventory)
            {
                companionInventory.Hide(); Destroy(companionInventory.gameObject); companionInventory = null;
            }
            if (manager) { manager.NoticeboardChanged -= Changed; manager.QuestProgressChanged -= ProgressChanged; }
            LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            ToolkitLocalization.Changed -= LocalizationChanged;
            acceptHand = null; lessonAccept = null;
            instructionBinding?.Dispose(); instructionBinding = null; root?.RemoveFromHierarchy(); root = null; inventoryHost = null; inventoryToggle = null; progress.Clear(); dirtyProgress.Clear(); dirty = false;
            ToolkitPanel.Release(GetComponent<UIDocument>(), ref settings);
        }
        private void OnDisable() => Hide();
        private void OnDestroy() { Hide(); if (settings) Destroy(settings); }
    }
}
