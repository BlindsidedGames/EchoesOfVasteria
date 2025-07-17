using UnityEngine;
using TimelessEchoes.Audio;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Provides audio hooks for the hero. Public methods can be triggered via
    /// animation events or tags to play appropriate sound effects.
    /// </summary>
    public class HeroAudio : MonoBehaviour
    {
        private AudioManager Audio => AudioManager.Instance ??
            Object.FindFirstObjectByType<AudioManager>();

        public void PlayWoodcutting()
        {
            Audio?.PlayTaskClip(AudioManager.TaskType.Woodcutting);
        }

        public void PlayFarming()
        {
            Audio?.PlayTaskClip(AudioManager.TaskType.Farming);
        }

        public void PlayFishing()
        {
            Audio?.PlayTaskClip(AudioManager.TaskType.Fishing);
        }

        public void PlayMining()
        {
            Audio?.PlayTaskClip(AudioManager.TaskType.Mining);
        }

        public void PlayCombat()
        {
            Audio?.PlaySlimeClip();
        }

        public void PlayWeaponSwing()
        {
            Audio?.PlayWeaponSwingClip();
        }

        public void PlayChestOpen()
        {
            Audio?.PlayChestOpenClip();
        }

        public void PlayFishCatch()
        {
            Audio?.PlayFishCatchClip();
        }
    }
}
