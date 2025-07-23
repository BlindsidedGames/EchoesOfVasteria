using System.Linq;
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
            if (controller == null)
                return;

            var regenUpgrade = controller.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Regeneration");
            if (regenUpgrade == null)
                return;

            float regen = controller.GetTotalValue(regenUpgrade);
            if (regen > 0f && health.CurrentHealth < health.MaxHealth)
                health.Heal(regen * Time.deltaTime);
        }
    }
}
