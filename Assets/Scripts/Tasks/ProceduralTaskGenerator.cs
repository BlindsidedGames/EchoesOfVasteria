using System.Collections.Generic;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Generates tasks procedurally within a rectangular area. The order of
    /// generated tasks is determined by their world X position from left to
    /// right.
    /// </summary>
    [RequireComponent(typeof(TaskController))]
    public class ProceduralTaskGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class WeightedSpawn
        {
            public GameObject prefab;
            public float weight = 1f;
            [Range(0f, 1f)] public float minProgress;
            [Range(0f, 1f)] public float maxProgress = 1f;

            public float GetWeight(float progress)
            {
                if (prefab == null) return 0f;
                if (progress < minProgress || progress > maxProgress)
                    return 0f;
                return Mathf.Max(0f, weight);
            }
        }

        [SerializeField] private float minX;
        [SerializeField] private float maxX = 990f;
        [SerializeField] private float height = 18f;
        [SerializeField] private float density = 0.1f;
        [SerializeField] private LayerMask blockingMask;
        [SerializeField] private List<WeightedSpawn> enemies = new();
        [SerializeField] private List<WeightedSpawn> otherTasks = new();

        private TaskController controller;

        private void Awake()
        {
            controller = GetComponent<TaskController>();
            Generate();
        }

        /// <summary>
        /// Generate and assign tasks based on the configured settings.
        /// </summary>
        public void Generate()
        {
            if (controller == null)
                return;

            controller.ClearTaskObjects();

            int count = Mathf.RoundToInt((maxX - minX) * density);
            if (count <= 0)
                return;

            var spawned = new List<(float x, MonoBehaviour obj)>();
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = RandomPosition();
                int attempts = 0;
                while (attempts < 5 && Physics2D.OverlapPoint(pos, blockingMask))
                {
                    pos = RandomPosition();
                    attempts++;
                }
                if (Physics2D.OverlapPoint(pos, blockingMask))
                    continue;

                float progress = Mathf.InverseLerp(minX, maxX, pos.x);
                var entry = PickEntry(progress);
                if (entry == null || entry.prefab == null)
                    continue;

                var obj = Instantiate(entry.prefab, pos, Quaternion.identity, transform);
                var mono = obj.GetComponent<MonoBehaviour>();
                if (mono != null)
                    spawned.Add((pos.x, mono));
            }

            spawned.Sort((a, b) => a.x.CompareTo(b.x));
            foreach (var pair in spawned)
                controller.AddTaskObject(pair.obj);

            controller.ResetTasks();
        }

        private Vector3 RandomPosition()
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(0f, height);
            return new Vector3(x, y, 0f) + transform.position;
        }

        private WeightedSpawn PickEntry(float progress)
        {
            float total = 0f;
            foreach (var e in enemies)
                total += e.GetWeight(progress);
            foreach (var t in otherTasks)
                total += t.GetWeight(progress);
            if (total <= 0f)
                return null;
            float r = Random.value * total;
            foreach (var e in enemies)
            {
                r -= e.GetWeight(progress);
                if (r <= 0f)
                    return e;
            }
            foreach (var t in otherTasks)
            {
                r -= t.GetWeight(progress);
                if (r <= 0f)
                    return t;
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + new Vector3((minX + maxX) * 0.5f, height * 0.5f, 0f);
            var size = new Vector3(maxX - minX, height, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
