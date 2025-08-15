using System.Collections.Generic;
using UnityEngine;
using Blindsided.Utilities.Pooling;
using TimelessEchoes.Audio;
using TimelessEchoes.Buffs;
using TimelessEchoes.Enemies;
using TimelessEchoes.Hero;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple projectile that seeks a target and applies damage on contact.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private static readonly HashSet<Projectile> active = new();

        /// <summary>
        /// All active projectiles in the scene.
        /// </summary>
        public static IEnumerable<Projectile> Active => active;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float hitDistance = 0.1f;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float effectDuration = 0.5f;

        private Transform target;
        private float damage;
        private float bonusDamage;
        private bool fromHero;
        private TimelessEchoes.Skills.Skill combatSkill;
        private bool isCritical;
        private int attackerLevel = -1;

        private IHasHealth targetHasHealth;
        private IDamageable targetDamageable;

        private void OnEnable()
        {
            active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        private GameObject effectPrefab;

        public void Init(Transform target, float damage,
            bool fromHero = false,
            GameObject hitEffect = null,
            TimelessEchoes.Skills.Skill combatSkill = null,
            float bonusDamage = 0f,
            bool isCritical = false,
            int attackerLevel = -1)
        {
            this.target = target;
            this.damage = damage;
            this.bonusDamage = bonusDamage;
            this.fromHero = fromHero;
            this.combatSkill = combatSkill;
            this.isCritical = isCritical;
            this.attackerLevel = attackerLevel;
            effectPrefab = hitEffect ?? hitEffectPrefab;
            transform.rotation = Quaternion.identity;

            targetHasHealth = target ? target.GetComponent<IHasHealth>() : null;
            targetDamageable = target ? target.GetComponent<IDamageable>() : null;
        }

        private void Update()
        {
            if (target == null)
            {
                PoolManager.Release(gameObject);
                return;
            }

            if (targetHasHealth != null && targetHasHealth.CurrentHealth <= 0f)
            {
                PoolManager.Release(gameObject);
                return;
            }

            Vector3 dir = target.position - transform.position;
            float distanceThisFrame = speed * Time.deltaTime;
            float mag = dir.magnitude;

            if (mag <= hitDistance)
            {
                float dmgAmount = damage;
                if (fromHero && combatSkill != null)
                {
                    var controller = TimelessEchoes.Skills.SkillController.Instance ??
                                     FindFirstObjectByType<TimelessEchoes.Skills.SkillController>();
                    if (controller != null && controller.RollForEffect(combatSkill, TimelessEchoes.Skills.MilestoneType.InstantKill))
                    {
                        var prefab = TimelessEchoes.GameManager.Instance != null ?
                                     TimelessEchoes.GameManager.Instance.ReaperPrefab : null;
                        var offset = TimelessEchoes.GameManager.Instance != null ?
                                     (Vector3?)TimelessEchoes.GameManager.Instance.ReaperSpawnOffset : null;
                        if (Enemies.ReaperManager.Spawn(prefab, target.gameObject, null, true, null, offset) != null)
                        {
                            var sfx2 = GetComponent<ProjectileHitSfx>();
                            sfx2?.PlayHit();
                            SpawnEffect();
                            PoolManager.Release(gameObject);
                            return;
                        }
                        if (targetHasHealth != null)
                            dmgAmount = Mathf.Max(dmgAmount, targetHasHealth.CurrentHealth);
                    }
                }

                float baseAmount = dmgAmount - bonusDamage;
                bool appliedCustom = false;
                if (!fromHero && attackerLevel >= 0)
                {
                    var heroHealth = target.GetComponent<TimelessEchoes.Hero.HeroHealth>();
                    if (heroHealth != null)
                    {
                        heroHealth.TakeDamageFromEnemy(baseAmount, attackerLevel, bonusDamage, isCritical);
                        appliedCustom = true;
                    }
                    else
                    {
                        var echoProxy = target.GetComponent<TimelessEchoes.Hero.EchoHealthProxy>();
                        if (echoProxy != null)
                        {
                            // Echo forwards to main hero at 50% effectiveness
                            TimelessEchoes.Hero.HeroHealth.Instance?.TakeDamageFromEnemy(baseAmount * 0.5f, attackerLevel, bonusDamage, isCritical);
                            appliedCustom = true;
                        }
                    }
                }

                if (!appliedCustom)
                    targetDamageable?.TakeDamage(baseAmount, bonusDamage, isCritical);
                if (fromHero)
                {
                    var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance ??
                                     FindFirstObjectByType<TimelessEchoes.Stats.GameplayStatTracker>();
                    tracker?.AddDamageDealt(dmgAmount);
                    var buffManager = TimelessEchoes.Buffs.BuffManager.Instance ??
                                      FindFirstObjectByType<TimelessEchoes.Buffs.BuffManager>();
                    var hero = TimelessEchoes.Hero.HeroController.Instance ??
                                FindFirstObjectByType<TimelessEchoes.Hero.HeroController>();
                    var heroHealth = hero != null ? hero.GetComponent<TimelessEchoes.Hero.HeroHealth>() : null;
                    if (buffManager != null && heroHealth != null)
                    {
                        float ls = buffManager.LifestealPercent;
                        if (ls > 0f)
                            heroHealth.Heal(dmgAmount * ls / 100f);
                    }
                }
                var sfx = GetComponent<ProjectileHitSfx>();
                sfx?.PlayHit();
                SpawnEffect();
                PoolManager.Release(gameObject);
                return;
            }

            if (mag > 1e-6f)
            {
                transform.position += dir * (distanceThisFrame / mag);
                // Rotate only around Z
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void SpawnEffect()
        {
            if (effectPrefab == null)
                return;

            var obj = PoolManager.Get(effectPrefab);
            obj.transform.position = transform.position;
            obj.transform.rotation = Quaternion.identity;
            var auto = obj.GetComponent<PoolAutoReturn>() ?? obj.AddComponent<PoolAutoReturn>();
            auto.ReturnAfter(effectDuration);
        }
    }
}
