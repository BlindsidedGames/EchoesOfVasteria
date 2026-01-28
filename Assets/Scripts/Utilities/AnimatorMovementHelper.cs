using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Helper class for setting movement-related animator parameters.
    /// Caches hash values to avoid string allocation overhead.
    /// </summary>
    public static class AnimatorMovementHelper
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveMagnitudeHash = Animator.StringToHash("MoveMagnitude");

        /// <summary>
        /// Set movement parameters on an animator.
        /// </summary>
        /// <param name="anim">The animator to update.</param>
        /// <param name="direction">The movement direction (typically normalized).</param>
        /// <param name="speed">The movement speed/magnitude.</param>
        public static void SetMovement(Animator anim, Vector2 direction, float speed)
        {
            if (anim == null) return;
            anim.SetFloat(MoveXHash, direction.x);
            anim.SetFloat(MoveYHash, direction.y);
            anim.SetFloat(MoveMagnitudeHash, speed);
        }

        /// <summary>
        /// Clear all movement parameters on an animator (sets to zero).
        /// </summary>
        /// <param name="anim">The animator to clear.</param>
        public static void ClearMovement(Animator anim)
        {
            SetMovement(anim, Vector2.zero, 0f);
        }

        /// <summary>
        /// Snap a direction vector to cardinal directions.
        /// </summary>
        /// <param name="dir">The input direction.</param>
        /// <param name="fourDirectional">If true, snaps to 4 directions. If false, only zeroes Y component.</param>
        /// <returns>The snapped direction.</returns>
        public static Vector2 SnapToCardinal(Vector2 dir, bool fourDirectional)
        {
            if (!fourDirectional)
            {
                dir.y = 0f;
                return dir;
            }

            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                dir.y = 0f;
            else
                dir.x = 0f;

            return dir;
        }
    }
}
