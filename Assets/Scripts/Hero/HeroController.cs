using System.Linq;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Concrete main-hero controller that owns the singleton and main-only systems.
    /// </summary>
    [RequireComponent(typeof(HeroHealth))]
    public class HeroController : HeroBase
    {
        public static HeroController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;


        [SerializeField] private Animator autoBuffAnimator;
        [SerializeField] private SpriteRenderer autoBuffSpriteRenderer;

        protected override bool IsEchoActor => false;

        protected override void Awake()
        {
            base.Awake();
            // Ensure singleton is the latest enabled instance
            if (Instance != null && Instance != this)
                Destroy(Instance.gameObject);
            Instance = this;

            // Initialize centralized hero stat system on scene load
            HeroStatSystem.Initialize(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        // Main-hero overrides for auto-buff visuals
        protected override void OnPostAwakeAnimatorSetup()
        {
            if (autoBuffAnimator != null)
            {
                if (autoBuffSpriteRenderer == null)
                    autoBuffSpriteRenderer = autoBuffAnimator.GetComponent<SpriteRenderer>();

                if (autoBuffAnimator.gameObject.GetComponent<HeroAudio>() == null)
                    autoBuffAnimator.gameObject.AddComponent<HeroAudio>();
            }
        }

        protected override void OnApplyRandomAnimatorOffset(AnimatorStateInfo stateInfo)
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled)
            {
                var offset = Random.value;
                autoBuffAnimator.Play(stateInfo.fullPathHash, 0, offset);
            }
        }

        protected override void OnAutoBuffChangedImpl()
        {
            if (autoBuffAnimator == null) return;
            var manager = TimelessEchoes.Buffs.BuffManager.Instance;
            var active = false;
            if (manager != null)
            {
                active = manager.ActiveBuffs.Any(b =>
                    b.effects.Any(e =>
                        (e.type == TimelessEchoes.Buffs.BuffEffectType.MaxDistancePercent ||
                         e.type == TimelessEchoes.Buffs.BuffEffectType.MaxDistanceIncrease) && e.value > 0f));
            }
            autoBuffAnimator.gameObject.SetActive(active);
            if (active && autoBuffAnimator.isActiveAndEnabled)
            {
                var main = this.Animator;
                if (main != null)
                {
                    autoBuffAnimator.runtimeAnimatorController = main.runtimeAnimatorController;
                    autoBuffAnimator.avatar = main.avatar;
                    autoBuffAnimator.updateMode = main.updateMode;
                    autoBuffAnimator.speed = main.speed;

                    var state = main.GetCurrentAnimatorStateInfo(0);
                    autoBuffAnimator.Play(state.fullPathHash, 0, state.normalizedTime);
                }
            }
        }

        protected override void OnAttackAnimationStarted()
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled)
                autoBuffAnimator.Play("Attack");
        }

        protected override void SetSecondaryAnimatorSpeed(float speed)
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled)
                autoBuffAnimator.speed = speed;
        }

        protected override void UpdateSecondaryAnimatorMovement(Vector2 lastMove, float speed)
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled)
                AnimatorMovementHelper.SetMovement(autoBuffAnimator, lastMove, speed);
        }

        protected override void UpdateSecondarySpriteFlip(bool flipX)
        {
            if (autoBuffSpriteRenderer != null)
                autoBuffSpriteRenderer.flipX = flipX;
        }

        protected override void OnPlaySecondaryAnimation(string stateName)
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled && !string.IsNullOrEmpty(stateName))
                autoBuffAnimator.Play(stateName);
        }

        protected override void OnSetSecondaryTrigger(string triggerName)
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled && !string.IsNullOrEmpty(triggerName))
                autoBuffAnimator.SetTrigger(triggerName);
        }

        protected override void OnResetSecondaryTrigger(string triggerName)
        {
            if (autoBuffAnimator != null && autoBuffAnimator.isActiveAndEnabled && !string.IsNullOrEmpty(triggerName))
                autoBuffAnimator.ResetTrigger(triggerName);
        }
    }
}

