using Pathfinding;
using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.NPC
{
    /// <summary>
    /// Simple movement and animation controller for Mildred the cat.
    /// </summary>
    [RequireComponent(typeof(AIPath))]
    public class MildredMovementController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private HeroController hero;
        private AIPath ai;

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            if (hero == null)
                hero = HeroController.Instance ?? FindFirstObjectByType<HeroController>();
        }

        private void Update()
        {
            if (hero != null && ai != null)
                ai.maxSpeed = hero.MoveSpeed + 1f;

            if (animator != null && ai != null)
                animator.SetFloat("MoveMagnitude", ai.desiredVelocity.magnitude);
        }
    }
}
