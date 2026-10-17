using System.Collections.Generic;
using TimelessEchoes.Hero;
using TimelessEchoes.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimelessEchoes.UI.Toolkit
{
    public sealed partial class ToolkitRunScreen
    {
        private static readonly Color HeroForeground = new Color32(234, 221, 209, 255);
        private readonly Dictionary<StatIconLookup.StatKey, Label> heroStatValues = new();
        private VisualElement heroStatsPanel;
        private Label heroHealthText;
        private Button heroStatsToggle;
        private bool heroStatsOpen;

        private void SetHeroStatsOpen(bool open)
        {
            heroStatsOpen = open;
            SetVisible(heroStatsPanel, open);
            heroStatsToggle?.EnableInClassList("active", open);
        }

        private void RefreshHeroHud(HeroController hero, HeroHealth health)
        {
            var snapshot = HeroStatSystem.GetSnapshot();
            var maximum = health ? health.MaxHealth : snapshot.maxHealth;
            var current = health ? health.CurrentHealth : maximum;
            heroHealthText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(maximum)}";
            bar.style.width = Length.Percent(maximum > 0 ? Mathf.Clamp01(current / maximum) * 100 : 0);
            HeroValue(StatIconLookup.StatKey.Damage, (hero ? hero.Damage : snapshot.damage).ToString("0.##"));
            HeroValue(StatIconLookup.StatKey.AttackRate, snapshot.attacksPerSecond.ToString("0.##") + "/s");
            HeroValue(StatIconLookup.StatKey.CritChance, snapshot.critChancePercent.ToString("0.#") + "%");
            HeroValue(StatIconLookup.StatKey.CritDamage, snapshot.critDamagePercent.ToString("0.#") + "%");
            HeroValue(StatIconLookup.StatKey.Defense, ((1 - Mathf.Clamp01(Combat.ApplyDefense(1, snapshot.defense))) * 100).ToString("0.#") + "%");
            HeroValue(StatIconLookup.StatKey.Regen, snapshot.healthRegenPerSecond.ToString("0.##") + "/s");
            HeroValue(StatIconLookup.StatKey.MoveSpeed, (snapshot.movementSpeed / 3 * 100).ToString("0.#") + "%");
        }

        private void HeroValue(StatIconLookup.StatKey key, string text) => heroStatValues[key].text = text;
        private VisualElement HeroRow(VisualElement parent)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            parent.Add(row); return row;
        }
        private Label HeroText(VisualElement parent, string text, int size = 8)
        {
            var label = new Label(text); label.style.color = HeroForeground; label.style.fontSize = size;
            label.style.marginTop = label.style.marginBottom = label.style.marginLeft = label.style.marginRight = 0;
            parent.Add(label); return label;
        }
        private void HeroMetric(VisualElement parent, StatIconLookup.StatKey key, string name)
        {
            var row = HeroRow(parent); row.style.height = 13;
            StatIconLookup.TryGetIcon(key, out var sprite);
            var icon = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.style.width = icon.style.height = 9; icon.style.marginRight = 5; row.Add(icon);
            var label = HeroText(row, ""); label.style.flexGrow = 1;
            ToolkitLocalization.Bind(label, "run.stat-label-" + key.ToString().ToLowerInvariant(), name);
            var value = HeroText(row, ""); value.style.unityTextAlign = TextAnchor.MiddleRight;
            heroStatValues[key] = value;
        }
        private void HeroSurface(VisualElement element)
        {
            element.style.backgroundColor = (Color)new Color32(43, 36, 37, 255);
            element.style.borderLeftWidth = element.style.borderRightWidth = element.style.borderTopWidth = element.style.borderBottomWidth = .5f;
            element.style.borderLeftColor = element.style.borderRightColor = element.style.borderTopColor = element.style.borderBottomColor = (Color)new Color32(126, 104, 78, 255);
        }
        private void BuildHeroHud()
        {
            var healthStrip = new VisualElement { name = "run-health-strip" };
            hud.style.width = 154;
            healthStrip.style.width = 154; healthStrip.style.height = 24;
            healthStrip.style.flexDirection = FlexDirection.Row; healthStrip.style.alignItems = Align.Center;
            healthStrip.style.paddingLeft = 5; healthStrip.style.paddingRight = 3;
            HeroSurface(healthStrip); hud.Add(healthStrip);

            StatIconLookup.TryGetIcon(StatIconLookup.StatKey.Health, out var heartSprite);
            var heart = new Image { sprite = heartSprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            heart.style.width = heart.style.height = 10; heart.style.marginRight = 5; healthStrip.Add(heart);
            var health = new VisualElement(); health.style.flexGrow = 1; health.style.marginRight = 6; healthStrip.Add(health);
            heroHealthText = HeroText(health, "", 8);
            var track = new VisualElement(); track.style.height = 3; track.style.marginTop = 2;
            track.style.backgroundColor = (Color)new Color32(104, 76, 63, 255); health.Add(track);
            bar = new VisualElement(); bar.style.height = Length.Percent(100);
            bar.style.backgroundColor = (Color)new Color32(101, 185, 68, 255); track.Add(bar);
            heroStatsToggle = new Button(() => SetHeroStatsOpen(!heroStatsOpen)) { name = "run-stats-toggle" };
            var toggle = heroStatsToggle;
            ToolkitLocalization.Bind(toggle, "run.stats", "Stats");
            toggle.RegisterCallback<ClickEvent>(_ => TimelessEchoes.Audio.AudioManager.Instance?.PlayUIButtonClick());
            ToolkitLocalization.BindTooltip(toggle, "run.stats-tooltip", "Show or hide combat stats"); toggle.style.fontSize = 8; toggle.style.color = HeroForeground;
            toggle.style.minWidth = 31; toggle.style.height = 18; toggle.style.marginLeft = toggle.style.marginRight = toggle.style.marginTop = toggle.style.marginBottom = 0;
            toggle.style.paddingLeft = toggle.style.paddingRight = toggle.style.paddingTop = toggle.style.paddingBottom = 0;
            HeroSurface(toggle); healthStrip.Add(toggle);

            heroStatsPanel = new VisualElement { name = "run-stats-panel" };
            heroStatsPanel.style.position = Position.Absolute; heroStatsPanel.style.left = 0; heroStatsPanel.style.bottom = Length.Percent(100); heroStatsPanel.style.marginBottom = 4;
            heroStatsPanel.style.width = 154; heroStatsPanel.style.paddingLeft = heroStatsPanel.style.paddingRight = 7;
            heroStatsPanel.style.paddingTop = heroStatsPanel.style.paddingBottom = 5;
            heroStatsOpen = false; heroStatsPanel.style.display = DisplayStyle.None;
            HeroSurface(heroStatsPanel); hud.Add(heroStatsPanel);
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.Damage, "Damage");
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.AttackRate, "Attack speed");
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.CritChance, "Crit chance");
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.CritDamage, "Crit damage");
            var line = new VisualElement(); line.style.height = .5f; line.style.marginTop = line.style.marginBottom = 3;
            line.style.backgroundColor = (Color)new Color32(112, 86, 73, 255); heroStatsPanel.Add(line);
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.Defense, "Damage reduction");
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.Regen, "Health regen");
            HeroMetric(heroStatsPanel, StatIconLookup.StatKey.MoveSpeed, "Movement speed");
        }
    }
}
