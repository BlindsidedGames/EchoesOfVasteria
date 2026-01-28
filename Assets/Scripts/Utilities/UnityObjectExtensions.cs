using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Extension methods for Unity objects providing safe access patterns.
    /// </summary>
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Safely get transform from a component that may have been destroyed.
        /// Use this instead of try-catch blocks around .transform access.
        /// </summary>
        /// <param name="comp">The component to get the transform from.</param>
        /// <param name="t">The transform if successful, null otherwise.</param>
        /// <returns>True if the transform was retrieved successfully.</returns>
        public static bool TryGetTransformSafe(this Component comp, out Transform t)
        {
            t = null;
            if (comp == null) return false;
            try
            {
                t = comp.transform;
                return t != null;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
        }

        /// <summary>
        /// Safely get a component from a GameObject that may have been destroyed.
        /// </summary>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <param name="go">The GameObject to get the component from.</param>
        /// <param name="component">The component if successful, null otherwise.</param>
        /// <returns>True if the component was retrieved successfully.</returns>
        public static bool TryGetComponentSafe<T>(this GameObject go, out T component) where T : Component
        {
            component = null;
            if (go == null) return false;
            try
            {
                component = go.GetComponent<T>();
                return component != null;
            }
            catch (MissingReferenceException)
            {
                return false;
            }
        }
    }
}
