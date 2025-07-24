using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Utility for spawning Echo helpers.
    /// </summary>
    public static class EchoManager
    {
        public static HeroController SpawnEcho(IEnumerable<Skill> skills, float duration,
            EchoType type = EchoType.All)
        {
            var hero = HeroController.Instance;
            if (hero == null)
                return null;

            HeroController.PrepareForEcho();
            var obj = Object.Instantiate(hero.gameObject, hero.transform.position, hero.transform.rotation,
                hero.transform.parent);

            var echoHero = obj.GetComponent<HeroController>();
            if (echoHero != null)
            {
                foreach (var r in obj.GetComponentsInChildren<SpriteRenderer>())
                {
                    var c = r.color;
                    c.a = 0.7f;
                    r.color = c;
                }

                // Echoes share the primary hero's health. Keep the existing
                // HeroHealth component so required dependencies remain intact
                // but flag it as an echo so damage is redirected.
                var hp = echoHero.GetComponent<HeroHealth>();
                if (hp != null)
                    hp.Immortal = false; // ensure damage can be forwarded

                bool combat = type == EchoType.Combat || type == EchoType.All;
                bool disableSkills = type == EchoType.Combat;

                echoHero.AllowAttacks = combat;

                if (disableSkills)
                {
                    var tc = obj.GetComponent<TaskController>();
                    if (tc != null)
                    {
                        Object.Destroy(tc);
                        echoHero.SetTask(null);
                        echoHero.ClearTaskController();
                    }
                }

                var echo = obj.AddComponent<EchoController>();
                echo.Init(skills, duration, type);
            }

            return echoHero;
        }

        public static HeroController SpawnEcho(Skill skill, float duration, EchoType type = EchoType.All)
        {
            return SpawnEcho(new List<Skill> { skill }, duration, type);
        }

        /// <summary>
        /// Spawn one or more Echoes using the provided configuration.
        /// </summary>
        /// <param name="config">Settings describing the Echoes to spawn. Can be null.</param>
        /// <param name="baseDuration">Base lifetime for the spawned Echoes.</param>
        /// <param name="fallbackSkills">Used when the config does not specify any skills.</param>
        /// <param name="applyLifetimeUpgrade">When true, applies the Echo Lifetime upgrade value.</param>
        /// <param name="countOverride">Optional spawn count override.</param>
        public static List<HeroController> SpawnEchoes(EchoSpawnConfig config, float baseDuration,
            IEnumerable<Skill> fallbackSkills = null, bool applyLifetimeUpgrade = false, int countOverride = 0)
        {
            float duration = baseDuration;
            if (applyLifetimeUpgrade)
            {
                var upgradeController = StatUpgradeController.Instance;
                var echoUpgrade = upgradeController?.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Echo Lifetime");
                if (echoUpgrade != null)
                    duration += upgradeController.GetTotalValue(echoUpgrade);
            }

            int count = 1;
            IEnumerable<Skill> skills = fallbackSkills;
            EchoType type = EchoType.All;

            if (config != null)
            {
                count = countOverride > 0 ? countOverride : Mathf.Max(1, config.echoCount);
                if (config.capableSkills != null && config.capableSkills.Count > 0)
                    skills = config.capableSkills;
                type = config.echoType;
            }
            else if (countOverride > 0)
            {
                count = countOverride;
            }

            var spawned = new List<HeroController>();
            for (int i = 0; i < count; i++)
            {
                var h = SpawnEcho(skills, duration, type);
                if (h != null)
                    spawned.Add(h);
            }

            return spawned;
        }
    }
}