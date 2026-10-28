using UnityEngine;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Fades the attached sprite while the hero stands inside its canopy.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class FadeWhenHeroInside : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0.3f;
        [SerializeField] private float fadeSpeed = 5f;

        private float targetAlpha;
        private float defaultAlpha;
        private Collider2D canopy;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                defaultAlpha = spriteRenderer.color.a;

            canopy = GetComponent<Collider2D>();
            if (canopy != null)
                canopy.isTrigger = true;
            targetAlpha = defaultAlpha;
        }

        private void OnEnable()
        {
            targetAlpha = defaultAlpha;
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                color.a = defaultAlpha;
                spriteRenderer.color = color;
            }
        }

        private void Update()
        {
            if (spriteRenderer == null)
                return;
            // The hero uses pathfinding/RVO movement and has no physics collider.
            var hero = HeroController.Instance;
            targetAlpha = hero && canopy && canopy.OverlapPoint(hero.transform.position)
                ? fadedAlpha : defaultAlpha;
            var c = spriteRenderer.color;
            c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
            spriteRenderer.color = c;
        }
    }
}
