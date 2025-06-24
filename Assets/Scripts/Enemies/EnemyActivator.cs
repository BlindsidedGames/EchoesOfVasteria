using System.Collections.Generic;
using UnityEngine;

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

        public static EnemyActivator Instance { get; private set; }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Instance = this;
        }

        public void Register(Enemy enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void Unregister(Enemy enemy)
        {
            enemies.Remove(enemy);
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            Vector3 min = cam.ViewportToWorldPoint(Vector3.zero);
            Vector3 max = cam.ViewportToWorldPoint(Vector3.one);
            min -= Vector3.one * activationPadding;
            max += Vector3.one * activationPadding;

            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (e == null)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                Vector3 p = e.transform.position;
                bool inside = p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
                e.SetActiveState(inside || e.IsEngaged);
            }
        }
    }
}
