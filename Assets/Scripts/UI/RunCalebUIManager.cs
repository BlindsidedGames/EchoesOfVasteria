using References.UI;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using System.Linq;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Updates the run UI with the hero's current stats and handles the skills button.
    /// </summary>
    public class RunCalebUIManager : MonoBehaviour
    {
        public static RunCalebUIManager Instance { get; private set; }
        [SerializeField] private RunCalebUIReferences uiReferences;
        [SerializeField] private GameObject skillsWindow;
        [SerializeField] private BuffManager buffManager;

        public bool IsSkillsWindowOpen => skillsWindow != null && skillsWindow.activeSelf;

        private HeroController hero;
        private HeroHealth heroHealth;

        private float lastBaseDamage;
        private float lastBonusDamage;
        private float lastAttack;
        private float lastMove;
        private float lastDefense;
        private float lastRegen;
        private float lastCrit;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (uiReferences == null)
                uiReferences = GetComponent<RunCalebUIReferences>();
            if (buffManager == null)
                buffManager = BuffManager.Instance ?? FindFirstObjectByType<BuffManager>();
            if (uiReferences != null && uiReferences.skillsButton != null && skillsWindow != null)
                uiReferences.skillsButton.onClick.AddListener(ToggleSkills);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (uiReferences != null && uiReferences.skillsButton != null)
                uiReferences.skillsButton.onClick.RemoveListener(ToggleSkills);
            if (heroHealth != null)
                heroHealth.OnHealthChanged -= OnHealthChanged;
        }

        private void OnEnable()
        {
            hero = HeroController.Instance ?? FindFirstObjectByType<HeroController>();
            heroHealth = hero ? hero.GetComponent<HeroHealth>() : null;
            // Ensure HUD text fields use the StatIcons sprite asset so <sprite=..> tags render
            var spriteAsset = StatIconLookup.GetSpriteAsset();
            if (uiReferences != null)
            {
                if (uiReferences.leftText != null)
                    uiReferences.leftText.spriteAsset = spriteAsset != null ? spriteAsset : uiReferences.leftText.spriteAsset;
                if (uiReferences.middleText != null)
                    uiReferences.middleText.spriteAsset = spriteAsset != null ? spriteAsset : uiReferences.middleText.spriteAsset;
                if (uiReferences.rightText != null)
                    uiReferences.rightText.spriteAsset = spriteAsset != null ? spriteAsset : uiReferences.rightText.spriteAsset;
            }
            if (heroHealth != null)
            {
                heroHealth.OnHealthChanged += OnHealthChanged;
                OnHealthChanged(heroHealth.CurrentHealth, heroHealth.MaxHealth);
                if (uiReferences != null)
                    heroHealth.HealthBar = uiReferences.healthBar;
            }

            UpdateStats(true);
        }

        private void OnDisable()
        {
            if (heroHealth != null)
                heroHealth.OnHealthChanged -= OnHealthChanged;
        }

        private void Update()
        {
            UpdateStats();
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (skillsWindow != null && skillsWindow.activeSelf)
                    skillsWindow.SetActive(false);
            }
        }

        private void ToggleSkills()
        {
            if (skillsWindow != null)
            {
                bool newState = !skillsWindow.activeSelf;
                skillsWindow.SetActive(newState);
                if (newState)
                {
                    var tooltip = FindFirstObjectByType<RunBuffTooltipUIReferences>();
                    if (tooltip != null && tooltip.tooltipPanel != null)
                        tooltip.tooltipPanel.SetActive(false);
                }
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            if (uiReferences != null && uiReferences.leftText != null)
            {
                var lines = uiReferences.leftText.text.Split('\n');
                if (lines.Length >= 3)
                {
                    var hpTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.MaxHealth);
                    lines[2] = $"{hpTag} {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
                    uiReferences.leftText.text = string.Join("\n", lines);
                }
            }
        }

        private void UpdateStats(bool force = false)
        {
            if (uiReferences == null || hero == null)
                return;

            var baseDamage = hero.BaseDamage;
            var totalDamage = hero.Damage;
            var bonusDamage = totalDamage - baseDamage;
            var attack = hero.AttackRate;
            var move = hero.MoveSpeed;
            var defense = hero.Defense;
            float critChance = 0f;
            var equip = TimelessEchoes.Gear.EquipmentController.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.EquipmentController>();
            if (equip != null)
            {
                var crafting = TimelessEchoes.Gear.CraftingService.Instance ?? FindFirstObjectByType<TimelessEchoes.Gear.CraftingService>();
                var critDef = crafting != null ? crafting.GetStatByMapping(TimelessEchoes.Gear.HeroStatMapping.CritChance) : null;
                if (critDef != null)
                {
                    var raw = equip.GetCritChance(critDef);
                    critChance = critDef.isPercent ? raw : raw * 100f;
                }
            }
            var controller = StatUpgradeController.Instance;
            var regenUpgrade = controller?.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Regeneration");
            float upgradeRegen = controller && regenUpgrade ? controller.GetTotalValue(regenUpgrade) : 0f;

            float gearRegen = 0f;
            if (equip != null)
                gearRegen = equip.GetTotalForMapping(TimelessEchoes.Gear.HeroStatMapping.HealthRegen);
            var regen = upgradeRegen + gearRegen;

            if (!force && Mathf.Approximately(baseDamage, lastBaseDamage) && Mathf.Approximately(bonusDamage, lastBonusDamage)
                && Mathf.Approximately(attack, lastAttack) && Mathf.Approximately(critChance, lastCrit)
                && Mathf.Approximately(move, lastMove) && Mathf.Approximately(defense, lastDefense)
                && Mathf.Approximately(regen, lastRegen))
                return;

            lastBaseDamage = baseDamage;
            lastBonusDamage = bonusDamage;
            lastAttack = attack;
            lastCrit = critChance;
            lastMove = move;
            lastDefense = defense;
            lastRegen = regen;

            if (uiReferences.leftText != null)
            {
                // Convert flat defense into a damage reduction percent using the global combat formula
                float damageFraction = TimelessEchoes.Combat.ApplyDefense(1f, defense);
                float reductionPercent = (1f - Mathf.Clamp01(damageFraction)) * 100f;

                var defTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.Defense);
                var regenTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.HealthRegen);
                var hpTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.MaxHealth);

                var hpLine = heroHealth != null
                    ? $"{hpTag} {Mathf.FloorToInt(heroHealth.CurrentHealth)} / {Mathf.FloorToInt(heroHealth.MaxHealth)}"
                    : string.Empty;
                uiReferences.leftText.text =
                    $"{defTag} {reductionPercent:0.#}%\n" +
                    $"{regenTag} {regen:0.###} /s\n" +
                    hpLine;
            }

            if (uiReferences.middleText != null)
            {
                var dmgTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.Damage);
                var atkTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.AttackRate);
                var critTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.CritChance);

                string dmgLine = $"{dmgTag} {totalDamage:0.##}";
                if (bonusDamage > 0f)
                    dmgLine += $" (+{bonusDamage:0.##})";
                uiReferences.middleText.text =
                    dmgLine + "\n" +
                    $"{atkTag} {attack:0.###} /s\n" +
                    $"{critTag} {critChance:0.#}%";
            }

            if (uiReferences.rightText != null)
            {
                var moveTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.MoveSpeed);
                uiReferences.rightText.text = $"{moveTag} {move:0.##}";
            }
        }
    }
}