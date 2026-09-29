using System;
using static TimelessEchoes.UI.Toolkit.ToolkitGameplay;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Buffs;
using TimelessEchoes.Gear;
using TimelessEchoes.Hero;
using TimelessEchoes.Stats;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ToolkitRunScreen : MonoBehaviour
    {
        [SerializeField] private ToolkitRunDefinition definition;
        [SerializeField] private ToolkitTheme theme;
        [SerializeField] private ThemeStyleSheet runtimeTheme;
        [SerializeField] private PanelTextSettings textSettings;
        private PanelSettings settings;
        private VisualElement root, hud, bar, returnControls, death, deathFill, overlay, detail, tooltip;
        private Label runTime,runRatesLeft,runRatesRight;
        private Label healthValue, defenseValue, regenValue, movementValue, deathTitle, detailTitle, detailSummary, tooltipText;
        private Button retreat, returnOnDeath, breakdown, joke;
        private DadJokeManager jokes;
        private VisualElement jokeBubble, jokeCountdown;
        private const float JokeDisplaySeconds = 3f;
        private float jokeClosesAt;
        private Label jokeText;
        private ScrollView detailRows;
        private readonly List<(Button button, Image icon, Image auto, Label text, VisualElement fill)> slots = new();
        private readonly Dictionary<Resource, double> runResources = new();
        private readonly Dictionary<Resource, VisualElement> resourceRows = new();
        private ResourceManager resources;
        private GameplayStatTracker tracker;
        private bool showingBreakdown;
        private float nextRefresh, elapsedAtEnd, runStartedAt;
        private int tooltipSlot = -1;
        public bool IsConfigured => definition && definition.buffs && theme && runtimeTheme && textSettings;

        private void OnEnable()
        {
            if (!IsConfigured) return;
            settings = ToolkitPanel.CreateSettings(runtimeTheme, textSettings); settings.sortingOrder = 80;
            var document = GetComponent<UIDocument>(); document.panelSettings = settings;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            root = new VisualElement { pickingMode = PickingMode.Ignore }; root.style.position = Position.Absolute;
            theme.Apply(root); ToolkitGameplay.Apply(root, theme); document.rootVisualElement.Add(root);
            hud = E(root,"hero-corner");hud.style.left=12;hud.style.bottom=12;
            var buffs=E(hud,"row abilities");for(int i=0;i<5;i++)BuildBuff(buffs,i);
            var stats=E(hud,"surface hero");var columns=E(stats,"row hero-content");
            var portrait=ToolkitGameplay.B(columns,"",()=>TownWindowManager.Instance?.OpenWindow(TownWindowManager.Window.Skills),"hero-portrait-frame");ToolkitGameplay.Icon(portrait,definition.portrait,32);
            var vitals=E(columns,"hero-vitals");var top=E(vitals,"row hero-vital-row");defenseValue=Stat(top,StatIconLookup.StatKey.Defense);healthValue=Stat(top,StatIconLookup.StatKey.Health);bar=ToolkitGameplay.Bar(vitals,"health");var below=E(vitals,"row hero-vital-row hero-below");regenValue=Stat(below,StatIconLookup.StatKey.Regen);movementValue=Stat(below,StatIconLookup.StatKey.MoveSpeed);
            returnControls = new VisualElement(); returnControls.AddToClassList("eov-run-return"); root.Add(returnControls);
            retreat = Button(returnControls, "run-return", "Return To Town", () => GameManager.Instance?.RequestRetreat());
            returnOnDeath = Button(returnControls, "run-return-on-death", "Return On Death", () => GameManager.Instance?.RequestReturnOnDeath());
            breakdown = Button(root, "run-breakdown", "Run Breakdown", ShowBreakdown); breakdown.AddToClassList("eov-run-breakdown");
            death = Panel(root, "eov-run-death"); deathTitle = Text(death, "", ToolkitControls.TextRole.Heading);
            Button(death, "run-restart", "Restart", () => GameManager.Instance?.RequestRestartAfterDeath());
            Button(death, "run-death-return", "Return To Town", () => GameManager.Instance?.RequestReturnAfterDeath());
            Button(death, "run-death-summary", "View Summary", ShowBreakdown); deathFill = Progress(death, definition.healthTrack, definition.healthFill);
            overlay = new VisualElement(); overlay.AddToClassList("eov-run-overlay"); root.Add(overlay);
            overlay.RegisterCallback<PointerDownEvent>(e => { if (e.target == overlay) HideDetails(); });
            detail=Panel(overlay,"breakdown-sheet");var head=E(detail,"row breakdown-header");L(head,"Base rate (return bonus)","small muted breakdown-rate");detailTitle=L(head,"Run breakdown","heading breakdown-title");runTime=L(head,"","strong breakdown-time");detailSummary=L(detail,"","small");detailSummary.style.display=DisplayStyle.None;
            detailRows=ToolkitGameplay.Scroll(detail,"run-ledger");detailRows.contentContainer.AddToClassList("run-ledger-grid");var footer=E(detail,"row breakdown-footer");runRatesLeft=L(footer,"","small");runRatesRight=L(footer,"","small");runRatesRight.style.unityTextAlign=TextAnchor.MiddleRight;HideDetails();
            tooltip = Panel(root, "eov-run-tooltip", definition.tooltipFrame); tooltipText = Text(tooltip, ""); SetVisible(tooltip, false);
            Blindsided.EventHandler.OnRunStarted += RunStarted;
            jokes = FindAnyObjectByType<DadJokeManager>(FindObjectsInactive.Include);
            joke = Button(root, "town-joke", "Dad-O-Cado", ShowJoke); joke.AddToClassList("eov-town-joke");
            jokeBubble = E(root, "town-joke-bubble"); jokeBubble.name = "town-joke-bubble";
            var bubbleArt = E(jokeBubble, "town-joke-art");
            bubbleArt.pickingMode = PickingMode.Ignore;
            ToolkitTheme.Background(bubbleArt, definition.dadJokeBubble);
            jokeText = L(jokeBubble, "", "town-joke-text");
            jokeText.style.unityFontDefinition = FontDefinition.FromFont(definition.dadJokeFont ? definition.dadJokeFont : theme.gameplayFont);
            jokeText.pickingMode = PickingMode.Ignore;
            var countdownTrack = E(jokeBubble, "town-joke-countdown");
            countdownTrack.pickingMode = PickingMode.Ignore;
            jokeCountdown = E(countdownTrack, "town-joke-countdown-fill");
            jokeCountdown.pickingMode = PickingMode.Ignore;
            jokeBubble.focusable = true;
            jokeBubble.RegisterCallback<ClickEvent>(_ => DismissJoke());
            jokeBubble.RegisterCallback<NavigationSubmitEvent>(_ => DismissJoke());
            DismissJoke();
            AttachServices(); Refresh();
        }
        private void AttachServices()
        {
            if (!resources && ResourceManager.Instance) { resources = ResourceManager.Instance; resources.OnResourceAdded += ResourceAdded; }
            if (!tracker && GameplayStatTracker.Instance) { tracker = GameplayStatTracker.Instance; tracker.OnRunEnded += RunEnded; }
        }
        private Label Stat(VisualElement parent, StatIconLookup.StatKey key){var row=E(parent,"row hero-stat");StatIconLookup.TryGetIcon(key,out var icon);ToolkitGameplay.Icon(row,icon,9);row.tooltip=key.ToString();return L(row,"","small strong");}
        private VisualElement Panel(VisualElement parent, string style, Sprite sprite = null)
        { var element = new VisualElement(); element.AddToClassList(style); element.AddToClassList("surface"); parent.Add(element); return element; }
        private Label Text(VisualElement parent, string text, ToolkitControls.TextRole role = ToolkitControls.TextRole.Body)
        { return L(parent,text,role==ToolkitControls.TextRole.Heading?"heading":role==ToolkitControls.TextRole.Caption?"small":""); }
        private Button Button(VisualElement parent, string name, string text, Action action)
        { var button = ToolkitGameplay.B(parent,text,action);button.name=name; button.AddToClassList("eov-run-button"); return button; }
        private VisualElement Progress(VisualElement parent, Sprite track, Sprite fill)
        { var holder = new VisualElement(); holder.AddToClassList("track"); parent.Add(holder); var value = new VisualElement(); value.style.height = Length.Percent(100); value.AddToClassList("fill"); holder.Add(value); return value; }
        private void BuildBuff(VisualElement parent, int index)
        {
            bool suppress = false;
            var button = ToolkitControls.Button("run-buff-" + index, () => { if (suppress) { suppress = false; return; } BuffManager.Instance?.ActivateSlot(index); }, definition.buffs.slot);
            button.style.backgroundImage=StyleKeyword.None;button.AddToClassList("button");button.AddToClassList("ability");if(index==4)button.AddToClassList("last-ability");parent.Add(button);
            var icon = ToolkitGameplay.Icon(button,null,21);
            var auto = ToolkitGameplay.Icon(button,definition.buffs.autoCast,20);auto.AddToClassList("ability-echo");auto.tintColor=definition.buffs.autoCastTint;
            var fill = new ToolkitBorderProgress();button.Insert(0,fill);
            var label = Text(button, "", ToolkitControls.TextRole.Caption);
            var held = button.schedule.Execute(() => { suppress = true; BuffManager.Instance?.ToggleSlotAutoCast(index); }); held.Pause();
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { suppress = false; held.ExecuteLater(1000); } else if (e.button == 1) BuffManager.Instance?.ToggleSlotAutoCast(index); }, TrickleDown.TrickleDown);
            button.RegisterCallback<PointerUpEvent>(_ => held.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<PointerCancelEvent>(_ => held.Pause(), TrickleDown.TrickleDown);
            button.RegisterCallback<DetachFromPanelEvent>(_ => held.Pause());
            button.RegisterCallback<PointerEnterEvent>(_ => { tooltipSlot = index; RefreshTooltip(); });
            button.RegisterCallback<PointerLeaveEvent>(_ => { held.Pause(); tooltipSlot = -1; SetVisible(tooltip, false); });
            button.RegisterCallback<FocusInEvent>(_ => { tooltipSlot = index; RefreshTooltip(); });
            button.RegisterCallback<FocusOutEvent>(_ => { tooltipSlot = -1; SetVisible(tooltip, false); });
            slots.Add((button, icon, auto, label, fill));
        }
        private void Update()
        {
            if (root == null) return; AttachServices();
            var area = ToolkitWindowLayout.SafeArea;
            root.style.left = area.x; root.style.top = area.y; root.style.width = area.width; root.style.height = area.height;
            // Original town anchors in the Canvas's 768x432 reference units.
            joke.style.left = Mathf.Clamp(area.width / 2 + 303.25f, 0, Mathf.Max(0, area.width - 54.45f));
            joke.style.top = Mathf.Clamp(area.height / 2 + 138.9f, 0, Mathf.Max(0, area.height - 17));
            jokeBubble.style.left = Mathf.Clamp(area.width / 2 + 230, 0, Mathf.Max(0, area.width - 84.39f));
            jokeBubble.style.bottom = Mathf.Max(20, area.height / 2 - 135);
            // Run-summary backdrops also extend outside the HUD safe area.
            overlay.style.left = -area.x; overlay.style.top = -area.y;
            overlay.style.width = Screen.height > 0 ? Screen.width * 432f / Screen.height : 768;
            overlay.style.height = 432;
            if (jokeClosesAt > 0)
            {
                var remaining = jokeClosesAt - Time.unscaledTime;
                if (remaining <= 0) DismissJoke();
                else jokeCountdown.style.width = Length.Percent(remaining / JokeDisplaySeconds * 100);
            }
            HandleBuffHotkeys();
            if (Mouse.current?.rightButton.wasPressedThisFrame == true || Keyboard.current?.escapeKey.wasPressedThisFrame == true) HideDetails();
            if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .1f; Refresh();
        }
        private void HandleBuffHotkeys()
        {
            if (Application.isMobilePlatform || Keyboard.current == null || (ToolkitConsoleScreen.Instance && ToolkitConsoleScreen.Instance.IsActive)) return;
            var manager = BuffManager.Instance;
            if (!manager) return;
            for (int i = 0; i < slots.Count; i++)
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame ||
                    Keyboard.current[(Key)((int)Key.Numpad1 + i)].wasPressedThisFrame)
                    manager.ActivateSlot(i);
        }
        private void Refresh()
        {
            var gm = GameManager.Instance; bool inRun = gm && gm.CurrentMap;
            SetVisible(hud, inRun); SetVisible(returnControls, inRun&&!(TownWindowManager.Instance&&TownWindowManager.Instance.HasOpenWindow)); SetVisible(breakdown, inRun); SetVisible(death, inRun && gm.DeathPromptVisible);
            SetVisible(joke, !inRun && jokes && !(TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow));
            if (inRun || (TownWindowManager.Instance && TownWindowManager.Instance.HasOpenWindow)) DismissJoke();
            if (!inRun) return;
            var hero = HeroController.Instance; var health = hero ? hero.GetComponent<HeroHealth>() : null; var snap = HeroStatSystem.GetSnapshot();
            float reduction=(1-Mathf.Clamp01(Combat.ApplyDefense(1,snap.defense)))*100;
            defenseValue.text=$"{reduction:0.#}%";healthValue.text=$"{Mathf.FloorToInt(health?health.CurrentHealth:0)}/{Mathf.FloorToInt(health?health.MaxHealth:0)}";regenValue.text=$"{snap.healthRegenPerSecond:0.###}/s";movementValue.text=$"{snap.movementSpeed/3*100:0.#}%";
            bar.style.width = Length.Percent(health && health.MaxHealth > 0 ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) * 100 : 0);
            retreat.text = gm.RetreatQueued ? "Retreating..." : "Return to Town"; retreat.SetEnabled(!gm.HeroIsDead);
            returnOnDeath.text = gm.ReturnOnDeathQueued ? "Queued" : "Return on Death"; returnOnDeath.SetEnabled(!gm.HeroIsDead);
            breakdown.text = $"Run breakdown\n<size=75%>+{(tracker ? tracker.CurrentRunKills : 0) * gm.BonusPercentPerKill:0}% Resources</size>";
            deathTitle.text = gm.RunEndedByReaper ? "You were reaped..." : "You have Died..."; deathFill.style.width = Length.Percent(gm.DeathPromptProgress * 100);
            RefreshBuffs(health && health.CurrentHealth > 0 && health.gameObject.activeInHierarchy);
            if (showingBreakdown) RefreshBreakdown();
        }
        private void RefreshBuffs(bool alive)
        {
            var manager = BuffManager.Instance; if (!manager) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var row = slots[i]; var recipe = manager.GetAssigned(i); row.icon.sprite = recipe ? recipe.buffIcon : null;
                SetVisible(row.auto, manager.IsSlotAutoCasting(i)); ((ToolkitBorderProgress)row.fill).Value=0; row.text.text = "";
                bool unlocked = i < manager.UnlockedSlots; row.button.SetEnabled(unlocked && recipe); row.icon.tintColor = Color.white;
                if (!unlocked) { row.text.text = "Locked"; continue; } if (!recipe) continue;
                float remain = manager.GetRemaining(recipe), cooldown = manager.GetCooldownRemaining(recipe);
                if (!alive) { row.text.text = "Dead"; row.icon.tintColor = Color.gray; continue; }
                if (recipe.durationType == BuffDurationType.DistancePercent && tracker)
                {
                    float end = Mathf.Max(1, tracker.LongestRun) * recipe.GetDuration();
                    if (tracker.CurrentRunDistance >= end) { row.text.text = "Too Far"; row.icon.tintColor = Color.gray; continue; }
                    if (remain > 0) { row.text.text = Mathf.FloorToInt(tracker.CurrentRunDistance / end * 100) + "%"; ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(1 - tracker.CurrentRunDistance / end); continue; }
                }
                if (remain > 0) { row.text.text = CalcUtils.FormatTime(remain, remain < 10, shortForm: true); ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(remain / recipe.GetDuration()); }
                else if (cooldown > 0) { row.text.text = CalcUtils.FormatTime(cooldown, cooldown < 10, shortForm: true); row.icon.tintColor = Color.gray; ((ToolkitBorderProgress)row.fill).Value=Mathf.Clamp01(1 - cooldown / recipe.GetCooldown()); }
            }
        }
        private void RefreshTooltip()
        {
            var recipe = tooltipSlot >= 0 ? BuffManager.Instance?.GetAssigned(tooltipSlot) : null;
            SetVisible(tooltip, recipe); if (recipe) tooltipText.text = recipe.GetDisplayName() + "\n" + string.Join("\n", recipe.GetDescriptionLines());
        }
        private void RunStarted() { runResources.Clear(); elapsedAtEnd = 0; runStartedAt = Time.time; HideDetails(); }
        private void RunEnded(bool died) { elapsedAtEnd = tracker ? tracker.LastRunDuration : 0; if (died) HideDetails(); }
        private void ResourceAdded(Resource resource, double amount, bool bonus)
        { if (!tracker || !tracker.RunInProgress || !resource || resource.DisableAlterEcho || bonus || amount <= 0) return; runResources.TryGetValue(resource, out var current); runResources[resource] = current + amount; }
        private void ShowJoke()
        {
            HideDetails(); jokeText.text = jokes ? jokes.NextJoke() : "";
            jokeClosesAt = Time.unscaledTime + JokeDisplaySeconds;
            jokeCountdown.style.width = Length.Percent(100);
            SetVisible(jokeBubble, true);
        }
        private void DismissJoke() { jokeClosesAt = 0; SetVisible(jokeBubble, false); }
        public void ShowBreakdown() { detail.Q(className: "breakdown-rate").style.display=DisplayStyle.Flex; showingBreakdown = true; detailTitle.text = "Run Breakdown"; SetVisible(overlay, true); RefreshBreakdown(); }
        private void RefreshBreakdown()
        {
            double elapsed = tracker && tracker.RunInProgress ? Time.time - runStartedAt : elapsedAtEnd; double seconds = Math.Max(.0001, elapsed);
            runTime.text=CalcUtils.FormatTime((float)elapsed);runRatesLeft.text=$"Tap outside to close\nDistance / min: {(tracker?tracker.CurrentRunDistance:0)*60/seconds:N0}";runRatesRight.text=$"Damage / s: {(tracker?tracker.CurrentRunDamageDealt:0)/seconds:N0}\nKills / min: {(tracker?tracker.CurrentRunKills:0)*60/seconds:N0}";
            double bonus = (tracker ? tracker.CurrentRunKills : 0) * (GameManager.Instance ? GameManager.Instance.BonusPercentPerKill : 0) * .01;
            foreach (var pair in runResources.OrderBy(x => x.Key.resourceID)) SetResourceRow(pair.Key, $"Earned: {CalcUtils.FormatNumber(pair.Value,true)} ({CalcUtils.FormatNumber(pair.Value*(1+bonus),true)})\nPer minute: {CalcUtils.FormatNumber(pair.Value*60/seconds,true)} ({CalcUtils.FormatNumber(pair.Value*(1+bonus)*60/seconds,true)})");
        }
        public void ShowSummary(string summary, IReadOnlyDictionary<Resource, double> amounts, IReadOnlyDictionary<Resource, double> bonus)
        {
            HideDetails(); showingBreakdown = false; detailTitle.text = "Run Summary";runRatesLeft.text="Tap outside to close"; detailSummary.style.display=DisplayStyle.Flex;detailSummary.text = summary;
            foreach (var pair in amounts.OrderBy(x => x.Key.resourceID)) { bonus.TryGetValue(pair.Key, out var extra); SetResourceRow(pair.Key, CalcUtils.FormatNumber(pair.Value - extra, true) + (extra >= 1 ? $" (+{CalcUtils.FormatNumber(extra, true)})" : "")); }
            SetVisible(overlay, true);
        }
        private void SetResourceRow(Resource resource, string value)
        {
            if (!resourceRows.TryGetValue(resource, out var row)) { row = E(detailRows,"row ledger-entry");row.tooltip=resource.name;ToolkitGameplay.Icon(row,resource.icon,18);L(row,"","small").name="amount"; resourceRows.Add(resource, row); }
            row.Q<Label>("amount").text = value;
        }
        public void HideDetails() { DismissJoke(); if(detail!=null)detail.Q(className: "breakdown-rate").style.display=DisplayStyle.None; showingBreakdown = false; if(detailSummary!=null)detailSummary.style.display=DisplayStyle.None;if(runTime!=null)runTime.text="";if(runRatesLeft!=null)runRatesLeft.text="";if(runRatesRight!=null)runRatesRight.text="";SetVisible(overlay, false); detailRows?.Clear(); resourceRows.Clear(); }
        private static void SetVisible(VisualElement element, bool value) { if (element != null) element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; }
        private void OnDisable()
        {
            Blindsided.EventHandler.OnRunStarted -= RunStarted;
            if (resources) resources.OnResourceAdded -= ResourceAdded;
            if (tracker) tracker.OnRunEnded -= RunEnded;
            resources = null; tracker = null; root?.RemoveFromHierarchy(); root = null; slots.Clear(); resourceRows.Clear();
            if (settings) Destroy(settings); settings = null;
        }
    }
}
