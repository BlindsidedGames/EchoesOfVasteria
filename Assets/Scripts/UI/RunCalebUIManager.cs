using References.UI;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using System.Linq;
using TimelessEchoes.Upgrades;
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
            if (uiReferences != null && uiReferences.rightText != null)
            {
                var lines = uiReferences.rightText.text.Split('\n');
                if (lines.Length >= 3)
                {
                    lines[2] = $"HP: {Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
                    uiReferences.rightText.text = string.Join("\n", lines);
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
            var controller = StatUpgradeController.Instance;
            var regenUpgrade = controller?.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Regeneration");
            var regen = controller && regenUpgrade ? controller.GetTotalValue(regenUpgrade) : 0f;

            if (!force && Mathf.Approximately(baseDamage, lastBaseDamage) && Mathf.Approximately(bonusDamage, lastBonusDamage)
                && Mathf.Approximately(attack, lastAttack)
                && Mathf.Approximately(move, lastMove) && Mathf.Approximately(defense, lastDefense)
                && Mathf.Approximately(regen, lastRegen))
                return;

            lastBaseDamage = baseDamage;
            lastBonusDamage = bonusDamage;
            lastAttack = attack;
            lastMove = move;
            lastDefense = defense;
            lastRegen = regen;

            if (uiReferences.leftText != null)
            {
                string dmgLine = $"Damage: {baseDamage:0.##}";
                if (bonusDamage > 0f)
                    dmgLine += $" (+{bonusDamage:0.##})";
                uiReferences.leftText.text =
                    dmgLine + "\n" +
                    $"Attack Rate: {attack:0.###} /s\n" +
                    $"Movement Speed {move:0.##}";
            }

            if (uiReferences.rightText != null)
            {
                // Convert flat defense into a damage reduction percent using the global combat formula
                float damageFraction = TimelessEchoes.Combat.ApplyDefense(1f, defense);
                float reductionPercent = (1f - Mathf.Clamp01(damageFraction)) * 100f;

                var hpLine = heroHealth != null
                    ? $"HP: {Mathf.FloorToInt(heroHealth.CurrentHealth)} / {Mathf.FloorToInt(heroHealth.MaxHealth)}"
                    : string.Empty;
                uiReferences.rightText.text =
                    $"Defense: {reductionPercent:0.#}%\n" +
                    $"Regen: {regen:0.###} /s\n" +
                    hpLine;
            }
        }
    }
}