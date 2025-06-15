using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Keeps an enemy’s world-space HP bar in sync with its Health component.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyHealthBar : MonoBehaviour
{
    [Tooltip("Filled Image whose Fill Amount represents HP.")]
    [SerializeField] private Image barFill;
    [Tooltip("TMP element showing the enemy's defense value.")]
    [SerializeField] private TMP_Text defenseText;

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
        if (barFill != null)
            barFill.fillAmount = (float)cur / max;

        if (defenseText != null)
            defenseText.text = hp ? hp.Defense.ToString() : string.Empty;
    }

    private void OnDestroy()
    {
        if (hp != null) hp.OnHealthChanged -= UpdateBar;   // tidy unsubscribe
    }
}