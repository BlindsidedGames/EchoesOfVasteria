using System.Linq;
using UnityEngine;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Gear;

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

            float upgradeRegen = 0f;
            var controller = StatUpgradeController.Instance ?? FindFirstObjectByType<StatUpgradeController>();
            if (controller != null)
            {
                var regenUpgrade = controller.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Regeneration");
                if (regenUpgrade != null)
                    upgradeRegen = controller.GetTotalValue(regenUpgrade);
            }

            float gearRegen = 0f;
            var equip = EquipmentController.Instance ?? FindFirstObjectByType<EquipmentController>();
            if (equip != null)
                gearRegen = equip.GetTotalForMapping(HeroStatMapping.HealthRegen);

            var buff = TimelessEchoes.Buffs.BuffManager.Instance ?? FindFirstObjectByType<TimelessEchoes.Buffs.BuffManager>();
            float regenMultiplier = buff != null ? (1f + Mathf.Max(0f, buff.HealthRegenPercent) / 100f) : 1f;
            float totalRegen = (upgradeRegen + gearRegen) * regenMultiplier;
            if (totalRegen > 0f && health.CurrentHealth < health.MaxHealth)
                health.Heal(totalRegen * Time.deltaTime);
        }
    }
}
