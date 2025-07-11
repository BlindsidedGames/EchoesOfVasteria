using UnityEngine;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    /// Standard health component for enemies.
    /// </summary>
    public class Health : HealthBase
    {
        protected override Color GetFloatingTextColor()
        {
            ColorUtility.TryParseHtmlString("#C69B60", out var orange);
            return orange;
        }

        protected override float GetFloatingTextSize() => 8f;

        protected override void OnZeroHealth()
        {
            base.OnZeroHealth();
            if (GetComponent<Enemy>() != null)
                Destroy(gameObject);
        }
    }
}
