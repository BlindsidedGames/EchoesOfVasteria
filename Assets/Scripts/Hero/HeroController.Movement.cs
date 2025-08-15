#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using UnityEngine;
using Sirenix.OdinInspector;

namespace TimelessEchoes.Hero
{
    public partial class HeroController
    {
        private void UpdateAnimation()
        {
            Vector2 vel = ai.desiredVelocity;
            var dir = vel;

            if (dir.sqrMagnitude < 0.0001f && setter != null && setter.target != null)
                dir = setter.target.position - transform.position;

            if (fourDirectional)
            {
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    dir.y = 0f;
                else
                    dir.x = 0f;
            }
            else
            {
                dir.y = 0f;
            }

            if (dir.sqrMagnitude > 0.0001f)
                lastMoveDir = dir;

            var mag = dir.magnitude;

            if (animator != null)
            {
                animator.SetFloat("MoveX", lastMoveDir.x);
                animator.SetFloat("MoveY", lastMoveDir.y);
                animator.SetFloat("MoveMagnitude", mag);
            }

            if (AutoBuffAnimator != null && AutoBuffAnimator.isActiveAndEnabled)
            {
                AutoBuffAnimator.SetFloat("MoveX", lastMoveDir.x);
                AutoBuffAnimator.SetFloat("MoveY", lastMoveDir.y);
                AutoBuffAnimator.SetFloat("MoveMagnitude", mag);
            }

            if (spriteRenderer != null)
                spriteRenderer.flipX = lastMoveDir.x < 0f;

            if (autoBuffSpriteRenderer != null)
                autoBuffSpriteRenderer.flipX = lastMoveDir.x < 0f;
        }

        public void SetActiveState(bool active)
        {
            if (ai != null) ai.enabled = active;
            if (setter != null) setter.enabled = active;
            logicActive = active;

            if (!active && animator != null)
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", 0f);
                animator.SetFloat("MoveMagnitude", 0f);
            }
        }

        public void SetDestination(Transform dest)
        {
            destinationOverride = false;
            setter.target = dest;
            ai?.SearchPath();
        }

        [Button("Mark Destination Reached")]
        public void SetDestinationReached()
        {
            destinationOverride = true;
        }

        private bool IsAtDestination(Transform dest)
        {
            if (dest == null || ai == null) return false;
            if (destinationOverride) return true;
            if (ai.reachedDestination || ai.reachedEndOfPath) return true;

            var threshold = ai.endReachedDistance + 0.1f;
            return Vector2.Distance(transform.position, dest.position) <= threshold;
        }

        private void AutoAdvance()
        {
            if (IsEcho && Instance != null && Instance != this)
            {
                var mainHero = Instance.transform;

                if (setter.target != mainHero)
                {
                    setter.target = mainHero;
                    ai?.SearchPath();
                }

                ai.canMove = true;
                return;
            }

            if (idleWalkTarget == null)
            {
                idleWalkTarget = new GameObject("IdleWalkTarget").transform;
                idleWalkTarget.hideFlags = HideFlags.HideInHierarchy;
            }

            var pos = transform.position;
            if (idleWalkTarget.position.x - pos.x < 1f)
                idleWalkTarget.position = new Vector3(pos.x + idleWalkStep, pos.y, pos.z);

            if (setter.target != idleWalkTarget)
            {
                setter.target = idleWalkTarget;
                ai?.SearchPath();
            }

            ai.canMove = true;
        }
    }
}
