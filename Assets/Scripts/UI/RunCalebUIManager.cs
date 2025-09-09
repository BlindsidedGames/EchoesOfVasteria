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

            // Subscribe to centralized stat recalculation events and draw immediately
            HeroStatSystem.OnStatsRecalculated += OnStatsRecalculated;
            HeroController.OnMainHeroDiceChanged += HandleMainHeroDiceChanged;
            OnStatsRecalculated(HeroStatSystem.GetSnapshot());
        }

        private void OnDisable()
        {
            if (heroHealth != null)
                heroHealth.OnHealthChanged -= OnHealthChanged;
            HeroStatSystem.OnStatsRecalculated -= OnStatsRecalculated;
            HeroController.OnMainHeroDiceChanged -= HandleMainHeroDiceChanged;
        }

        private void Update()
        {
            // Only handle input/UI toggles; stat text updates are event-driven
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

        private void OnStatsRecalculated(HeroStatsSnapshot snap)
        {
            if (uiReferences == null || hero == null)
                return;

            // Show the controller's effective damage (includes local dice multiplier for hero/echo)
            var totalDamage = hero != null ? hero.Damage : snap.damage;
            var attack = snap.attacksPerSecond;
            var move = snap.movementSpeed;
            var defense = snap.defense;
            var critChance = snap.critChancePercent;
            var regen = snap.healthRegenPerSecond;

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
                uiReferences.middleText.text =
                    dmgLine + "\n" +
                    $"{atkTag} {attack:0.###} /s\n" +
                    $"{critTag} {critChance:0.#}%";
            }

            if (uiReferences.rightText != null)
            {
                // Display movement as 100%..400% based on final speed mapping [3..12]
                var moveTag = StatIconLookup.GetIconTag(TimelessEchoes.Gear.HeroStatMapping.MoveSpeed);
                float percent;
                {
                    const float minSpeed = 3f;
                    const float maxSpeed = 12f;
                    var t = Mathf.InverseLerp(minSpeed, maxSpeed, Mathf.Clamp(move, minSpeed, maxSpeed));
                    percent = 100f + 300f * t; // 3 => 100%, 12 => 400%
                }
                uiReferences.rightText.text = $"{moveTag} {percent:0.#}%";
            }
        }

        private void HandleMainHeroDiceChanged()
        {
            // Force a HUD redraw to reflect the main hero's local dice multiplier
            OnStatsRecalculated(HeroStatSystem.GetSnapshot());
        }
    }
}
