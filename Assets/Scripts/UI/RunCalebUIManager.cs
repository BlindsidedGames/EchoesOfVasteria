using References.UI;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;
using TimelessEchoes.Regen;
using UnityEngine;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Updates the run UI with the hero's current stats and handles the skills button.
    /// </summary>
    public class RunCalebUIManager : MonoBehaviour
    {
        [SerializeField] private RunCalebUIReferences uiReferences;
        [SerializeField] private GameObject skillsWindow;
        [SerializeField] private BuffManager buffManager;
        [SerializeField] private RegenManager regenManager;

        private HeroController hero;
        private Health heroHealth;

        private float lastDamage;
        private float lastAttack;
        private float lastMove;
        private float lastDefense;
        private float lastRegen;

        private void Awake()
        {
            if (uiReferences == null)
                uiReferences = GetComponent<RunCalebUIReferences>();
            if (buffManager == null)
                buffManager = BuffManager.Instance ?? FindFirstObjectByType<BuffManager>();
            if (regenManager == null)
                regenManager = FindFirstObjectByType<RegenManager>();
            if (uiReferences != null && uiReferences.skillsButton != null && skillsWindow != null)
                uiReferences.skillsButton.onClick.AddListener(ToggleSkills);
        }

        private void OnDestroy()
        {
            if (uiReferences != null && uiReferences.skillsButton != null)
                uiReferences.skillsButton.onClick.RemoveListener(ToggleSkills);
            if (heroHealth != null)
                heroHealth.OnHealthChanged -= OnHealthChanged;
        }

        private void OnEnable()
        {
            hero = FindFirstObjectByType<HeroController>();
            heroHealth = hero ? hero.GetComponent<Health>() : null;
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
        }

        private void ToggleSkills()
        {
            if (skillsWindow != null)
                skillsWindow.SetActive(!skillsWindow.activeSelf);
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

            var damage = hero.Damage;
            var attack = hero.AttackRate;
            var move = hero.MoveSpeed;
            var defense = hero.Defense;
            var regen = regenManager ? (float)regenManager.GetTotalRegen() : 0f;

            if (!force && Mathf.Approximately(damage, lastDamage) && Mathf.Approximately(attack, lastAttack)
                && Mathf.Approximately(move, lastMove) && Mathf.Approximately(defense, lastDefense)
                && Mathf.Approximately(regen, lastRegen))
                return;

            lastDamage = damage;
            lastAttack = attack;
            lastMove = move;
            lastDefense = defense;
            lastRegen = regen;

            if (uiReferences.leftText != null)
                uiReferences.leftText.text =
                    $"Damage: {damage:0.##}\n" +
                    $"Attack Speed: {attack:0.###} /s\n" +
                    $"Movement Speed {move:0.##}";

            if (uiReferences.rightText != null)
            {
                var hpLine = heroHealth != null
                    ? $"HP: {Mathf.FloorToInt(heroHealth.CurrentHealth)} / {Mathf.FloorToInt(heroHealth.MaxHealth)}"
                    : string.Empty;
                uiReferences.rightText.text =
                    $"Defense: {defense:0.##}\n" +
                    $"Regen: {regen:0.###} /s\n" +
                    hpLine;
            }
        }
    }
}