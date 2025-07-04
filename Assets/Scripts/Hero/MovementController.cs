using Pathfinding;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    public class MovementController : MonoBehaviour
    {
        private AIPath ai;
        private AIDestinationSetter setter;
        private bool destinationOverride;

        public AIPath Path => ai;
        public Transform Destination
        {
            get => setter != null ? setter.target : null;
            set { if (setter != null) setter.target = value; }
        }

        private void Awake()
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
        }

        public void EnableMovement(bool enable)
        {
            if (ai != null)
                ai.canMove = enable;
        }

        public void SetDestination(Transform dest)
        {
            destinationOverride = false;
            if (setter != null)
                setter.target = dest;
            ai?.SearchPath();
        }

        public void MarkDestinationReached()
        {
            destinationOverride = true;
        }

        public bool IsAtDestination(Transform dest)
        {
            if (dest == null || ai == null) return false;
            if (destinationOverride) return true;
            if (ai.reachedDestination || ai.reachedEndOfPath) return true;

            float threshold = ai.endReachedDistance + 0.1f;
            return Vector2.Distance(transform.position, dest.position) <= threshold;
        }
    }
}
