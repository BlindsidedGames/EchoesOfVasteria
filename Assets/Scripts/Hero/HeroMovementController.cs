using System;
using Pathfinding;
using TimelessEchoes.Enemies;
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
        private Seeker seeker;
        private AIDestinationSetter setter;
        private Transform idleWalkTarget;
        private EchoController echo;
        private Camera guardCamera;
        private float nextGuardUpdate;
        private float normalMaxSpeed;
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
            normalMaxSpeed = ai ? ai.maxSpeed : 0;
            seeker = GetComponent<Seeker>();
            setter = GetComponent<AIDestinationSetter>();
            isEcho = isEchoActor;
            echo = isEchoActor ? GetComponent<EchoController>() : null;
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
            nextGuardUpdate = 0;

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
            Vector2 vel = ai != null && ai.simulateMovement ? ai.desiredVelocity : Vector2.zero;
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
            var threshold = ai.endReachedDistance + 0.1f;
            if (Vector2.Distance(transform.position, dest.position) <= threshold) return true;

            // Some task targets are in water or inside an obstacle. The reachable
            // path endpoint is their interaction spot, but an old path left by a
            // conversation must never count as arrival at a different task.
            if (!ai.hasPath || ai.pathPending || !ai.reachedEndOfPath) return false;
            var path = seeker != null ? seeker.GetCurrentPath() as ABPath : null;
            return path != null && !path.error &&
                ((Vector2)(path.originalEndPoint - dest.position)).sqrMagnitude <= 0.0001f;

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

            if (echo && echo.Type == EchoType.Combat && TryGuardMainHero(mainHero))
                return true;

            if (setter.target != mainHero)
            {
                setter.target = mainHero;
                ai?.SearchPath();
            }

            ai.maxSpeed = normalMaxSpeed;
            ai.simulateMovement = true;
            return true;
        }

        private bool TryGuardMainHero(Transform mainHero)
        {
            if (!GameManager.Instance || !GameManager.Instance.CurrentMap) return false;
            if (!guardCamera && EnemyActivator.Instance) guardCamera = EnemyActivator.Instance.GetComponent<Camera>();
            if (!guardCamera) return false;
            if (idleWalkTarget && setter.target == idleWalkTarget && Time.time < nextGuardUpdate)
            { ContinueGuardMovement(); return true; }
            nextGuardUpdate = Time.time + .2f;

            // Use the visible world rather than the wider portrait simulation area.
            var min = guardCamera.ViewportToWorldPoint(Vector3.zero);
            var max = guardCamera.ViewportToWorldPoint(Vector3.one);
            var heroPosition = mainHero.position;
            var ahead = Mathf.Min(Mathf.Max(0, max.x - .5f - heroPosition.x) * .5f, echo.CombatAggroRange);
            if (ahead < .25f) return false;
            var index = 0;
            foreach (var other in EchoController.CombatEchoes)
            {
                if (other == echo) break;
                if (other && other.isActiveAndEnabled && other.Type == EchoType.Combat) index++;
            }
            var spread = index == 0 ? 0 : (index % 2 == 1 ? 1 : -1) * ((index + 1) / 2) * 1.25f;
            var candidate = new Vector3(heroPosition.x + ahead,
                Mathf.Clamp(heroPosition.y + spread, min.y + .5f, Mathf.Max(min.y + .5f, max.y - .5f)), heroPosition.z);
            var pathfinder = AstarPath.active;
            if (!pathfinder) return false;
            var constraint = NearestNodeConstraint.Walkable; constraint.tags = ~0;
            var start = pathfinder.GetNearest(transform.position, constraint).node;
            var goal = pathfinder.GetNearest(candidate, constraint);
            if (start == null || goal.node == null || !PathUtilities.IsPathPossible(start, goal.node)) return false;
            var position = (Vector3)goal.position; position.z = heroPosition.z;
            if (position.x < heroPosition.x || position.x > max.x - .25f || position.y < min.y || position.y > max.y) return false;
            EnsureIdleWalkTarget();
            var moved = (idleWalkTarget.position - position).sqrMagnitude > .0625f;
            idleWalkTarget.position = position;
            if (setter.target != idleWalkTarget || moved)
            { destinationOverride = false; setter.target = idleWalkTarget; ai.SearchPath(); }
            ContinueGuardMovement();
            return true;
        }

        private void ContinueGuardMovement()
        {
            // Normal speed is restored before combat targeting each frame.
            var catchingUp = (idleWalkTarget.position - transform.position).sqrMagnitude > 1f;
            ai.maxSpeed = normalMaxSpeed * (catchingUp ? 1.2f : 1f);
            ai.simulateMovement = true;
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
            normalMaxSpeed = speed;
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
