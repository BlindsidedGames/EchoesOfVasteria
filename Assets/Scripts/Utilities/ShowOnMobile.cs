using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Ensures this GameObject is only visible on mobile platforms (Android or iOS).
    /// Deactivates the GameObject on non-mobile platforms during Awake.
    ///
    /// Editor behavior:
    /// - Controlled by <c>activeInEditor</c> (default true). If set to false, the
    ///   same deactivation rule applies while in the Unity Editor.
    /// </summary>
    public class ShowOnMobile : MonoBehaviour
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
                if (!Application.isMobilePlatform)
                {
                    gameObject.SetActive(false);
                }
            }
#else
            if (!Application.isMobilePlatform)
            {
                gameObject.SetActive(false);
            }
#endif
        }
    }
}

