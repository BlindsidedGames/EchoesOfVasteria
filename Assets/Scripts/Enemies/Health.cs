using UnityEngine;
using Blindsided.Utilities;
using TMPro;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    /// Standard health component for enemies.
    /// </summary>
    public class Health : HealthBase
    {
        [SerializeField] private GameObject healthBarParent;
        [SerializeField] private TMP_Text healthText;

        protected override void Awake()
        {
            OnHealthChanged += HandleHealthChanged;
            base.Awake();
            if (healthBarParent != null)
                healthBarParent.SetActive(false);
            else if (healthBar != null)
                healthBar.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            OnHealthChanged -= HandleHealthChanged;
        }

        protected override Color GetFloatingTextColor()
        {
            ColorUtility.TryParseHtmlString("#C69B60", out var orange);
            return orange;
        }

        protected override float GetFloatingTextSize() => 8f;

        protected override float GetFloatingTextDuration()
        {
            return Blindsided.SaveData.StaticReferences.EnemyDamageTextDuration;
        }

        protected override bool ShouldShowFloatingText()
        {
            return Blindsided.SaveData.StaticReferences.EnemyFloatingDamage;
        }

        public override void TakeDamage(float amount, float bonusDamage = 0f)
        {
            if (CurrentHealth <= 0f) return;

            var enemy = GetComponent<Enemy>();
            var defense = enemy != null ? enemy.GetDefense() : 0f;

            float full = amount + bonusDamage;
            float total = Mathf.Max(full - defense, full * 0.1f);

            CurrentHealth -= total;
            UpdateBar();
            RaiseHealthChanged();

            if (Application.isPlaying && ShouldShowFloatingText())
            {
                // Display only the final damage amount dealt to the enemy.
                string text = CalcUtils.FormatNumber(total);
                FloatingText.Spawn(text, transform.position + Vector3.up, GetFloatingTextColor(),
                    GetFloatingTextSize(), null, GetFloatingTextDuration());
            }

            AfterDamage(total);

            if (CurrentHealth <= 0f)
                OnZeroHealth();
        }

        public void SetHealthBarVisible(bool visible)
        {
            if (healthBarParent != null)
                healthBarParent.SetActive(visible);
            else if (healthBar != null)
                healthBar.gameObject.SetActive(visible);
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthText != null)
            {
                int shownCurrent = Mathf.FloorToInt(current);
                if (shownCurrent == 0 && current > 0f)
                    shownCurrent = 1;
                healthText.text = $"{shownCurrent} / {Mathf.FloorToInt(max)}";
            }
        }

        protected override void OnZeroHealth()
        {
            base.OnZeroHealth();
            if (GetComponent<Enemy>() != null)
                Destroy(gameObject);
        }
    }
}
