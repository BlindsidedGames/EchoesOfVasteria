using System;
using System.Collections.Generic;
using Pathfinding;
using Pathfinding.RVO;
using TimelessEchoes.Hero;
using TimelessEchoes.Skills;
using TimelessEchoes.Stats;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TMPro;
using UnityEngine;
using System.Linq;
using static TimelessEchoes.TELogger;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Enemies
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyData stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private float targetUpdateInterval = 1f;
        [SerializeField] private TMP_Text levelText;

        private ResourceManager resourceManager;

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
        private Transform wanderTarget;
        private float nextWanderTime;
        private LayerMask blockingMask;

        [SerializeField] private Skill combatSkill;

        public bool IsEngaged => setter != null && setter.target == hero;
        public EnemyData Stats => stats;
        public int Level => level;

        public float GetDefense()
        {
            return stats != null ? stats.GetDefenseForLevel(level) : 0f;
        }

        public static event Action<Enemy> OnEngage;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();
            spawnPos = transform.position;
            float spawnOffset = GameManager.CurrentGenerationConfig != null
                ? GameManager.CurrentGenerationConfig.taskGeneratorSettings.enemySpawnXOffset
                : 0f;
            level = stats != null
                ? stats.GetLevel(spawnPos.x - spawnOffset - stats.minX)
                : 1;

            var controller = GetComponentInParent<TaskController>();
            if (controller != null)
                hero = controller.hero != null ? controller.hero.transform : null;
            wanderTarget = new GameObject("WanderTarget").transform;
            wanderTarget.hideFlags = HideFlags.HideInHierarchy;
            wanderTarget.position = transform.position;
            blockingMask = LayerMask.GetMask("Blocking");
            if (stats != null)
            {
                ai.maxSpeed = stats.moveSpeed;
                health.Init(stats.GetMaxHealthForLevel(level));
            }

            if (levelText != null)
                levelText.text = $"Lvl {level}";

            startTarget = setter.target;
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            if (health != null)
                health.OnDeath += OnDeath;
            nextWanderTime = Time.time;
            nextTargetUpdate = Time.time;
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

            nextWanderTime = Time.time;
            nextTargetUpdate = Time.time;
            Wander();

            // Offset the animator's starting time so enemies don't animate
            // in perfect sync when spawned simultaneously.
            if (animator != null)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }
        }

        private void OnDisable()
        {
            EnemyActivator.Instance?.Unregister(this);
            OnEngage -= HandleAllyEngaged;
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

            animator.SetFloat("MoveX", lastMoveDir.x);
            animator.SetFloat("MoveY", lastMoveDir.y);
            animator.SetFloat("MoveMagnitude", vel.magnitude);

            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;
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
                    animator.Play("Attack");
                    FireProjectile();
                }
            }
            else if (setter.target == wanderTarget)
            {
                Wander();
            }
        }

        private Transform ChooseTarget()
        {
            Transform chosen = null;
            var bestDist = float.PositiveInfinity;

            // check hero
            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                var heroCtrl = hero.GetComponent<HeroController>();
                if (heroCtrl == null || heroCtrl.AllowAttacks)
                {
                    var dist = Vector2.Distance(transform.position, hero.position);
                    if (dist <= stats.visionRange && dist < bestDist)
                    {
                        chosen = hero;
                        bestDist = dist;
                    }
                }
            }

            // check combat-enabled echoes
            foreach (var echo in EchoController.CombatEchoes)
            {
                if (echo == null || !echo.isActiveAndEnabled)
                    continue;
                var ecHero = echo.GetComponent<HeroController>();
                if (ecHero == null || !ecHero.AllowAttacks)
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
            if (setter.target != wanderTarget)
                setter.target = wanderTarget;
            if (!ai.reachedEndOfPath) return;
            if (Time.time < nextWanderTime) return;

            const int maxAttempts = 5;
            var wander = (Vector2)transform.position;
            for (var i = 0; i < maxAttempts; i++)
            {
                var candidate = (Vector2)spawnPos + Random.insideUnitCircle * stats.wanderDistance;
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
            var projObj = Instantiate(stats.projectilePrefab, origin.position, Quaternion.identity);
            var proj = projObj.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(setter.target, stats.GetDamageForLevel(level));
        }

        private void OnDeath()
        {
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    Log("ResourceManager missing", TELogCategory.Resource, this);
            }

            if (resourceManager == null) return;

            Log($"Enemy {name} died", TELogCategory.Combat, this);

            var skillController = SkillController.Instance;
            var mult = 1;
            var gainMult = 1f;
            var combatSkill = skillController != null ? skillController.CombatSkill : null;
            if (skillController != null && combatSkill != null)
            {
                mult = skillController.GetEffectMultiplier(combatSkill, MilestoneType.DoubleResources);
                gainMult = skillController.GetResourceGainMultiplier();
            }

            foreach (var drop in stats.resourceDrops)
            {
                if (drop.resource == null) continue;
                if (Random.value > drop.dropChance) continue;

                var min = drop.dropRange.x;
                var max = drop.dropRange.y;
                if (max < min) max = min;
                var t = Random.value;
                t *= t; // bias towards lower values
                var count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
                if (count > 0)
                {
                    double final = count * mult * gainMult;
                    resourceManager.Add(drop.resource, final);
                    Log($"Dropped {final} {drop.resource.name}", TELogCategory.Resource, this);
                }
            }

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

            GrantCombatExperience();
        }

        private void GrantCombatExperience()
        {
            if (stats == null) return;
            var controller = SkillController.Instance;
            var skill = controller != null ? controller.CombatSkill : combatSkill;
            if (controller == null && skill == null)
            {
                Log("Missing SkillController and combat skill", TELogCategory.Combat, this);
                return;
            }

            if (controller != null && skill == null)
            {
                Log("Combat skill not set on SkillController", TELogCategory.Combat, this);
                return;
            }

            if (skill != null)
            {
                controller?.AddExperience(skill, stats.experience);
                var progress = controller?.GetProgress(skill);
                if (progress != null)
                    foreach (var id in progress.Milestones)
                    {
                        var ms = skill.milestones.Find(m => m.bonusID == id);
                        if (ms != null && ms.type == MilestoneType.SpawnEcho && Random.value <= ms.chance)
                        {
                            EchoManager.SpawnEchoes(ms.echoSpawnConfig, ms.echoDuration,
                                new List<Skill> { skill }, true);
                        }
                    }
            }
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= OnDeath;
            OnEngage -= HandleAllyEngaged;
            if (wanderTarget != null)
                Destroy(wanderTarget.gameObject);
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
            if (other != this && other != null && hero != null && other.IsEngaged)
            {
                var dist = Vector2.Distance(transform.position, other.transform.position);
                if (dist <= stats.assistRange)
                    setter.target = hero;
            }
        }
    }
}