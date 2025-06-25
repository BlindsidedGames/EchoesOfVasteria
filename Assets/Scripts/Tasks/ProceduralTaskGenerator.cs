using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Generates tasks procedurally within a rectangular area. The order of
    ///     generated tasks is determined by their world X position from left to
    ///     right.
    /// </summary>
    [RequireComponent(typeof(TaskController))]
    public class ProceduralTaskGenerator : MonoBehaviour
    {
        [TabGroup("Settings", "Area")] [SerializeField]
        private float minX;

        [TabGroup("Settings", "Area")] [SerializeField]
        private float maxX = 990f;

        [TabGroup("Settings", "Area")] [SerializeField]
        private float height = 18f;

        [TabGroup("Settings", "Area")] [SerializeField]
        private float density = 0.1f;

        [TabGroup("Settings", "Generation")] [SerializeField]
        private LayerMask blockingMask;

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> enemies = new();

        [TabGroup("Settings", "Generation")] [SerializeField]
        private List<WeightedSpawn> otherTasks = new();

        private readonly List<GameObject> generatedObjects = new();

        private TaskController controller;

        private void Awake()
        {
            controller = GetComponent<TaskController>();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var center = transform.position + new Vector3((minX + maxX) * 0.5f, height * 0.5f, 0f);
            var size = new Vector3(maxX - minX, height, 0f);
            Gizmos.DrawWireCube(center, size);
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
        ///     Clear all spawned task objects and remove them from the controller.
        /// </summary>
        public void Clear()
        {
            if (controller == null)
                controller = GetComponent<TaskController>();

            ClearSpawnedObjects();
            controller?.ClearTaskObjects();
        }

        /// <summary>
        ///     Generate and assign tasks based on the configured settings.
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

            var count = Mathf.RoundToInt((maxX - minX) * density);
            if (count <= 0)
                return;

            var spawned = new List<(float x, MonoBehaviour obj)>();
            for (var i = 0; i < count; i++)
            {
                var pos = RandomPosition();
                var attempts = 0;
                while (attempts < 5 && (HasBlockingCollider(pos) || IsBlockedAhead(pos)))
                {
                    pos = RandomPosition();
                    attempts++;
                }

                if (HasBlockingCollider(pos) || IsBlockedAhead(pos))
                    continue;

                var progress = Mathf.InverseLerp(minX, maxX, pos.x);
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
            var x = Random.Range(minX, maxX);
            var y = Random.Range(0f, height);

            var worldX = transform.position.x + x;
            var worldY = transform.position.y + y;

            return new Vector3(worldX, worldY, 0f);
        }

        /// <summary>
        ///     Determine if the exact XY position contains a collider on the blocking mask.
        /// </summary>
        private bool HasBlockingCollider(Vector2 point)
        {
            return Physics2D.OverlapPoint(point, blockingMask) != null;
        }

        /// <summary>
        ///     Check if there is a blocking collider directly in front of the given position.
        /// </summary>
        private bool IsBlockedAhead(Vector3 pos)
        {
            const float checkRadius = 0.4f;
            var hits = Physics2D.OverlapCircleAll(pos, checkRadius, blockingMask);
            foreach (var h in hits)
                if (h.bounds.min.y > pos.y - 0.1f)
                    return true;
            return false;
        }

        private WeightedSpawn PickEntry(float progress)
        {
            var total = 0f;
            foreach (var e in enemies)
                total += e.GetWeight(progress);
            foreach (var t in otherTasks)
                total += t.GetWeight(progress);
            if (total <= 0f)
                return null;
            var r = Random.value * total;
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

        [Serializable]
        [InlineProperty]
        [HideLabel]
        public class WeightedSpawn
        {
            [Required] public GameObject prefab;

            [MinValue(0)] public float weight = 1f;

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
    }
}