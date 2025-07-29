using UnityEngine;
using Blindsided.Utilities;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    /// Standard health component for enemies.
    /// </summary>
    public class Health : HealthBase
    {
        [SerializeField] private GameObject healthBarParent;

        protected override void Awake()
        {
            base.Awake();
            if (healthBarParent != null)
                healthBarParent.SetActive(false);
            else if (healthBar != null)
                healthBar.gameObject.SetActive(false);
        }

        protected override Color GetFloatingTextColor()
        {
            ColorUtility.TryParseHtmlString("#C69B60", out var orange);
            return orange;
        }

        protected override float GetFloatingTextSize() => 8f;

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

            if (Application.isPlaying)
            {
                string text = CalcUtils.FormatNumber(total);
                if (bonusDamage != 0f)
                    text += $"<size=70%><color=#60C560>+{CalcUtils.FormatNumber(bonusDamage)}</color></size>";
                if (defense != 0f)
                    text += $"<size=70%><color=#C69B60>-{CalcUtils.FormatNumber(defense)}</color></size>";
                FloatingText.Spawn(text, transform.position + Vector3.up, GetFloatingTextColor(), GetFloatingTextSize());
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

        protected override void OnZeroHealth()
        {
            base.OnZeroHealth();
            if (GetComponent<Enemy>() != null)
                Destroy(gameObject);
        }
    }
}
