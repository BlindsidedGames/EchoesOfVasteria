using System.Collections.Generic;
using Sirenix.OdinInspector;
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
        [InlineProperty]
        [HideLabel]
        public class WeightedSpawn
        {
            [Required]
            [HorizontalGroup("Split", 70)]
            public GameObject prefab;

            [HorizontalGroup("Split")]
            [LabelWidth(50)]
            [MinValue(0)]
            public float weight = 1f;

            [HorizontalGroup("Split")]
            [LabelWidth(50)]
            [Range(0f, 1f)]
            public float minProgress;

            [HorizontalGroup("Split")]
            [LabelWidth(50)]
            [Range(0f, 1f)]
            public float maxProgress = 1f;

            public float GetWeight(float progress)
            {
                if (prefab == null) return 0f;
                if (progress < minProgress || progress > maxProgress)
                    return 0f;
                return Mathf.Max(0f, weight);
            }
        }

        [TabGroup("Settings", "Area")]
        [SerializeField] private float minX;
        [TabGroup("Settings", "Area")]
        [SerializeField] private float maxX = 990f;
        [TabGroup("Settings", "Area")]
        [SerializeField] private float height = 18f;
        [TabGroup("Settings", "Area")]
        [SerializeField] private float density = 0.1f;

        [TabGroup("Settings", "Generation")]
        [SerializeField] private LayerMask blockingMask;

        [TabGroup("Settings", "Generation")]
        [SerializeField] private List<WeightedSpawn> enemies = new();

        [TabGroup("Settings", "Generation")]
        [SerializeField] private List<WeightedSpawn> otherTasks = new();

        private TaskController controller;
        private readonly List<GameObject> generatedObjects = new();

        private void Awake()
        {
            controller = GetComponent<TaskController>();
        }

        private void ClearSpawnedObjects()
        {
            foreach (var obj in generatedObjects)
            {
                if (obj == null) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(obj);
                else
#endif
                    Destroy(obj);
            }
            generatedObjects.Clear();
        }

        /// <summary>
        /// Clear all spawned task objects and remove them from the controller.
        /// </summary>
        public void Clear()
        {
            if (controller == null)
                controller = GetComponent<TaskController>();

            ClearSpawnedObjects();
            controller?.ClearTaskObjects();
        }

        /// <summary>
        /// Generate and assign tasks based on the configured settings.
        /// </summary>
        [Button]
        public void Generate()
        {
            if (controller == null)
                controller = GetComponent<TaskController>();
            if (controller == null)
                return;

            ClearSpawnedObjects();
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
                generatedObjects.Add(obj);
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
            float worldX = transform.position.x + x;
            float castStartY = transform.position.y + height + 1f;
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldX, castStartY), Vector2.down, height + 2f, blockingMask);

            float worldY = hit.collider != null
                ? hit.point.y + 0.1f
                : transform.position.y + Random.Range(0f, height);

            return new Vector3(worldX, worldY, 0f);
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
