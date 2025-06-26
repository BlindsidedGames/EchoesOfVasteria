using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Upgrades;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for opening a chest in the scene.
    /// </summary>
    public class OpenChestTask : BaseTask
    {
        [SerializeField] private Transform chest;
        [SerializeField] private Animator chestAnimator;
        [SerializeField] private Transform openPoint;
        [SerializeField] private List<ResourceDrop> resourceDrops = new();

        private Transform cachedTarget;
        private ResourceManager resourceManager;
        private bool opened;

        public IList<ResourceDrop> Drops => resourceDrops;

        public override Transform Target
        {
            get
            {
                if (cachedTarget != null)
                    return cachedTarget;

                cachedTarget = openPoint != null ? openPoint : transform;
                return cachedTarget;
            }
        }

        public override void StartTask()
        {
            opened = false;
            cachedTarget = null;
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        public void Open()
        {
            if (opened) return;
            if (chestAnimator != null)
                chestAnimator.SetTrigger("Open");
            StartCoroutine(DelayedLoot());
        }

        private IEnumerator DelayedLoot()
        {
            yield return new WaitForSeconds(0.5f);
            GiveLoot();
            opened = true;
        }

        private void GiveLoot()
        {
            if (resourceManager == null)
                resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceManager == null) return;
            foreach (var drop in resourceDrops)
            {
                if (drop.resource == null) continue;
                if (Random.value > drop.dropChance) continue;

                var min = drop.dropRange.x;
                var max = drop.dropRange.y;
                if (max < min) max = min;
                var t = Random.value;
                t *= t;
                var count = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(min, max + 1, t)), min, max);
                if (count > 0)
                {
                    resourceManager.Add(drop.resource, count);
                    FloatingText.Spawn($"{drop.resource.name} x{count}", transform.position + Vector3.up, Color.blue);
                }
            }
        }


        public override bool IsComplete()
        {
            return opened;
        }
    }
}
