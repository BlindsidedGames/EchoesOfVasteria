using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Blindsided.UGS
{
    /// <summary>
    /// Ensures Unity Gaming Services are initialized and the player is signed in
    /// before any UGS API is used (Cloud Save, Cloud Code, Leaderboards, etc.).
    /// </summary>
    public static class UgsInitializer
    {
        private static Task _initTask;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit()
        {
            // Fire-and-forget early initialization; callers can still await EnsureInitializedAsync.
            _ = EnsureInitializedAsync();
        }

        public static Task EnsureInitializedAsync()
        {
            // If already created and not faulted/canceled, reuse the task
            if (_initTask == null || _initTask.IsCanceled || _initTask.IsFaulted)
            {
                _initTask = InitializeAndSignInAsync();
            }

            return _initTask;
        }

        private static async Task InitializeAndSignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"UGS signed in. PlayerId={AuthenticationService.Instance.PlayerId}");
            }
        }
    }
}
