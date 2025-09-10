// <copyright file="AchievementsButton.cs" company="Jan Ivar Z. Carlsen, Sindri Jóelsson">
// Copyright (c) 2016 Jan Ivar Z. Carlsen, Sindri Jóelsson. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace CloudOnce.QuickStart
{
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Attach this to your Achievements GUI button.
    /// </summary>
    [AddComponentMenu("CloudOnce/Show Achievements Button", 3)]
    public class AchievementsButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        private bool isShowingOverlay;

        private const float iOSReenableDelaySeconds = 0.5f;

        private static void OnSignedInChanged(bool isSignedIn)
        {
            Cloud.OnSignedInChanged -= OnSignedInChanged;
            if (isSignedIn)
            {
                Cloud.Achievements.ShowOverlay();
            }
        }

        private static void SubscribeEvent()
        {
            Cloud.OnSignedInChanged -= OnSignedInChanged;
            Cloud.OnSignedInChanged += OnSignedInChanged;
        }

        private void OnButtonClicked()
        {
            if (isShowingOverlay) return;

            if (!Cloud.IsSignedIn)
            {
#if CLOUDONCE_DEBUG
                Debug.Log("[AchievementsButton] User not signed in. Subscribing and initiating sign-in.");
#endif
                SubscribeEvent();
                Cloud.SignIn();
                return;
            }

#if CLOUDONCE_DEBUG
            Debug.Log("[AchievementsButton] Queuing achievements overlay (end of frame).");
#endif
            StartCoroutine(ShowOverlayDeferred());
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
            if (button == null)
            {
                Debug.LogError("Show Achievements Button script placed on GameObject that is not a button." +
                               " Script is only compatible with UI buttons created from GameObject menu (GameObjects -> UI -> Button).");
            }
        }

        private void Start()
        {
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
            Cloud.OnSignedInChanged -= OnSignedInChanged;
        }

        private System.Collections.IEnumerator ShowOverlayDeferred()
        {
            isShowingOverlay = true;
            if (button != null) button.interactable = false;
            // Defer to end of frame to avoid UI event conflicts.
            yield return new UnityEngine.WaitForEndOfFrame();

#if CLOUDONCE_DEBUG
            UnityEngine.Debug.Log("[AchievementsButton] Showing achievements overlay now.");
#endif
            Cloud.Achievements.ShowOverlay();

            // Platform-specific re-enable strategy.
#if UNITY_ANDROID
            // We don't have a direct callback here (handled inside CloudOnce/GPGS),
            // so use a modest timeout to re-enable input even if overlay fails to appear.
            yield return new UnityEngine.WaitForSecondsRealtime(0.25f);
#elif UNITY_IOS || UNITY_TVOS
            // Game Center UI has no callback in this wrapper; allow brief delay.
            yield return new UnityEngine.WaitForSecondsRealtime(iOSReenableDelaySeconds);
#else
            yield return null;
#endif

            if (button != null) button.interactable = true;
            isShowingOverlay = false;

#if CLOUDONCE_DEBUG
            UnityEngine.Debug.Log("[AchievementsButton] Overlay flow finished; button re-enabled.");
#endif
        }
    }
}
