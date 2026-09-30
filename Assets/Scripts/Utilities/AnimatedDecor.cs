using UnityEngine;
using UnityEngine.Tilemaps;

namespace TimelessEchoes
{
    /// <summary>
    /// Plays an AnimatedTile's existing frames on a freely positioned sprite.
    /// Placement, material and sorting remain the renderer's responsibility.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AnimatedDecor : MonoBehaviour
    {
        [SerializeField] private AnimatedTile animationSource;
        [SerializeField, Min(0.01f)] private float framesPerSecond = 4f;
        [SerializeField] private bool randomizeStart = true;
        [SerializeField] private Vector2 speedVariation = new(0.85f, 1.15f);

        private SpriteRenderer spriteRenderer;
        private float framePosition;
        private float playbackSpeed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            var frames = animationSource != null ? animationSource.m_AnimatedSprites : null;
            if (frames == null || frames.Length == 0)
                return;

            framePosition = randomizeStart ? Random.Range(0f, frames.Length) : 0f;
            playbackSpeed = Mathf.Max(0.01f, framesPerSecond) *
                            Random.Range(Mathf.Max(0.01f, speedVariation.x),
                                Mathf.Max(Mathf.Max(0.01f, speedVariation.x), speedVariation.y));
            spriteRenderer.sprite = frames[Mathf.Min((int)framePosition, frames.Length - 1)];
        }

        private void Update()
        {
            var frames = animationSource != null ? animationSource.m_AnimatedSprites : null;
            if (frames == null || frames.Length == 0)
                return;

            framePosition = Mathf.Repeat(framePosition + Time.deltaTime * playbackSpeed, frames.Length);
            var next = frames[(int)framePosition];
            if (spriteRenderer.sprite != next)
                spriteRenderer.sprite = next;
        }
    }
}
