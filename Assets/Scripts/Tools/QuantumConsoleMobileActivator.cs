using QFSW.QC;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Automatically instantiates at startup and opens the Quantum Console on mobile
    /// when three fingers are held on the screen for a set duration.
    /// No manual scene setup is required.
    /// </summary>
    public class QuantumConsoleMobileActivator : MonoBehaviour
    {
        [SerializeField] private float holdDuration = 2f;
        private float _touchTimer;
        private QuantumConsole _console;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject(nameof(QuantumConsoleMobileActivator));
            DontDestroyOnLoad(go);
            go.AddComponent<QuantumConsoleMobileActivator>();
        }

        private void Awake()
        {
            _console = FindFirstObjectByType<QuantumConsole>(FindObjectsInactive.Include);
        }

        private void Update()
        {
            if (!Application.isMobilePlatform)
            {
                return;
            }

            if (_console == null)
            {
                _console = FindFirstObjectByType<QuantumConsole>(FindObjectsInactive.Include);
                if (_console == null)
                {
                    return;
                }
            }

            if (Input.touchCount >= 3)
            {
                bool allHeld = true;
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var phase = Input.touches[i].phase;
                    if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled)
                    {
                        allHeld = false;
                        break;
                    }
                }

                if (allHeld)
                {
                    _touchTimer += Time.unscaledDeltaTime;
                    if (_touchTimer >= holdDuration)
                    {
                        _console.Toggle();
                        _touchTimer = 0f;
                    }
                    return;
                }
            }

            _touchTimer = 0f;
        }
    }
}
