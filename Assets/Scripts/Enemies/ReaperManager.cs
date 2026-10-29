using System;
using TimelessEchoes.Buffs;
using TimelessEchoes.Hero;
using TimelessEchoes.Stats;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    ///     Handles spawning and timed kills for reaper animations.
    /// </summary>
    public class ReaperManager : MonoBehaviour
    {
        private GameObject target;
        private Action onKill;
        private bool fromHero;
        private UI.Toolkit.ToolkitNavigationScreen flightNavigation;
        private Vector3 arrivalOffset;

        [SerializeField] private float moveDistance = 0.5f;
        [SerializeField] private float moveDownDistance;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float moveDownSpeed = 8f;

        private Vector3 startPosition;
        private float moved;
        private float movedDown;

        private void OnEnable()
        {
            startPosition = transform.position;
            moved = 0f;
            movedDown = 0f;
        }

        private void Update()
        {
            if (flightNavigation)
            {
                // Follow through the swing as well, so a moving hero cannot
                // leave Carl's visible strike behind before the impact event.
                if (target) transform.position = target.transform.position + arrivalOffset;
                return;
            }
            var step = moveSpeed * Time.deltaTime;
            var downStep = moveDownSpeed * Time.deltaTime;

            if (moved < moveDistance)
            {
                var remaining = moveDistance - moved;
                var horizStep = step;
                if (horizStep > remaining) horizStep = remaining;
                var direction = Mathf.Sign(transform.localScale.x);
                transform.position += transform.right * horizStep * direction;
                moved += horizStep;
            }

            if (moveDownDistance > 0f && movedDown < moveDownDistance)
            {
                var remaining = moveDownDistance - movedDown;
                var vertStep = downStep;
                if (vertStep > remaining) vertStep = remaining;
                transform.position += Vector3.down * vertStep;
                movedDown += vertStep;
            }
        }

        /// <summary>
        ///     Initialize the reaper for a target.
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
        ///     Animator event used to kill the assigned target.
        /// </summary>
        public void KillTarget()
        {
            if (target == null) return;
            var hp = target.GetComponent<IHasHealth>();
            var dmg = target.GetComponent<IDamageable>();
            var heroHp = target.GetComponent<Hero.HeroHealth>();
            var heroCtrl = target.GetComponent<HeroController>();
            if (heroHp != null)
                heroHp.Immortal = false; // ensure OnDeath fires
            var tracker = GameplayStatTracker.Instance ??
                          FindAnyObjectByType<GameplayStatTracker>();

            if (hp != null && hp.CurrentHealth > 0f)
            {
                // Bypass defense and any mitigation; force death immediately
                var amount = hp.CurrentHealth; // capture pre-kill health for stats
                var hb = target.GetComponent<TimelessEchoes.HealthBase>();
                if (hb != null)
                    hb.KillImmediately();
                else if (dmg != null)
                    dmg.TakeDamage(amount + 1000000f); // extreme overkill fallback
                if (fromHero)
                {
                    tracker?.AddDamageDealt(amount);
                }
            }

            // Count reaps only when the hero is the target
            if (heroCtrl != null)
                tracker?.AddTimesReaped();

            // If the hero was reaped for reaching the maximum distance,
            // increase that maximum by 1% for future runs if the current map allows it.
            if (heroCtrl != null && heroCtrl.ReaperSpawnedByDistance && tracker != null)
            {
                var currentConfig = TimelessEchoes.GameManager.CurrentGenerationConfig;
                if (currentConfig != null && !currentConfig.allowMaxDistanceReapGain)
                {
                    onKill?.Invoke();
                    target = null;
                    if (heroCtrl != null)
                    {
                        var gm = TimelessEchoes.GameManager.Instance;
                        gm?.EnsureAutoReturnOnReapIfQueued();
                        gm?.ForceHandleHeroDeath();
                    }
                    return;
                }
                var buff = BuffManager.Instance;
                var baseMax = tracker.MaxRunDistance;
                var mult = buff != null ? buff.MaxDistanceMultiplier : 1f;
                var flat = buff != null ? buff.MaxDistanceFlatBonus : 0f;
                var buffedMax = baseMax * mult + flat;
                var oc = Blindsided.Oracle.oracle;
                var isDemo = oc != null && oc.demo;
                // In demo, treat the effective cap as 300 to avoid implicit progression beyond the demo limit
                var effectiveForIncrease = isDemo ? Mathf.Min(buffedMax, 300f) : buffedMax;
                var increase = effectiveForIncrease * 0.01f;
                tracker.IncreaseMaxRunDistance(increase);
            }

            onKill?.Invoke();
            target = null;

            // Fallback: if the hero was reaped and a return was queued, force an
            // immediate return to the tavern. This covers edge cases where the
            // death event flow is interrupted and ensures expected UX.
            if (heroCtrl != null)
            {
                var gm = TimelessEchoes.GameManager.Instance;
                gm?.EnsureAutoReturnOnReapIfQueued();
                // If the OnDeath flow somehow didn't fire yet, enforce it.
                gm?.ForceHandleHeroDeath();
            }
        }

        private void OnDestroy()
        {
            if (flightNavigation) flightNavigation.CancelReaperFlight();
        }

        private System.Collections.IEnumerator ArriveFromProgress(SpriteRenderer artwork)
        {
            var animator = GetComponent<Animator>();
            var flyIn = animator && animator.runtimeAnimatorController
                ? Array.Find(animator.runtimeAnimatorController.animationClips, clip => clip.name == "FlyIn") : null;
            // Land at the original fly-in's final position, then play the full
            // authored swing. Its KillTarget event must not run during travel.
            arrivalOffset = transform.position - target.transform.position +
                transform.right * (moveDistance * Mathf.Sign(transform.localScale.x)) - Vector3.up * moveDownDistance;
            moved = moveDistance;
            movedDown = moveDownDistance;
            transform.position = target.transform.position + arrivalOffset;
            if (animator) animator.enabled = false;
            try
            {
                yield return flightNavigation.FlyReaperFromProgress(artwork, flyIn);
            }
            finally
            {
                if (animator)
                {
                    animator.enabled = true;
                    animator.Play("Reap", 0, 0);
                    animator.Update(0);
                }
            }
        }

        /// <summary>
        ///     Animator event to destroy the reaper instance.
        /// </summary>
        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        /// <summary>
        ///     Spawns a new reaper targeting the given object.
        /// </summary>
        public static ReaperManager Spawn(GameObject prefab, GameObject target, Transform parent = null,
            bool fromHero = false, Action onKill = null, Vector3? positionOffset = null, bool fromProgressBar = false)
        {
            if (prefab == null || target == null) return null;
            var offset = positionOffset ?? Vector3.zero;

            var navigation = fromProgressBar ? FindAnyObjectByType<UI.Toolkit.ToolkitNavigationScreen>() : null;
            // Distance reaps approach from the HUD's side of the hero. Combat
            // procs retain their existing random approach.
            var fromRight = navigation && navigation.CanReceiveReaper
                ? navigation.ProgressReaperIsToRightOf(target.transform.position) : Random.value < 0.5f;
            if (fromRight) offset.x = -offset.x;

            var obj = Instantiate(prefab, target.transform.position + offset, Quaternion.identity, parent);
            if (fromRight)
            {
                // Flip horizontally by inverting the x scale rather than rotating,
                // as this is a 2D sprite.
                var localScale = obj.transform.localScale;
                localScale.x *= -1f;
                obj.transform.localScale = localScale;
            }

            var mgr = obj.GetComponent<ReaperManager>();
            if (mgr != null)
            {
                mgr.Init(target, fromHero, onKill);
                if (fromProgressBar)
                {
                    var artwork = obj.GetComponentInChildren<SpriteRenderer>();
                    if (navigation && navigation.CanReceiveReaper && artwork)
                    {
                        mgr.flightNavigation = navigation;
                        mgr.StartCoroutine(mgr.ArriveFromProgress(artwork));
                    }
                }
            }
            return mgr;
        }
    }
}
