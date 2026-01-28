using System;
using Pathfinding;
using TimelessEchoes.Utilities;
using UnityEngine;

namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Handles movement logic for the hero/echo: pathfinding, animation sync,
    /// destination management, and idle walking. Works in conjunction with HeroBase
    /// which manages the overall state machine.
    /// </summary>
    [RequireComponent(typeof(AIPath))]
    [RequireComponent(typeof(AIDestinationSetter))]
    public class HeroMovementController : MonoBehaviour
    {
        [SerializeField] private float idleWalkStep = 5f;

        private AIPath ai;
        private AIDestinationSetter setter;
        private Transform idleWalkTarget;
        private Vector2 lastMoveDir = Vector2.down;
        private bool destinationOverride;
        private bool isEcho;
        private bool logicActive = true;

        /// <summary>
        /// Last movement direction (normalized, snapped to cardinal).
        /// </summary>
        public Vector2 LastMoveDirection => lastMoveDir;

        /// <summary>
        /// The AIPath component for pathfinding.
        /// </summary>
        public AIPath AI => ai;

        /// <summary>
        /// The AIDestinationSetter component for target management.
        /// </summary>
        public AIDestinationSetter Setter => setter;

        /// <summary>
        /// Whether movement logic is active.
        /// </summary>
        public bool LogicActive => logicActive;

        /// <summary>
        /// Initialize the movement controller.
        /// </summary>
        /// <param name="isEchoActor">Whether this is an echo (not main hero).</param>
        /// <param name="walkStep">Distance to walk when idle advancing.</param>
        public void Init(bool isEchoActor, float walkStep = 5f)
        {
            ai = GetComponent<AIPath>();
            setter = GetComponent<AIDestinationSetter>();
            isEcho = isEchoActor;
            idleWalkStep = walkStep;
        }

        /// <summary>
        /// Reset state on enable.
        /// </summary>
        public void ResetState()
        {
            destinationOverride = false;
            lastMoveDir = Vector2.down;
            logicActive = true;

            if (!isEcho)
            {
                if (idleWalkTarget == null)
                {
                    idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                    idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
                }
                idleWalkTarget.position = transform.position;
            }
        }

        /// <summary>
        /// Cleanup when disabled.
        /// </summary>
        public void Cleanup()
        {
            if (idleWalkTarget != null)
                Destroy(idleWalkTarget.gameObject);
            idleWalkTarget = null;
        }

        /// <summary>
        /// Update animation parameters based on movement.
        /// </summary>
        /// <param name="animator">The animator to update.</param>
        /// <param name="sprite">The sprite renderer for flip.</param>
        /// <param name="fourDirectional">Whether to use 4-directional snapping.</param>
        /// <param name="onSecondaryUpdate">Callback for secondary animator update (main hero only).</param>
        /// <param name="onSecondaryFlip">Callback for secondary sprite flip (main hero only).</param>
        public void UpdateAnimation(Animator animator, SpriteRenderer sprite, bool fourDirectional,
            Action<Vector2, float> onSecondaryUpdate = null, Action<bool> onSecondaryFlip = null)
        {
            Vector2 vel = ai != null ? ai.desiredVelocity : Vector2.zero;
            Vector2 dir = vel;

            if (dir.sqrMagnitude < 0.0001f && setter != null && setter.target != null)
                dir = setter.target.position - transform.position;

            dir = AnimatorMovementHelper.SnapToCardinal(dir, fourDirectional);

            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir.normalized;

            var speed = vel.magnitude;

            AnimatorMovementHelper.SetMovement(animator, lastMoveDir, speed);
            onSecondaryUpdate?.Invoke(lastMoveDir, speed);

            if (sprite != null)
                sprite.flipX = lastMoveDir.x < 0f;

            onSecondaryFlip?.Invoke(lastMoveDir.x < 0f);
        }

        /// <summary>
        /// Enable or disable movement systems.
        /// </summary>
        /// <param name="active">Whether to enable movement.</param>
        /// <param name="animator">The animator to clear movement on disable.</param>
        public void SetActiveState(bool active, Animator animator)
        {
            if (ai != null) ai.enabled = active;
            if (setter != null) setter.enabled = active;
            logicActive = active;

            if (!active)
                AnimatorMovementHelper.ClearMovement(animator);
        }

        /// <summary>
        /// Set the movement destination.
        /// </summary>
        public void SetDestination(Transform dest)
        {
            destinationOverride = false;
            if (setter != null)
                setter.target = dest;
            ai?.SearchPath();
        }

        /// <summary>
        /// Mark destination as reached (for immediate arrival).
        /// </summary>
        public void SetDestinationReached()
        {
            destinationOverride = true;
        }

        /// <summary>
        /// Check if hero has arrived at destination.
        /// </summary>
        public bool IsAtDestination(Transform dest)
        {
            if (dest == null || ai == null) return false;
            if (destinationOverride) return true;
            if (ai.reachedDestination || ai.reachedEndOfPath) return true;

            var threshold = ai.endReachedDistance + 0.1f;
            return Vector2.Distance(transform.position, dest.position) <= threshold;
        }

        /// <summary>
        /// Try to follow the main hero (for echoes).
        /// </summary>
        /// <param name="mainHero">The main hero transform to follow.</param>
        /// <returns>True if following main hero, false if this is the main hero.</returns>
        public bool TryFollowMainHero(Transform mainHero)
        {
            if (!isEcho || mainHero == null || mainHero == transform)
                return false;

            if (setter.target != mainHero)
            {
                setter.target = mainHero;
                ai?.SearchPath();
            }

            ai.simulateMovement = true;
            return true;
        }

        /// <summary>
        /// Advance to idle walk position (main hero only).
        /// </summary>
        public void IdleAdvance()
        {
            EnsureIdleWalkTarget();

            var pos = transform.position;
            if (idleWalkTarget.position.x - pos.x < 1f)
            {
                var nextIdle = ResolveIdleAdvanceTarget(pos);
                idleWalkTarget.position = nextIdle;
            }

            if (setter.target != idleWalkTarget)
            {
                setter.target = idleWalkTarget;
                ai?.SearchPath();
            }

            ai.simulateMovement = true;
        }

        /// <summary>
        /// Update max speed from stats.
        /// </summary>
        public void UpdateMaxSpeed(float speed)
        {
            if (ai != null)
                ai.maxSpeed = speed;
        }

        /// <summary>
        /// Set whether AI should simulate movement.
        /// </summary>
        public void SetSimulateMovement(bool simulate)
        {
            if (ai != null)
                ai.simulateMovement = simulate;
        }

        /// <summary>
        /// Teleport to current position (reset path).
        /// </summary>
        public void TeleportToCurrent()
        {
            ai?.Teleport(transform.position);
        }

        /// <summary>
        /// Clear the current destination target.
        /// </summary>
        public void ClearTarget()
        {
            if (setter != null)
                setter.target = null;
        }

        private void EnsureIdleWalkTarget()
        {
            if (idleWalkTarget == null)
            {
                idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
            }
        }

        private Vector3 ResolveIdleAdvanceTarget(Vector3 currentPosition)
        {
            var fallback = new Vector3(currentPosition.x + idleWalkStep, currentPosition.y, currentPosition.z);
            var pathfinder = AstarPath.active;
            if (pathfinder == null)
                return fallback;

            var constraint = NearestNodeConstraint.Walkable;
            constraint.tags = ~0; // allow all tags

            var startInfo = pathfinder.GetNearest(currentPosition, constraint);
            var startNode = startInfo.node;
            if (startNode == null || !startNode.Walkable)
                return fallback;

            var verticalStep = Mathf.Max(0.5f, Mathf.Abs(idleWalkStep) * 0.5f);
            var candidates = new[]
            {
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y + verticalStep, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y - verticalStep, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y + verticalStep * 2f, currentPosition.z),
                new Vector3(currentPosition.x + idleWalkStep, currentPosition.y - verticalStep * 2f, currentPosition.z)
            };

            foreach (var candidate in candidates)
            {
                var goalInfo = pathfinder.GetNearest(candidate, constraint);
                var node = goalInfo.node;
                if (node == null || !node.Walkable)
                    continue;

                if (!PathUtilities.IsPathPossible(startNode, node))
                    continue;

                var resolved = goalInfo.position;
                resolved.z = currentPosition.z;

                if (resolved.x < currentPosition.x)
                    continue;

                return resolved;
            }

            return fallback;
        }
    }
}
