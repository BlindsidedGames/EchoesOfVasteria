using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Generic singleton base class for MonoBehaviours.
    /// </summary>
    /// <typeparam name="T">Type deriving from MonoBehaviour.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private bool makePersistent = false;
        /// <summary>
        /// The current instance of <typeparamref name="T"/>.
        /// </summary>
        // Unity's analyzer cannot attach a runtime initializer to an open generic
        // type. Destroyed Unity objects already compare as null, and OnDestroy
        // clears live instances explicitly.
#pragma warning disable UDR0001
        public static T Instance { get; private set; }
#pragma warning restore UDR0001

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
            if (makePersistent)
            {
                DontDestroyOnLoad(gameObject);
            }
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
