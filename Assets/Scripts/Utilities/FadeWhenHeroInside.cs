using UnityEngine;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Fades the attached <see cref="SpriteRenderer"/> when the hero enters this
    /// object's trigger collider and restores the alpha on exit.
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

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                defaultAlpha = spriteRenderer.color.a;

            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
            targetAlpha = defaultAlpha;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<HeroController>() != null)
                targetAlpha = fadedAlpha;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<HeroController>() != null)
                targetAlpha = defaultAlpha;
        }

        private void Update()
        {
            if (spriteRenderer == null)
                return;
            var c = spriteRenderer.color;
            c.a = Mathf.MoveTowards(c.a, targetAlpha, fadeSpeed * Time.deltaTime);
            spriteRenderer.color = c;
        }
    }
}
