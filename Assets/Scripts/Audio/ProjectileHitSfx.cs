using UnityEngine;

namespace TimelessEchoes.Audio
{
    /// <summary>
    /// Plays a hit sound effect based on projectile type.
    /// Attach to projectile prefabs and invoke <see cref="PlayHit"/> on impact.
    /// </summary>
    public class ProjectileHitSfx : MonoBehaviour
    {
        private AudioManager Audio => AudioManager.Instance ??
            Object.FindFirstObjectByType<AudioManager>();

        public enum HitType
        {
            Slime
        }

        [SerializeField] private HitType hitType = HitType.Slime;

        public void PlayHit()
        {
            switch (hitType)
            {
                case HitType.Slime:
                    Audio?.PlaySlimeClip();
                    break;
            }
        }
    }
}
