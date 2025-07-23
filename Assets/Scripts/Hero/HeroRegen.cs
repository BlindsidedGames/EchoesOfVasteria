using UnityEngine;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Applies health regeneration to the hero based on the Regeneration stat upgrade.
    /// </summary>
    [RequireComponent(typeof(HeroHealth))]
    public class HeroRegen : MonoBehaviour
    {
        private HeroHealth health;
        [SerializeField] private StatUpgrade regenerationUpgrade;

        private void Awake()
        {
            health = GetComponent<HeroHealth>();
        }

        private void Update()
        {
            if (health == null)
                return;

            var controller = StatUpgradeController.Instance ??
                              FindFirstObjectByType<StatUpgradeController>();
            if (controller == null || regenerationUpgrade == null)
                return;

            float regen = controller.GetTotalValue(regenerationUpgrade);
            if (regen > 0f && health.CurrentHealth < health.MaxHealth)
                health.Heal(regen * Time.deltaTime);
        }
    }
}
