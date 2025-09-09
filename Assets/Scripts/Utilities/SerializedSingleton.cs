using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Odin-friendly singleton base for SerializedMonoBehaviour inheritors.
    /// Matches Singleton<T> behavior, with optional persistence.
    /// </summary>
    /// <typeparam name="T">Type deriving from SerializedMonoBehaviour.</typeparam>
    public class SerializedSingleton<T> : Sirenix.OdinInspector.SerializedMonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        [SerializeField] private bool makePersistent = false;

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

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

