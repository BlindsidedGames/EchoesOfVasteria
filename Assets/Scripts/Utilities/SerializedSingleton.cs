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
        // Unity's analyzer cannot attach a runtime initializer to an open generic
        // type. Destroyed Unity objects already compare as null, and OnDestroy
        // clears live instances explicitly.
#pragma warning disable UDR0001
        public static T Instance { get; private set; }
#pragma warning restore UDR0001

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

