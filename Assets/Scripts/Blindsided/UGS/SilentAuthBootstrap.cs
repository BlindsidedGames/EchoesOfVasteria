using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Blindsided.UGS
{
    public class SilentAuthBootstrap : MonoBehaviour
    {
        async void Awake()
        {
            await TimelessEchoes.Platform.StartupDiagnostics.WaitForSelection();
            if (TimelessEchoes.Platform.StartupDiagnostics.SkipOnlineServices) return;
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"Signed in (anon). PlayerId={AuthenticationService.Instance.PlayerId}");
        }
    }
}
