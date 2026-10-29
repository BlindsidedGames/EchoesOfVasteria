using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Task for chopping down a tree. Spawns a stump when completed and
    ///     does not destroy the original tree GameObject.
    /// </summary>
    public class WoodcuttingTask : ContinuousTask
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite stumpSprite;
        [Tooltip("Optional extra stump sprites chosen at random")] [SerializeField]
        private Sprite[] stumpSpriteOptions;

        [SerializeField] private Transform cuttingPoint;

        private Sprite originalSprite;
        private Quaternion originalRotation;
        private bool hasOriginalRotation;

        private bool spawnedStump;

        protected override string AnimationName => "Chopping";
        protected override string InterruptTriggerName => "StopChopping";
        public override Transform Target => cuttingPoint != null ? cuttingPoint : transform;

        public override void StartTask()
        {
            base.StartTask();
            ResetWobble();
            spawnedStump = false;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (originalSprite == null)
                    originalSprite = spriteRenderer.sprite;
                spriteRenderer.sprite = originalSprite;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetWobble();
            // Reset visual state when reused from pool
            spawnedStump = false;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && originalSprite != null)
                spriteRenderer.sprite = originalSprite;
            IsExhausted = false;
        }

        public override void OnArrival(HeroBase hero)
        {
            ResetWobble();
            base.OnArrival(hero);
        }

        public override void Tick(HeroBase hero)
        {
            base.Tick(hero);
            if (!spawnedStump && !IsComplete() && spriteRenderer && hero && hero.Animator)
            {
                var state = hero.Animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(AnimationName))
                {
                    // The third of six axe frames lands the hit. Reading its
                    // phase keeps the tree in step with Echoes and speed buffs.
                    var sinceHit = Mathf.Repeat(state.normalizedTime, 1) - 1f / 3;
                    var shake = sinceHit >= 0 && sinceHit < .36f
                        ? Mathf.Sin(sinceHit / .36f * Mathf.PI * 2) * 2 : 0;
                    spriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, shake);
                    // An Echo can expire without another task tick. Always let
                    // its last hit settle even after the chopper disappears.
                    if (shake != 0 && !IsInvoking(nameof(ResetWobble)))
                        Invoke(nameof(ResetWobble), .18f / Mathf.Max(.01f, hero.Animator.speed));
                }
                else ResetWobble();
            }
            if (!spawnedStump && IsComplete())
            {
                ResetWobble();
                spawnedStump = true;
                if (spriteRenderer == null)
                    spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    var s = ChooseStumpSprite();
                    if (s != null)
                        spriteRenderer.sprite = s;
                }
            }
        }

        protected override void OnTaskCompleted(HeroBase hero)
        {
            ResetWobble();
            if (spawnedStump)
                return;
            spawnedStump = true;
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var s = ChooseStumpSprite();
                if (s != null)
                    spriteRenderer.sprite = s;
            }
            IsExhausted = true;
        }

        public override void OnInterrupt(HeroBase hero)
        {
            base.OnInterrupt(hero);
            ResetWobble();
        }

        protected override void OnDisable()
        {
            ResetWobble();
            base.OnDisable();
        }

        private void ResetWobble()
        {
            CancelInvoke(nameof(ResetWobble));
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
            if (!spriteRenderer) return;
            if (!hasOriginalRotation)
            { originalRotation = spriteRenderer.transform.localRotation; hasOriginalRotation = true; }
            spriteRenderer.transform.localRotation = originalRotation;
        }

        private Sprite ChooseStumpSprite()
        {
            if (stumpSpriteOptions == null || stumpSpriteOptions.Length == 0)
                return stumpSprite;

            var count = stumpSpriteOptions.Length + (stumpSprite != null ? 1 : 0);
            var index = Random.Range(0, count);
            if (index == 0 && stumpSprite != null)
                return stumpSprite;
            var optionIndex = stumpSprite != null ? index - 1 : index;
            return stumpSpriteOptions[optionIndex];
        }
    }
}
