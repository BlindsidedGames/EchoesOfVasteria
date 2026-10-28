using UnityEngine;

namespace TimelessEchoes.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackLunge : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField, Min(0f)] private float forwardDistance = 3f / 16f;
        [SerializeField, Min(0f)] private float recoilDistance = 1f / 16f;
        [SerializeField, Min(.01f)] private float attackDuration = .34f;
        [SerializeField, Min(0f)] private float hoverAmplitude;
        [SerializeField, Min(.1f)] private float hoverPeriod = 1.5f;
        [SerializeField, Range(0f, .2f)] private float crawlSquash;
        [SerializeField, Min(.1f)] private float crawlPeriod = .85f;

        private Vector3 restingPosition, restingScale, direction;
        private float elapsed, hoverElapsed, hoverPhase;
        private float crawlElapsed, crawlBlend;
        private bool playing;
        private Pathfinding.AIPath movement;

        private void Awake()
        {
            if (visual)
            {
                restingPosition = visual.localPosition;
                restingScale = visual.localScale;
            }
            movement = GetComponent<Pathfinding.AIPath>();
        }

        private void OnEnable()
        {
            hoverElapsed = 0f;
            // Vary the visual phase without consuming the map generator's random sequence.
            hoverPhase = Mathf.Repeat((GetEntityId().GetHashCode() & 1023) * .618034f, 1f) * Mathf.PI * 2f;
        }

        public void Play(Vector2 attackDirection)
        {
            if (!isActiveAndEnabled || !visual || attackDirection.sqrMagnitude < .0001f) return;
            direction = transform.InverseTransformDirection(attackDirection).normalized;
            elapsed = 0f;
            playing = true;
        }

        private void LateUpdate()
        {
            if (!visual) return;
            hoverElapsed += Time.deltaTime;
            float offset = 0f;
            if (playing)
            {
                elapsed += Time.deltaTime;
                var motionTime = elapsed * .34f / Mathf.Max(.01f, attackDuration);
                if (motionTime < .1f)
                    offset = Mathf.Lerp(0f, forwardDistance, Mathf.SmoothStep(0f, 1f, motionTime / .1f));
                else if (motionTime < .22f)
                    offset = Mathf.Lerp(forwardDistance, -recoilDistance, Mathf.SmoothStep(0f, 1f, (motionTime - .1f) / .12f));
                else if (motionTime < .34f)
                    offset = Mathf.Lerp(-recoilDistance, 0f, Mathf.SmoothStep(0f, 1f, (motionTime - .22f) / .12f));
                else
                {
                    playing = false;
                    elapsed = 0f;
                }
            }
            var hover = Mathf.Sin(hoverElapsed * Mathf.PI * 2f / Mathf.Max(.1f, hoverPeriod) + hoverPhase) * hoverAmplitude;
            visual.localPosition = restingPosition + Vector3.up * hover + direction * offset;
            if (crawlSquash > 0f)
            {
                var crawling = !playing && movement && movement.velocity.sqrMagnitude > .0025f;
                crawlBlend = Mathf.MoveTowards(crawlBlend, crawling ? 1f : 0f, Time.deltaTime * 8f);
                if (crawling) crawlElapsed += Time.deltaTime;
                var squash = Mathf.Sin(crawlElapsed * Mathf.PI * 2f / crawlPeriod) * crawlSquash * crawlBlend;
                visual.localScale = Vector3.Scale(restingScale, new Vector3(1f + squash, 1f - squash, 1f));
            }
        }

        private void OnDisable() => ResetPose();

        private void ResetPose()
        {
            if (visual)
            {
                visual.localPosition = restingPosition;
                visual.localScale = restingScale;
            }
            playing = false;
            elapsed = 0f;
            hoverElapsed = 0f;
            crawlElapsed = 0f;
            crawlBlend = 0f;
        }
    }
}
