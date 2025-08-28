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

            // Use centralized stat snapshot for regen per second
            var snap = HeroStatSystem.GetSnapshot();
            float totalRegen = snap.healthRegenPerSecond;
            if (totalRegen > 0f && health.CurrentHealth < health.MaxHealth)
                health.Heal(totalRegen * Time.deltaTime);
        }
    }
}
