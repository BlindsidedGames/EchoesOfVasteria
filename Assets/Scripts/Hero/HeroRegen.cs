using UnityEngine;
using TimelessEchoes.Regen;
using TimelessEchoes.Enemies;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Applies health regeneration to the hero based on donations handled by <see cref="RegenManager"/>.
    /// </summary>
    [RequireComponent(typeof(HeroHealth))]
    public class HeroRegen : MonoBehaviour
    {
        private HeroHealth health;
        private RegenManager regenManager;

        private void Awake()
        {
            health = GetComponent<HeroHealth>();
            if (regenManager == null)
                regenManager = FindFirstObjectByType<RegenManager>();
        }

        private void Update()
        {
            if (health == null || regenManager == null)
                return;

            float regen = (float)regenManager.GetTotalRegen();
            if (regen > 0f && health.CurrentHealth < health.MaxHealth)
                health.Heal(regen * Time.deltaTime);
        }
    }
}
