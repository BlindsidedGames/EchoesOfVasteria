using System.Collections.Generic;
using System.Linq;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;
using Blindsided.Utilities.Pooling;

namespace TimelessEchoes.Hero
{
    /// <summary>
    ///     Utility for spawning Echo helpers.
    /// </summary>
    public static class EchoManager
    {
        public static EchoController SpawnEcho(IEnumerable<Skill> skills, float duration,
            EchoType type = EchoType.All)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.EchoPrefab == null || HeroController.Instance == null)
                return null;

            var pos = HeroController.Instance.transform.position;
            var parent = HeroController.Instance.transform.parent;
            var obj = PoolManager.Get(gm.EchoPrefab);
            // Ensure transform hierarchy and placement match the hero
            if (obj != null)
            {
                obj.transform.SetParent(parent, false);
                obj.transform.SetPositionAndRotation(pos, HeroController.Instance.transform.rotation);
            }

            // Visual alpha tint (optional)
            foreach (var r in obj.GetComponentsInChildren<SpriteRenderer>())
            {
                var c = r.color;
                c.a = 0.7f;
                r.color = c;
            }

            var echo = obj.GetComponent<EchoController>();
            if (echo == null)
                echo = obj.AddComponent<EchoController>();

            var combat = type == EchoType.Combat || type == EchoType.All;
            var disableSkills = type == EchoType.Combat;
            echo.AllowAttacks = combat;

            // Ensure echoes never carry their own TaskController; they should use the map singleton
            var tc = obj.GetComponent<TaskController>();
            if (tc != null && tc != TaskController.Instance)
                Object.Destroy(tc);
            if (disableSkills)
            {
                echo.SetTask(null);
                echo.ClearTaskController();
            }

            echo.Init(skills, duration, type);
            return echo;
        }

        public static EchoController SpawnEcho(Skill skill, float duration, EchoType type = EchoType.All)
        {
            return SpawnEcho(new List<Skill> { skill }, duration, type);
        }

        /// <summary>
        ///     Spawn one or more Echoes using the provided configuration.
        /// </summary>
        /// <param name="config">Settings describing the Echoes to spawn. Can be null.</param>
        /// <param name="baseDuration">Base lifetime for the spawned Echoes.</param>
        /// <param name="fallbackSkills">Used when the config does not specify any skills.</param>
        /// <param name="applyLifetimeUpgrade">When true, applies the Echo Lifetime upgrade value.</param>
        /// <param name="count">Number of echoes to spawn.</param>
        public static List<EchoController> SpawnEchoes(EchoSpawnConfig config, float baseDuration,
            IEnumerable<Skill> fallbackSkills = null, bool applyLifetimeUpgrade = false, int count = 1)
        {
            var duration = baseDuration;
            if (applyLifetimeUpgrade)
            {
                var upgradeController = StatUpgradeController.Instance;
                var echoUpgrade =
                    upgradeController?.AllUpgrades.FirstOrDefault(u => u != null && u.name == "Echo Lifetime");
                if (echoUpgrade != null)
                    duration += upgradeController.GetTotalValue(echoUpgrade);
            }

            var skills = fallbackSkills;
            var type = EchoType.All;

            if (config != null)
            {
                if (config.capableSkills != null && config.capableSkills.Count > 0)
                    skills = config.capableSkills;
                type = config.echoType;
            }
            if (count <= 0)
                count = 1;

            var spawned = new List<EchoController>();
            for (var i = 0; i < count; i++)
            {
                var h = SpawnEcho(skills, duration, type);
                if (h != null)
                    spawned.Add(h);
            }

            return spawned;
        }
    }
}
