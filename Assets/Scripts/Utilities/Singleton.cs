using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// </summary>
    /// <typeparam name="T">Type deriving from MonoBehaviour.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// The current instance of <typeparamref name="T"/>.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Assigns the singleton instance and destroys duplicates.
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this as T;
        }

        /// <summary>
        /// Clears the singleton instance when destroyed.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
