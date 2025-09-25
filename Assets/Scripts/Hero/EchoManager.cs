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
        private const int DefaultEchoCap = 10;
        private static readonly Dictionary<EchoType, int> CustomEchoCaps = new();

        public static void SetEchoCap(EchoType type, int cap)
        {
            CustomEchoCaps[type] = Mathf.Max(0, cap);
        }

        public static int GetEchoCap(EchoType type)
        {
            return ResolveEchoCap(type);
        }

        private static int ResolveEchoCap(EchoType type)
        {
            return CustomEchoCaps.TryGetValue(type, out var cap) ? cap : DefaultEchoCap;
        }

        private static void EnforceTypeCap(EchoController newest)
        {
            if (newest == null || newest.ExcludedFromCap)
                return;

            var cap = ResolveEchoCap(newest.Type);
            if (cap < 1)
                return;

            var eligible = EchoController.AllEchoes
                .Where(e => e != null && !e.ExcludedFromCap && e.Type == newest.Type)
                .OrderBy(e => e.SpawnTimestamp)
                .ToList();

            var overflow = eligible.Count - cap;
            for (var i = 0; i < overflow; i++)
            {
                var toCull = eligible[i];
                if (toCull == null)
                    continue;
                toCull.ForceExpireSoon();
            }
        }

        public static EchoController SpawnEcho(IEnumerable<Skill> skills, float duration,
            EchoType type = EchoType.All, bool excludeFromCap = false)
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

            echo.Init(skills, duration, type, excludeFromCap);
            EnforceTypeCap(echo);
            return echo;
        }

        public static EchoController SpawnEcho(Skill skill, float duration, EchoType type = EchoType.All, bool excludeFromCap = false)
        {
            return SpawnEcho(new List<Skill> { skill }, duration, type, excludeFromCap);
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
            IEnumerable<Skill> fallbackSkills = null, bool applyLifetimeUpgrade = false, int count = 1, bool excludeFromCap = false)
        {
            var duration = baseDuration;
            if (applyLifetimeUpgrade)
            {
                var echoStat = BaseStatService.GetStat("Echo Lifetime");
                if (echoStat != null)
                    duration += BaseStatService.GetTotalValue(echoStat);
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
                var h = SpawnEcho(skills, duration, type, excludeFromCap);
                if (h != null)
                    spawned.Add(h);
            }

            return spawned;
        }
    }
}

