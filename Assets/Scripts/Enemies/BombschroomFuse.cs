using Blindsided.Utilities.Pooling;
using Pathfinding;
using TimelessEchoes.Hero;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Enemy), typeof(EnemyHealth))]
    public sealed class BombschroomFuse : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float fuseDuration = 2f;
        [SerializeField, Min(.1f)] private float blastRadius = 1.5f;
        [SerializeField, Min(0f)] private float damageMultiplier = 1.5f;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField, Min(.1f)] private float explosionDuration = .7f;
        [SerializeField] private GameObject fuseBar;

        public float FuseRemainingFraction => armed && !detonated
            ? Mathf.Clamp01(1f - elapsed / Mathf.Max(.1f, fuseDuration)) : 0f;

        private Enemy enemy;
        private EnemyHealth health;
        private AIPath movement;
        private AIDestinationSetter targeter;
        private Animator animator;
        private SpriteRenderer body;
        private Color bodyColor;
        private float elapsed;
        private bool armed, detonated, wasStopped;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            health = GetComponent<EnemyHealth>();
            movement = GetComponent<AIPath>();
            targeter = GetComponent<AIDestinationSetter>();
            animator = GetComponent<Animator>();
            body = GetComponent<SpriteRenderer>();
            bodyColor = body ? body.color : Color.white;
        }

        private void OnEnable()
        {
            elapsed = 0;
            armed = detonated = false;
            if (fuseBar) fuseBar.SetActive(false);
        }

        private void OnDisable()
        {
            if (armed && movement) movement.isStopped = wasStopped;
            if (body) body.color = bodyColor;
            elapsed = 0;
            armed = detonated = false;
            if (fuseBar) fuseBar.SetActive(false);
        }

        private void Update()
        {
            if (!enemy || !health || health.CurrentHealth <= 0 || detonated) return;
            if (!armed)
            {
                if (!enemy.IsEngaged || !enemy.Stats || !targeter || !targeter.target) return;
                var range = enemy.Stats.attackRange;
                if (((Vector2)(targeter.target.position - transform.position)).sqrMagnitude > range * range)
                    return;
                armed = true;
                if (fuseBar) fuseBar.SetActive(true);
                if (movement)
                {
                    wasStopped = movement.isStopped;
                    movement.isStopped = true;
                }
            }

            elapsed += Time.deltaTime;
            if (elapsed >= Mathf.Max(.1f, fuseDuration)) Detonate();
        }

        private void LateUpdate()
        {
            if (!armed || detonated) return;
            AnimatorMovementHelper.ClearMovement(animator);
            if (body)
            {
                var pulse = .5f + .5f * Mathf.Sin(elapsed * Mathf.PI * 10f);
                body.color = Color.Lerp(bodyColor,
                    new Color(.5f, 1f, .5f, bodyColor.a), pulse);
            }
        }

        private void Detonate()
        {
            if (detonated || health.CurrentHealth <= 0) return;
            detonated = true;

            if (explosionPrefab)
            {
                var effect = PoolManager.Get(explosionPrefab);
                effect.transform.position = transform.position;
                effect.transform.rotation = Quaternion.identity;
                var renderer = effect.GetComponent<SpriteRenderer>();
                if (renderer && body) renderer.flipX = body.flipX;
                var timer = effect.GetComponent<PoolAutoReturn>() ?? effect.AddComponent<PoolAutoReturn>();
                timer.ReturnAfter(explosionDuration);
            }

            ApplyBlastDamage();
            // A failed interception is not a player kill: no kill credit, XP or drops.
            health.DespawnWithoutRewards();
        }

        private void ApplyBlastDamage()
        {
            var hero = HeroHealth.Instance;
            if (!hero || !hero.isActiveAndEnabled || hero.CurrentHealth <= 0 || !enemy.Stats) return;
            var radiusSquared = blastRadius * blastRadius;
            var multiplier = 0f;
            if (((Vector2)(hero.transform.position - transform.position)).sqrMagnitude <= radiusSquared)
                multiplier = 1f;
            else
                foreach (var echo in EchoController.CombatEchoes)
                    if (echo && echo.isActiveAndEnabled && echo.AllowAttacks &&
                        ((Vector2)(echo.transform.position - transform.position)).sqrMagnitude <= radiusSquared)
                    {
                        // Echoes share the hero's health. Multiple echoes must not multiply one blast.
                        multiplier = .5f;
                        break;
                    }

            if (multiplier > 0)
                hero.TakeDamageFromEnemy(enemy.Stats.GetDamageForLevel(enemy.Level) *
                    damageMultiplier * multiplier, enemy.Level);
        }
    }
}
