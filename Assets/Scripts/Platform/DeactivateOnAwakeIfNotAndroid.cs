using UnityEngine;

namespace TimelessEchoes.Platform
{
    /// <summary>
    /// Disables this GameObject on Awake when not running on Android.
    /// </summary>
    public class DeactivateOnAwakeIfNotAndroid : MonoBehaviour
    {
        private void Awake()
        {
#if !UNITY_ANDROID
            gameObject.SetActive(false);
#endif
        }
    }
}

