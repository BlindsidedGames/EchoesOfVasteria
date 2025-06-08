using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps an enemy’s world-space HP bar in sync with its Health component.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Filled Image whose Fill Amount represents HP.")]
    [SerializeField] private Image barFill;

    private Health  hp;
    private Camera  cam;

    private void Awake()
    {
        hp  = GetComponent<Health>();
        cam = Camera.main;

        hp.OnHealthChanged += UpdateBar;       // initial hookup
        UpdateBar(hp.CurrentHP, hp.MaxHP);     // push first value
    }

    private void Update()
    {
        // Keep the bar facing the camera (optional)
        if (barFill && barFill.canvas.renderMode == RenderMode.WorldSpace)
            barFill.canvas.transform.rotation = cam.transform.rotation;
    }

    private void UpdateBar(int cur, int max)
    {
        if (barFill == null) return;
        barFill.fillAmount = (float)cur / max;
    }

    private void OnDestroy()
    {
        if (hp != null) hp.OnHealthChanged -= UpdateBar;   // tidy unsubscribe
    }
}