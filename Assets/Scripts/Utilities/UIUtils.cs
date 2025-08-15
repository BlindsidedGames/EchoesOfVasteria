using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    ///     Common UI helper methods.
    /// </summary>
    public static class UIUtils
    {
        /// <summary>
        ///     Destroys all child GameObjects of the given parent Transform.
        /// </summary>
        /// <param name="parent">The parent transform whose children will be destroyed.</param>
        public static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;

            foreach (Transform child in parent)
                Object.Destroy(child.gameObject);
        }
    }
}
