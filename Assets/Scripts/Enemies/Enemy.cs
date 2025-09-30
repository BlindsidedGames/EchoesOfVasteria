using System;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.RVO;
using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Buffs;
using Blindsided.SaveData;
using TMPro;
using UnityEngine;
using System.Linq;
using static TimelessEchoes.TELogger;
using Random = UnityEngine.Random;
using static Blindsided.Oracle;
using Blindsided.Utilities.Pooling;
using UnityEngine.U2D.Animation;

namespace TimelessEchoes.Enemies
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Health))]
    /// <summary>
    /// Runtime enemy AI: computes level from world X, idles/wanders when idle,
    /// periodically selects a target (hero or eligible echo), moves using A* and
    /// attacks with projectiles when in range. Handles resource drops and UI on death.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        private const float EchoXpFraction = 0.25f;
        [SerializeField] private EnemyData stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool invertXFlip = false;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private float targetUpdateInterval = 1f;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private bool randomizeSpriteLibraryOnSpawn = false;
        [SerializeField] private List<SpriteLibraryAsset> spriteLibraryVariants = new List<SpriteLibraryAsset>();
        [SerializeField, Tooltip("Weight of the regular (uniform) variants section when choosing between sections. If the weighted section has no valid entries, this is ignored.")]
        private float regularSectionWeight = 1f;

        [Serializable]
        private class WeightedSpriteLibraryEntry
        {
            public SpriteLibraryAsset asset;
            public float weight = 1f;
        }

        [SerializeField, Tooltip("Per-entry weighted sprite library variants. If empty or all invalid, the regular section is used (uniform selection).")]
        private List<WeightedSpriteLibraryEntry> weightedSpriteLibraryVariants = new List<WeightedSpriteLibraryEntry>();

        [SerializeField] private SpriteLibrary targetSpriteLibrary;

        private ResourceManager resourceManager;
        private bool lastDamageFromEcho;

        private AIPath ai;
        private Health health;
        private float nextAttack;
        private float nextTargetUpdate;
        private AIDestinationSetter setter;
        private Vector2 lastMoveDir = Vector2.down;
        private bool logicActive = true;
        private Vector3 spawnPos;
        private int level = 1;
        private Transform startTarget;
        private Transform hero;
        private HeroController heroController;
        private Transform wanderTarget;
        private float nextWanderTime;
        private LayerMask blockingMask;

        [SerializeField] private Skill combatSkill;

        public bool IsEngaged => setter != null && setter.target != null && setter.target != wanderTarget;
        public EnemyData Stats => stats;
        public int Level => level;

        public float GetDefense()
        {
            return stats != null ? stats.GetDefenseForLevel(level) : 0f;
        }

        /// <summary>
        /// Fired when an enemy switches to an engagement target (hero or echo).
        /// Useful for systems that react to aggro (e.g., swarms, UI cues).
        /// </summary>
        public static event Action<Enemy> OnEngage;

        private void Awake()
        {
            SetupInitialInstance();
        }

        /// <summary>
        /// One-time setup for this instance. Cache components, create wander target,
        /// subscribe events, and apply stat-derived static settings. Do not perform
        /// any per-spawn logic here (position/level/hero acquisition).
        /// </summary>
        private void SetupInitialInstance()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();

            // Do not parent a wander target under the enemy. We acquire a pooled
            // target object when needed (InitForSpawn/OnEnable) and release it when
            // the enemy is returned to its pool.

            blockingMask = LayerMask.GetMask("Blocking");

            if (stats != null && ai != null)
            {
                ai.maxSpeed = stats.moveSpeed;
                ai.endReachedDistance = stats.attackRange;
                ai.slowdownDistance = Mathf.Max(ai.slowdownDistance, ai.endReachedDistance);
            }

            startTarget = setter != null ? setter.target : null;
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            if (health != null)
                health.OnDeath += OnDeath;

            // Seed timers to now; per-spawn will reset them properly
            nextWanderTime = Time.time;
            nextTargetUpdate = Time.time;
            lastDamageFromEcho = false;
        }

        private void Update()
        {
            UpdateAnimation();
            if (logicActive)
                UpdateBehavior();
        }

        private void OnEnable()
        {
            EnemyActivator.Instance?.Register(this);
            OnEngage += HandleAllyEngaged;

            // Offset the animator's starting time so enemies don't animate
            // in perfect sync when spawned simultaneously.
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }

            // Ensure we have a wander target available early to avoid any null
            // access before InitForSpawn is invoked (e.g., scene-placed enemies).
            if (wanderTarget == null)
            {
                AcquireWanderTarget();
                if (setter != null && wanderTarget != null)
                    setter.target = wanderTarget;
            }
        }

        private void OnDisable()
        {
            EnemyActivator.Instance?.Unregister(this);
            OnEngage -= HandleAllyEngaged;

            // Release pooled wander destination when this enemy is deactivated.
            ReleaseWanderTarget();
            if (setter != null && setter.target == wanderTarget)
                setter.target = null;
        }

        private void ApplyRandomSpriteLibrary()
        {
            if (!randomizeSpriteLibraryOnSpawn) return;

            if (targetSpriteLibrary == null)
                targetSpriteLibrary = GetComponentInChildren<SpriteLibrary>(true);
            if (targetSpriteLibrary == null) return;

            // Build valid lists
            var validRegular = spriteLibraryVariants != null
                ? spriteLibraryVariants.Where(a => a != null).ToList()
                : new List<SpriteLibraryAsset>();

            var validWeighted = weightedSpriteLibraryVariants != null
                ? weightedSpriteLibraryVariants.Where(e => e != null && e.asset != null && e.weight > 0f).ToList()
                : new List<WeightedSpriteLibraryEntry>();

            // If no valid weighted entries, ignore weights altogether and use existing uniform behavior
            if (validWeighted.Count == 0)
            {
                if (validRegular.Count == 0) return;
                var chosenUniform = validRegular[Random.Range(0, validRegular.Count)];
                if (chosenUniform == null) return;

                if (targetSpriteLibrary.spriteLibraryAsset != chosenUniform)
                    targetSpriteLibrary.spriteLibraryAsset = chosenUniform;

                var resolversUniform = GetComponentsInChildren<SpriteResolver>(true);
                for (int i = 0; i < resolversUniform.Length; i++)
                    resolversUniform[i].ResolveSpriteToSpriteRenderer();
                return;
            }

            // Weighted section exists: choose between sections based on weights
            float sectionRegular = Mathf.Max(0f, regularSectionWeight);
            float sectionWeighted = 0f;
            for (int i = 0; i < validWeighted.Count; i++)
                sectionWeighted += Mathf.Max(0f, validWeighted[i].weight);

            // If both sections are invalid, bail
            if (sectionWeighted <= 0f && validRegular.Count == 0)
                return;

            SpriteLibraryAsset chosen = null;

            // If no regular entries, force weighted; if section weights allow, roll between them
            if (validRegular.Count == 0)
            {
                // pick weighted entry by weight
                float rollW = Random.value * Mathf.Max(sectionWeighted, 0.0001f);
                float acc = 0f;
                for (int i = 0; i < validWeighted.Count; i++)
                {
                    acc += Mathf.Max(0f, validWeighted[i].weight);
                    if (rollW <= acc)
                    {
                        chosen = validWeighted[i].asset;
                        break;
                    }
                }
                chosen ??= validWeighted[validWeighted.Count - 1].asset;
            }
            else
            {
                // Choose section by weights (regularSectionWeight vs sum of weighted entries)
                float totalSection = sectionRegular + sectionWeighted;
                float roll = Random.value * Mathf.Max(totalSection, 0.0001f);
                bool chooseRegular = roll <= sectionRegular;

                if (chooseRegular)
                {
                    chosen = validRegular[Random.Range(0, validRegular.Count)];
                }
                else
                {
                    float rollW = Random.value * Mathf.Max(sectionWeighted, 0.0001f);
                    float acc = 0f;
                    for (int i = 0; i < validWeighted.Count; i++)
                    {
                        acc += Mathf.Max(0f, validWeighted[i].weight);
                        if (rollW <= acc)
                        {
                            chosen = validWeighted[i].asset;
                            break;
                        }
                    }
                    if (chosen == null)
                        chosen = validWeighted[validWeighted.Count - 1].asset;
                }
            }

            if (chosen == null) return;

            if (targetSpriteLibrary.spriteLibraryAsset != chosen)
                targetSpriteLibrary.spriteLibraryAsset = chosen;

            var resolvers = GetComponentsInChildren<SpriteResolver>(true);
            for (int i = 0; i < resolvers.Length; i++)
                resolvers[i].ResolveSpriteToSpriteRenderer();
        }


        private void UpdateAnimation()
        {
            Vector2 vel = ai.desiredVelocity;
            var dir = vel;

            if (dir.sqrMagnitude < 0.0001f && setter != null && setter.target != null)
                dir = setter.target.position - transform.position;
            if (fourDirectional)
            {
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    dir.y = 0f;
                else
                    dir.x = 0f;
            }
            else
            {
                dir.y = 0f;
            }

            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir;

            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
                animator.SetFloat("MoveMagnitude", vel.magnitude);
            }

            if (spriteRenderer != null)
                spriteRenderer.flipX = invertXFlip ? lastMoveDir.x > 0f : lastMoveDir.x < 0f;
        }

        private void UpdateBehavior()
        {
            if (stats == null)
                return;

            if (Time.time >= nextTargetUpdate)
            {
                var chosen = ChooseTarget();
                if (chosen != null)
                {
                    setter.target = chosen;
                    OnEngage?.Invoke(this);
                }
                else
                {
                    if (setter.target != wanderTarget)
                        setter.target = wanderTarget;
                    Wander();
                }

                nextTargetUpdate = Time.time + Mathf.Max(0.1f, targetUpdateInterval);
            }

            if (setter.target != null && setter.target != wanderTarget)
            {
                var dist = Vector2.Distance(transform.position, setter.target.position);
                if (dist <= stats.attackRange && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    FaceTarget();
                    if (animator != null)
                        animator.Play("Attack");
                    FireProjectile();
                }
            }
            else if (setter.target == wanderTarget)
            {
                Wander();
            }
        }

        private void FaceTarget()
        {
            if (setter == null || setter.target == null)
                return;

            var dir = setter.target.position - transform.position;
            if (fourDirectional)
            {
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    dir.y = 0f;
                else
                    dir.x = 0f;
            }
            else
            {
                dir.y = 0f;
            }

            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir;

            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
                animator.SetFloat("MoveMagnitude", 0f);
            }

            if (spriteRenderer != null)
                spriteRenderer.flipX = invertXFlip ? lastMoveDir.x > 0f : lastMoveDir.x < 0f;
        }

        private Transform ChooseTarget()
        {
            Transform chosen = null;
            var bestDist = float.PositiveInfinity;

            // check hero (use cached reference; no per-tick GetComponent)
            if (heroController != null && heroController.isActiveAndEnabled && heroController.AllowAttacks)
            {
                var dist = Vector2.Distance(transform.position, heroController.transform.position);
                if (dist <= stats.visionRange && dist < bestDist)
                {
                    chosen = heroController.transform;
                    bestDist = dist;
                }
            }

            // check combat-enabled echoes
            foreach (var echo in EchoController.CombatEchoes)
            {
                if (echo == null || !echo.isActiveAndEnabled)
                    continue;
                // Avoid per-interval GetComponent by using EchoController API
                if (!echo.AllowAttacks)
                    continue;
                var dist = Vector2.Distance(transform.position, echo.transform.position);
                if (dist <= stats.visionRange && dist < bestDist)
                {
                    chosen = echo.transform;
                    bestDist = dist;
                }
            }

            return chosen;
        }

        private void Wander()
        {
            // Guard against missing references during initialization/pooling edge cases.
            if (stats == null || ai == null || setter == null)
                return;
            if (wanderTarget == null)
            {
                AcquireWanderTarget();
                if (wanderTarget == null)
                    return;
            }
            if (setter.target != wanderTarget)
                setter.target = wanderTarget;
            if (!ai.reachedEndOfPath) return;
            if (Time.time < nextWanderTime) return;

            const int maxAttempts = 5;
            var wander = (Vector2)transform.position;
            float maxDist = Mathf.Max(0f, stats.wanderDistance);
            float minDist = Mathf.Clamp(stats.minWanderDistance, 0f, maxDist);
            for (var i = 0; i < maxAttempts; i++)
            {
                // Pick a direction and a radius within [minDist, maxDist]
                var dir = Random.insideUnitCircle;
                if (dir.sqrMagnitude < 1e-5f)
                    dir = Vector2.right; // fallback direction
                dir.Normalize();
                float radius = (minDist >= maxDist)
                    ? maxDist
                    : Random.Range(minDist, maxDist);
                var candidate = (Vector2)spawnPos + dir * radius;
                if (Physics2D.OverlapCircle(candidate, 0.2f, blockingMask) == null)
                {
                    wander = candidate;
                    break;
                }
            }

            wanderTarget.position = wander;
            setter.target = wanderTarget;
            nextWanderTime = Time.time + Random.Range(1f, 3f);
        }

        private void FireProjectile()
        {
            if (stats.projectilePrefab == null || setter.target == null) return;
            var origin = projectileOrigin ? projectileOrigin : transform;
            var projObj = PoolManager.Get(stats.projectilePrefab);
            projObj.transform.position = origin.position;
            projObj.transform.rotation = Quaternion.identity;
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(setter.target, stats.GetDamageForLevel(level), false, null, null, 0f, false, level);
        }

        public void RegisterDamageSource(bool fromEcho)
        {
            lastDamageFromEcho = fromEcho;
        }

        private void OnDeath()
        {
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    Log("ResourceManager missing", TELogCategory.Resource, this);
            }

            if (resourceManager == null)
                return;

            Log($"Enemy {name} died", TELogCategory.Combat, this);

            var skillController = SkillController.Instance;
            var xpSkill = skillController != null ? skillController.CombatSkill : combatSkill;
            var xpAwarded = GrantCombatExperience(skillController, xpSkill, lastDamageFromEcho);

            var mult = 1;
            var gainMult = 1f;
            var controllerCombatSkill = skillController != null ? skillController.CombatSkill : null;
            if (skillController != null && controllerCombatSkill != null)
            {
                mult = skillController.GetStackingMultiplier(controllerCombatSkill, MilestoneProcType.DoubleResources);
                gainMult = skillController.GetResourceGainMultiplier();
            }

            float milestoneMult = skillController != null ? skillController.GetResourceBonusMultiplier(controllerCombatSkill) : 1f;

            var dropTotals = new Dictionary<Resource, double>();
            var dropOrder = new List<Resource>();
            var worldX = transform.position.x;

            var results = DropResolver.RollDrops(stats.resourceDrops, stats.additionalLootChances, worldX);
            foreach (var res in results)
            {
                double final = res.count * mult * gainMult * milestoneMult;
                var buff = BuffManager.Instance;
                if (buff != null)
                    final *= buff.ResourceGainMultiplier;
                resourceManager.Add(res.resource, final);
                Log($"Dropped {final} {res.resource.name}", TELogCategory.Resource, this);
                if (dropTotals.ContainsKey(res.resource))
                    dropTotals[res.resource] += final;
                else
                {
                    dropTotals[res.resource] = final;
                    dropOrder.Add(res.resource);
                }
            }

            if (dropTotals.Count > 0)
            {
                var parts = new List<string>();
                foreach (var res in dropOrder)
                    parts.Add($"{Blindsided.Utilities.TextStrings.ResourceIcon(res.resourceID)}{Mathf.FloorToInt((float)dropTotals[res])}");
                var lines = new List<string>();
                var line = string.Empty;
                for (var i = 0; i < parts.Count; i++)
                {
                    if (i % 3 == 0)
                    {
                        if (line.Length > 0)
                        {
                            lines.Add(line);
                            line = string.Empty;
                        }
                        line = parts[i];
                    }
                    else
                    {
                        line += ", " + parts[i];
                    }
                }
                if (line.Length > 0)
                    lines.Add(line);
                const float floatingTextFontSize = 8f;

                if (lines.Count > 0)
                {
                    var xpLine = SkillIconLookup.FormatXpLine(xpSkill, xpAwarded, floatingTextFontSize);
                    if (!string.IsNullOrEmpty(xpLine))
                    {
                        int lastIndex = lines.Count - 1;
                        lines[lastIndex] = $"{lines[lastIndex]} {xpLine}";
                    }

                    if (Blindsided.SaveData.StaticReferences.ItemDropFloatingText)
                        FloatingText.SpawnResourceText(string.Join("\n", lines), transform.position + Vector3.up,
                            FloatingText.DefaultColor, floatingTextFontSize, null,
                            Blindsided.SaveData.StaticReferences.DropFloatingTextDuration);
                }
            }

            lastDamageFromEcho = false;
            var tracker = EnemyKillTracker.Instance;
            if (tracker == null)
                Log("EnemyKillTracker missing", TELogCategory.Combat, this);
            else
                tracker.RegisterKill(stats);
            var statsTracker = GameplayStatTracker.Instance;
            if (statsTracker == null)
                Log("GameplayStatTracker missing", TELogCategory.Combat, this);
            else
                statsTracker.AddKill(stats);
        }

        private float GrantCombatExperience(SkillController controller, Skill skill, bool fromEcho)
        {
            if (stats == null)
                return 0f;

            if (controller == null && skill == null)
            {
                Log("Missing SkillController and combat skill", TELogCategory.Combat, this);
                return 0f;
            }

            if (controller != null && skill == null)
            {
                Log("Combat skill not set on SkillController", TELogCategory.Combat, this);
                return 0f;
            }

            if (skill == null)
                return 0f;

            float amount = stats.experience;
            if (amount <= 0f)
                return 0f;

            if (fromEcho)
                amount *= EchoXpFraction;

            controller?.AddExperience(skill, amount);

            if (controller != null && !fromEcho)
            {
                // Only hero-triggered kills can spawn additional echoes.
                var spawnEntries = controller.GetSpawnEntries(skill);
                if (spawnEntries != null)
                {
                    foreach (var entry in spawnEntries)
                    {
                        if (Random.value <= entry.Chance)
                        {
                            var fallback = entry.UseAssociatedSkillFallback && skill != null
                                ? new List<Skill> { skill }
                                : null;
                            EchoManager.SpawnEchoes(entry.Config, entry.Duration, fallback, true, entry.Count);
                        }
                    }
                }
            }

            return amount;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
            OnEngage -= HandleAllyEngaged;
            // Safety: release pooled target if still held when object is destroyed.
            ReleaseWanderTarget();
        }

        /// <summary>
        /// Initialize stats and health for a fresh spawn at the current position.
        /// Call this right after placing the enemy from a pool.
        /// </summary>
        public void InitForSpawn()
        {
            lastDamageFromEcho = false;
            // Acquire primary hero from static instance to avoid lookups/parent dependency
            heroController = HeroController.Instance;
            if (heroController == null)
            {
                // Fallback: in unusual scenes where Instance is not yet set, try parent TaskController
                var controller = GetComponentInParent<TaskController>();
                heroController = controller != null ? controller.hero : null;
            }
            hero = heroController != null ? heroController.transform : null;

            spawnPos = transform.position;
            float spawnOffset = GameManager.CurrentGenerationConfig != null
                ? GameManager.CurrentGenerationConfig.taskGeneratorSettings.enemySpawnXOffset
                : 0f;
            level = stats != null
                ? stats.GetLevel(spawnPos.x - spawnOffset - stats.minX)
                : 1;

            var gmInstance = TimelessEchoes.GameManager.Instance;
            if (gmInstance != null && gmInstance.IsKillScalingMode)
            {
                var trackerInstance = GameplayStatTracker.Instance;
                var killsPerLevel = gmInstance.CurrentKillsPerLevel;
                if (trackerInstance != null && killsPerLevel > 0)
                {
                    var extraLevelTiers = trackerInstance.CurrentRunKills / killsPerLevel;
                    if (extraLevelTiers > 0)
                    {
                        var levelsPerTier = Mathf.Max(1, gmInstance.KillLevelIncreasePerTier);
                        level += extraLevelTiers * levelsPerTier;
                    }
                }
            }
            if (stats != null)
            {
                if (ai != null)
                {
                    // Ensure core behaviours are enabled when spawned from pool.
                    ai.enabled = true;
                    ai.maxSpeed = stats.moveSpeed;
                    ai.endReachedDistance = stats.attackRange;
                    ai.slowdownDistance = Mathf.Max(ai.slowdownDistance, ai.endReachedDistance);
                    // Reset any previous path and snap to the new spawn position
                    ai.Teleport(transform.position);
                }

                if (setter != null)
                    setter.enabled = true;

                // Make sure the enemy logic is active again after pooling.
                logicActive = true;

                if (health != null)
                    health.Init(stats.GetMaxHealthForLevel(level));
            }

            // Reset wandering target and disengage any previous target assignment
            if (wanderTarget == null)
                AcquireWanderTarget();
            if (wanderTarget != null)
                wanderTarget.position = transform.position;
            if (setter != null)
                setter.target = wanderTarget;

            // Reset timers and attack cadence on fresh spawn
            nextWanderTime = Time.time;
            nextTargetUpdate = Time.time;
            nextAttack = 0f;

            // Visual randomization and UI refresh for pooled reuse
            ApplyRandomSpriteLibrary();
            UpdateDisplayNameAndLevelUI();
        }

        private void AcquireWanderTarget()
        {
            // Get a pooled GameObject to act as the enemy's wander destination.
            // Keep it outside the enemy hierarchy so movement doesn't drag it.
            var go = PoolManager.GetNamed("EnemyWanderTarget");
            if (go == null)
                return;

            go.hideFlags = HideFlags.HideInHierarchy;
            var t = go.transform;
            t.SetParent(null, false);
            t.position = transform.position;
            wanderTarget = t;
        }

        private void ReleaseWanderTarget()
        {
            if (wanderTarget == null)
                return;
            PoolManager.Release(wanderTarget.gameObject);
            wanderTarget = null;
        }

        public void SetActiveState(bool active)
        {
            if (ai != null) ai.enabled = active;
            if (setter != null) setter.enabled = active;
            logicActive = active;

            if (!active && animator != null)
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", 0f);
                animator.SetFloat("MoveMagnitude", 0f);
            }
        }

        private void HandleAllyEngaged(Enemy other)
        {
            if (other == null || other == this || setter == null || stats == null)
                return;

            if (!other.IsEngaged)
                return;

            var dist = Vector2.Distance(transform.position, other.transform.position);
            if (dist > stats.assistRange)
                return;

            // Assist the ally's current target (hero or echo), not just the hero
            var allyTarget = other.setter != null ? other.setter.target : null;
            if (allyTarget != null && allyTarget != wanderTarget)
                setter.target = allyTarget;
        }

        private void UpdateDisplayNameAndLevelUI()
        {
            var displayName = stats != null ? stats.enemyName : null;
            displayName = EnemyNameProvider.GetName(displayName);
            if (!string.IsNullOrEmpty(displayName))
                gameObject.name = displayName;

            if (levelText != null)
            {
                if (!string.IsNullOrEmpty(displayName))
                    levelText.text = $"{displayName} Lvl {level}";
                else
                    levelText.text = $"Lvl {level}";
            }
        }

    }
}
