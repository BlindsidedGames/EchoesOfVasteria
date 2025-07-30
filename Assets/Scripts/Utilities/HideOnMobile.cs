using UnityEngine;

namespace TimelessEchoes.Utilities
{
    /// <summary>
    /// Disables the GameObject on mobile platforms (Android or iOS).
    /// Attach this script to objects that should be hidden on mobile devices.
    /// </summary>
    public class HideOnMobile : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.isMobilePlatform)
                gameObject.SetActive(false);
        }
    }
}
