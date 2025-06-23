using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Simple global registry for tracking targets like enemies.
    /// </summary>
    public class TargetRegistry : MonoBehaviour, ITargetRegistry
    {
        private static TargetRegistry instance;
        public static TargetRegistry Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<TargetRegistry>();
                return instance;
            }
        }

        private readonly HashSet<Transform> targets = new();

        public void Register(Transform target)
        {
            if (target != null)
                targets.Add(target);
        }

        public void Unregister(Transform target)
        {
            targets.Remove(target);
        }

        public Transform FindClosest(Vector3 position, LayerMask mask, Transform ignore = null)
        {
            Transform closest = null;
            float best = float.MaxValue;
            foreach (var t in GetTargets(mask))
            {
                if (t == ignore) continue;
                float d = Vector3.Distance(position, t.position);
                if (d < best)
                {
                    best = d;
                    closest = t;
                }
            }
            return closest;
        }

        public IEnumerable<Transform> GetTargets(LayerMask mask)
        {
            foreach (var t in targets.ToList())
            {
                if (t == null)
                {
                    targets.Remove(t);
                    continue;
                }
                if (((1 << t.gameObject.layer) & mask) != 0)
                    yield return t;
            }
        }
    }
}
