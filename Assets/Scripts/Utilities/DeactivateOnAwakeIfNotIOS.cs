using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Deactivates this GameObject on all platforms except iOS/iPadOS.
    /// Attach to objects that should only be active on iOS devices.
    ///
    /// Behavior:
    /// - In Player: Deactivates during Awake when the runtime platform is not iOS (iPhone/iPad).
    /// - In Editor: Controlled by <c>activeInEditor</c> (default true). If set to false, it deactivates when the
    ///   current runtime platform is not iOS.
    /// </summary>
    public class DeactivateOnAwakeIfNotIOS : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool activeInEditor = true;
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            if (!activeInEditor)
            {
                if (Application.platform != RuntimePlatform.IPhonePlayer)
                {
                    gameObject.SetActive(false);
                }
            }
#else
            if (Application.platform != RuntimePlatform.IPhonePlayer)
            {
                gameObject.SetActive(false);
            }
#endif
        }
    }
}

