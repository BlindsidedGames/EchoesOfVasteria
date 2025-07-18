using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Hero health variant used by clones. Prevents death while active.
    /// </summary>
    public class CloneHeroHealth : HeroHealth
    {
        protected override void OnZeroHealth()
        {
            // Clones are immortal; reset to max health instead of dying.
            CurrentHealth = MaxHealth;
            RaiseHealthChanged();
        }
    }
}
