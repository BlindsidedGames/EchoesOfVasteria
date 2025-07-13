using System;
using UnityEngine;
using TimelessEchoes.Buffs;
using TimelessEchoes.Hero;
using TimelessEchoes.Stats;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    /// Handles spawning and timed kills for reaper animations.
    /// </summary>
    public class ReaperManager : MonoBehaviour
    {
        private GameObject target;
        private Action onKill;
        private bool fromHero;

        [SerializeField] private float moveDistance = 0.5f;
        [SerializeField] private float moveSpeed = 1f;

        private Vector3 startPosition;
        private float moved;

        private void OnEnable()
        {
            startPosition = transform.position;
            moved = 0f;
        }

        private void Update()
        {
            if (moved < moveDistance)
            {
                float step = moveSpeed * Time.deltaTime;
                float remaining = moveDistance - moved;
                if (step > remaining) step = remaining;
                transform.position += transform.right * step;
                moved += step;
            }
        }

        /// <summary>
        /// Initialize the reaper for a target.
        /// </summary>
        /// <param name="target">Target object to reap.</param>
        /// <param name="fromHero">True if spawned from the hero.</param>
        /// <param name="onKill">Callback invoked after the target is killed.</param>
        public void Init(GameObject target, bool fromHero = false, Action onKill = null)
        {
            this.target = target;
            this.onKill = onKill;
            this.fromHero = fromHero;
        }

        /// <summary>
        /// Animator event used to kill the assigned target.
        /// </summary>
        public void KillTarget()
        {
            if (target == null) return;
            var hp = target.GetComponent<IHasHealth>();
            var dmg = target.GetComponent<IDamageable>();
            if (hp != null && dmg != null && hp.CurrentHealth > 0f)
            {
                float amount = hp.CurrentHealth;
                var enemy = target.GetComponent<Enemy>();
                if (enemy != null && enemy.Stats != null)
                    amount += enemy.Stats.defense + 1f;
                dmg.TakeDamage(amount);
                if (fromHero)
                {
                    var tracker = GameplayStatTracker.Instance ??
                                   UnityEngine.Object.FindFirstObjectByType<GameplayStatTracker>();
                    tracker?.AddDamageDealt(amount);
                    var buff = BuffManager.Instance ??
                               UnityEngine.Object.FindFirstObjectByType<BuffManager>();
                    var hero = HeroController.Instance ??
                                UnityEngine.Object.FindFirstObjectByType<HeroController>();
                    var heroHp = hero != null ? hero.GetComponent<HeroHealth>() : null;
                    if (buff != null && heroHp != null)
                    {
                        float ls = buff.LifestealPercent;
                        if (ls > 0f)
                            heroHp.Heal(amount * ls / 100f);
                    }
                }
            }
            onKill?.Invoke();
            target = null;
        }

        /// <summary>
        /// Animator event to destroy the reaper instance.
        /// </summary>
        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Spawns a new reaper targeting the given object.
        /// </summary>
        public static ReaperManager Spawn(GameObject prefab, GameObject target, Transform parent = null,
            bool fromHero = false, Action onKill = null, Vector3? positionOffset = null)
        {
            if (prefab == null || target == null) return null;
            var offset = positionOffset ?? Vector3.zero;
            var obj = Instantiate(prefab, target.transform.position + offset, Quaternion.identity, parent);
            var mgr = obj.GetComponent<ReaperManager>();
            if (mgr != null)
                mgr.Init(target, fromHero, onKill);
            return mgr;
        }
    }
}
