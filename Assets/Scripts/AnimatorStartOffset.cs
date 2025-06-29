using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Utility component that offsets the starting time of an Animator's
    /// current state. Attach this script to prefabs to prevent identical
    /// objects from animating in perfect sync.
    /// </summary>
    public class AnimatorStartOffset : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Vector2 normalizedTimeRange = new(0f, 1f);

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            if (animator == null) return;

            var state = animator.GetCurrentAnimatorStateInfo(0);
            var offset = Random.Range(normalizedTimeRange.x, normalizedTimeRange.y);
            animator.Play(state.fullPathHash, 0, offset);
        }
    }
}
