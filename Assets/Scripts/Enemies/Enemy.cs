using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Stats;
using TimelessEchoes.Skills;
using System.Collections.Generic;
using TimelessEchoes.Hero;
using System.Collections.Generic;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.Enemies
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(RVOController))]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyStats stats;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool fourDirectional = true;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private ResourceManager resourceManager;

        private AIPath ai;
        private Health health;
        private float nextAttack;
        private AIDestinationSetter setter;
        private bool logicActive = true;
        private Vector3 spawnPos;
        private Transform startTarget;
        private Transform hero;
        private Transform wanderTarget;
        private float nextWanderTime;
        private LayerMask blockingMask;

        [SerializeField] private TimelessEchoes.Skills.Skill combatSkill;

        public bool IsEngaged => setter != null && setter.target == hero;
        public EnemyStats Stats => stats;

        public static event System.Action<Enemy> OnEngage;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            health = GetComponent<Health>();
            spawnPos = transform.position;

            var controller = GetComponentInParent<TimelessEchoes.Tasks.TaskController>();
            if (controller != null)
                hero = controller.hero != null ? controller.hero.transform : null;
            wanderTarget = new GameObject("WanderTarget").transform;
            wanderTarget.hideFlags = HideFlags.HideInHierarchy;
            wanderTarget.position = transform.position;
            blockingMask = LayerMask.GetMask("Blocking");
            if (stats != null)
            {
                ai.maxSpeed = stats.moveSpeed;
                health.Init(stats.maxHealth);
            }

            startTarget = setter.target;
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            if (health != null)
                health.OnDeath += OnDeath;
            nextWanderTime = Time.time;
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

            animator.SetFloat("MoveX", dir.x);
            animator.SetFloat("MoveY", dir.y);
            animator.SetFloat("MoveMagnitude", vel.magnitude);
            if (spriteRenderer != null)
                spriteRenderer.flipX = vel.x < 0f;
        }

        private void UpdateBehavior()
        {
            if (stats == null)
                return;

            bool heroInVision = false;
            float heroDistance = float.PositiveInfinity;
            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                heroDistance = Vector2.Distance(transform.position, hero.position);
                if (heroDistance <= stats.visionRange)
                {
                    heroInVision = true;
                    setter.target = hero;
                    OnEngage?.Invoke(this);
                }
            }

            if (heroInVision)
            {
                if (heroDistance <= stats.attackRange && Time.time >= nextAttack)
                {
                    nextAttack = Time.time + 1f / Mathf.Max(stats.attackSpeed, 0.01f);
                    animator.Play("Attack");
                    FireProjectile();
                }
            }
            else
            {
                if (setter.target == hero)
                    setter.target = wanderTarget;
                Wander();
            }
        }

        private void Wander()
        {
            if (setter.target != wanderTarget)
                setter.target = wanderTarget;
            if (!ai.reachedEndOfPath) return;
            if (Time.time < nextWanderTime) return;

            const int maxAttempts = 5;
            Vector2 wander = (Vector2)transform.position;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 candidate = (Vector2)spawnPos + Random.insideUnitCircle * stats.wanderDistance;
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
                proj.Init(setter.target, stats.damage, false);
        }

        private void OnDeath()
        {
            if (resourceManager == null)
            {
                resourceManager = ResourceManager.Instance;
                if (resourceManager == null)
                    TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            }
            if (resourceManager == null) return;

            TELogger.Log($"Enemy {name} died", TELogCategory.Combat, this);

            var skillController = TimelessEchoes.Skills.SkillController.Instance;
            int mult = 1;
            float gainMult = 1f;
            var combatSkill = skillController != null ? skillController.CombatSkill : null;
            if (skillController != null && combatSkill != null)
            {
                mult = skillController.GetEffectMultiplier(combatSkill, TimelessEchoes.Skills.MilestoneType.DoubleResources);
                gainMult = skillController.GetResourceGainMultiplier();
            }

            foreach (var drop in resourceDrops)
            {
                if (drop.resource == null) continue;
                if (Random.value > drop.dropChance) continue;

                int min = drop.dropRange.x;
                int max = drop.dropRange.y;
                if (max < min) max = min;
                float t = Random.value;
                t *= t; // bias towards lower values
                int count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
                if (count > 0)
                {
                    double final = count * mult * gainMult;
                    resourceManager.Add(drop.resource, final);
                    TELogger.Log($"Dropped {final} {drop.resource.name}", TELogCategory.Resource, this);
                }
            }

            var tracker = EnemyKillTracker.Instance;
            if (tracker == null)
                TELogger.Log("EnemyKillTracker missing", TELogCategory.Combat, this);
            else
                tracker.RegisterKill(stats);
            var statsTracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            if (statsTracker == null)
                TELogger.Log("GameplayStatTracker missing", TELogCategory.Combat, this);
            else
                statsTracker.AddKill(stats);

            GrantCombatExperience();
        }

        private void GrantCombatExperience()
        {
            if (stats == null) return;
            var controller = TimelessEchoes.Skills.SkillController.Instance;
            var skill = controller != null ? controller.CombatSkill : combatSkill;
            if (controller == null && skill == null)
            {
                TELogger.Log("Missing SkillController and combat skill", TELogCategory.Combat, this);
                return;
            }
            if (controller != null && skill == null)
            {
                TELogger.Log("Combat skill not set on SkillController", TELogCategory.Combat, this);
                return;
            }
            if (skill != null)
            {
                controller?.AddExperience(skill, stats.experience);
                var progress = controller?.GetProgress(skill);
                if (progress != null)
                {
                    foreach (var id in progress.Milestones)
                    {
                        var ms = skill.milestones.Find(m => m.bonusID == id);
                        if (ms != null && ms.type == TimelessEchoes.Skills.MilestoneType.SpawnEcho && UnityEngine.Random.value <= ms.chance)
                        {
                            var config = ms.echoSpawnConfig;
                            int count = config != null ? Mathf.Max(1, config.echoCount) : 1;
                            var skills = config != null && config.capableSkills != null && config.capableSkills.Count > 0
                                ? config.capableSkills
                                : new System.Collections.Generic.List<Skill> { skill };
                            bool disable = config != null && config.disableSkills;
                            for (int c = 0; c < count; c++)
                            {
                                var target = skills[Mathf.Min(c, skills.Count - 1)];
                                bool combat = config != null && config.combatEnabled && controller.CombatSkill == target;
                                TimelessEchoes.Hero.EchoManager.SpawnEcho(new System.Collections.Generic.List<Skill> { target }, ms.echoDuration, combat, disable);
                            }
                        }
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
            if (other != this && other != null && hero != null)
            {
                float dist = Vector2.Distance(transform.position, other.transform.position);
                if (dist <= stats.assistRange)
                    setter.target = hero;
            }
        }
    }
}