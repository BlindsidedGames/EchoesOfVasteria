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
        /// <summary>
        ///     Fallback limit for each echo type when no explicit override is present.
        /// </summary>
        private const int DefaultEchoCap = 10;

        /// <summary>
        ///     Stores per-type overrides for echo population caps. Only populated when a system explicitly calls
        ///     <see cref="SetEchoCap" />.
        /// </summary>
        private static readonly Dictionary<EchoType, int> CustomEchoCaps = new();

        /// <summary>
        ///     Override the default population cap for a specific echo type. Passing a negative number clamps to zero.
        /// </summary>
        /// <param name="type">Echo category to configure.</param>
        /// <param name="cap">Maximum number of simultaneous echoes allowed.</param>
        public static void SetEchoCap(EchoType type, int cap)
        {
            CustomEchoCaps[type] = Mathf.Max(0, cap);
        }

        /// <summary>
        ///     Returns the active cap for the provided echo type (custom value if set, otherwise default).
        /// </summary>
        public static int GetEchoCap(EchoType type)
        {
            return ResolveEchoCap(type);
        }

        /// <summary>
        ///     Looks up the configured cap without applying any additional logic. Internal helper so we keep the public API
        ///     small while allowing tests to bypass the culling side-effects.
        /// </summary>
        private static int ResolveEchoCap(EchoType type)
        {
            return CustomEchoCaps.TryGetValue(type, out var cap) ? cap : DefaultEchoCap;
        }

        /// <summary>
        ///     Ensure the newest echo does not violate the configured cap. When we exceed the cap we expire the oldest
        ///     eligible echoes to make room for the new one.
        /// </summary>
        /// <param name="newest">Freshly spawned echo that triggered the cap check.</param>
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

        /// <summary>
        ///     Spawn a single echo using the hero's prefab reference. This method handles pooling, parenting, and
        ///     configuration so callers only need to provide the behavioural inputs.
        /// </summary>
        /// <param name="skills">Skills to assign to the echo. Null grants access to all non-combat tasks.</param>
        /// <param name="duration">Lifetime in seconds the echo should exist.</param>
        /// <param name="type">Behaviour archetype to apply.</param>
        /// <param name="excludeFromCap">When true the echo does not count towards the active population cap.</param>
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

        /// <summary>
        ///     Convenience overload for spawning an echo that focuses on a single skill.
        /// </summary>
        /// <param name="skill">Skill to enable on the spawned echo.</param>
        /// <param name="duration">Lifetime in seconds the echo should exist.</param>
        /// <param name="type">Behaviour archetype to apply.</param>
        /// <param name="excludeFromCap">When true the echo does not count towards the active population cap.</param>
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
        /// <param name="excludeFromCap">When true, spawned echoes do not count towards population caps.</param>
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

