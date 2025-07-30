using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Hero;

namespace TimelessEchoes.Enemies
{
    /// <summary>
    /// Manages activation of enemies based on camera visibility.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class EnemyActivator : MonoBehaviour
    {
        [SerializeField] private float activationPadding = 2f;

        private Camera cam;
        private readonly List<Enemy> enemies = new();
        private readonly List<Enemy> activeEnemies = new();

        public static IReadOnlyList<Enemy> ActiveEnemies => Instance?.activeEnemies;

        public static EnemyActivator Instance { get; private set; }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Instance = this;
        }

        public void Register(Enemy enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
                if (!activeEnemies.Contains(enemy))
                    activeEnemies.Add(enemy);
            }
        }

        public void Unregister(Enemy enemy)
        {
            enemies.Remove(enemy);
            activeEnemies.Remove(enemy);
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            Vector3 min = cam.ViewportToWorldPoint(Vector3.zero);
            Vector3 max = cam.ViewportToWorldPoint(Vector3.one);
            min -= Vector3.one * activationPadding;
            max += Vector3.one * activationPadding;

            var hero = HeroController.Instance != null && HeroController.Instance.isActiveAndEnabled
                ? HeroController.Instance
                : null;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (e == null)
                {
                    enemies.RemoveAt(i);
                    activeEnemies.Remove(e);
                    continue;
                }

                Vector3 p = e.transform.position;
                bool inside = p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
                bool nearCombatant = false;

                if (!inside && e.Stats != null)
                {
                    if (hero != null)
                        nearCombatant = Vector2.Distance(hero.transform.position, p) <= e.Stats.visionRange;

                    if (!nearCombatant)
                    {
                        foreach (var echo in EchoController.CombatEchoes)
                        {
                            if (echo == null || !echo.isActiveAndEnabled) continue;
                            if (Vector2.Distance(echo.transform.position, p) <= e.Stats.visionRange)
                            {
                                nearCombatant = true;
                                break;
                            }
                        }
                    }
                }

                bool active = inside || e.IsEngaged || nearCombatant;
                e.SetActiveState(active);
                if (active)
                {
                    if (!activeEnemies.Contains(e))
                        activeEnemies.Add(e);
                }
                else
                {
                    activeEnemies.Remove(e);
                }
            }
        }
    }
}
