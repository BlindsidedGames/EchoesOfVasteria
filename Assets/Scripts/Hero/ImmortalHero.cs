using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Makes a hero clone immortal by resetting health on death.
    /// </summary>
    [RequireComponent(typeof(HeroHealth))]
    public class ImmortalHero : MonoBehaviour
    {
        private HeroHealth health;

        private void Awake()
        {
            health = GetComponent<HeroHealth>();
            if (health != null)
                health.OnDeath += OnDeath;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
        }

        private void OnDeath()
        {
            if (health != null)
                health.Init((int)health.MaxHealth);
        }
    }
}
