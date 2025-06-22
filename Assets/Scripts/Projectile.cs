using System.Collections.Generic;
using UnityEngine;
using Blindsided.Utilities.Pooling;
using TowerDefense;

namespace AutoBattler
{
    /// <summary>
    /// Simple projectile that seeks a target and applies damage on contact.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private static readonly HashSet<Projectile> active = new();

        /// <summary>
        /// All active projectiles in the scene.
        /// </summary>
        public static IEnumerable<Projectile> Active => active;
        [SerializeField] private float speed = 5f;
        [SerializeField] private float hitDistance = 0.1f;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float effectDuration = 0.5f;

        private Transform target;
        private float damage;

        private void OnEnable()
        {
            active.Add(this);
        }

        private void OnDisable()
        {
            active.Remove(this);
        }

        private GameObject effectPrefab;

        public void Init(Transform target, float damage,
            GameObject hitEffect = null)
        {
            this.target = target;
            this.damage = damage;
            effectPrefab = hitEffect ?? hitEffectPrefab;
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            IHasHealth health = target.GetComponent<IHasHealth>();
            if (health != null && health.CurrentHealth <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 dir = target.position - transform.position;
            // Rotate only around the Z axis so slight vertical offsets between
            // the fire point and target don't cause unwanted tilting.
            if (dir.sqrMagnitude > 0f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            float distanceThisFrame = speed * Time.deltaTime;

            if (dir.magnitude <= hitDistance)
            {
                var dmg = target.GetComponent<IDamageable>();
                dmg?.TakeDamage(damage);
                SpawnEffect();
                Destroy(gameObject);
                return;
            }

            transform.position += dir.normalized * distanceThisFrame;
        }

        private void SpawnEffect()
        {
            if (effectPrefab == null)
                return;

            var obj = PoolManager.Get(effectPrefab);
            obj.transform.position = transform.position;
            obj.transform.rotation = Quaternion.identity;
            var auto = obj.GetComponent<PoolAutoReturn>() ?? obj.AddComponent<PoolAutoReturn>();
            auto.ReturnAfter(effectDuration);
        }
    }
}
