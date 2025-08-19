using TimelessEchoes.Buffs;
using TimelessEchoes.Stats;
using UnityEngine;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    /// Keeps the reap line positioned at the current reaping distance.
    /// Attach to an always-present object (e.g. GameManager).
    /// </summary>
    public class ReapLineSystem : MonoBehaviour
    {
        [SerializeField] private Transform reapLine;
        private float cachedDistance;
        private const float CheckInterval = 0.2f; // 5 checks per second

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            cachedDistance = ComputeReapDistance();
            UpdateLine();
            InvokeRepeating(nameof(CheckReapDistance), CheckInterval, CheckInterval);
        }

        private void CheckReapDistance()
        {
            var current = ComputeReapDistance();
            if (!Mathf.Approximately(current, cachedDistance))
            {
                cachedDistance = current;
                UpdateLine();
            }
        }

        private float ComputeReapDistance()
        {
            var tracker = GameplayStatTracker.Instance;
            if (tracker == null) return 0f;

            var buff = BuffManager.Instance;
            var baseDist = tracker.MaxRunDistance;
            if (buff != null)
            {
                baseDist = baseDist * buff.MaxDistanceMultiplier + buff.MaxDistanceFlatBonus;
            }
            return baseDist;
        }

        private void UpdateLine()
        {
            if (reapLine == null) return;
            var pos = reapLine.position;
            pos.x = cachedDistance;
            reapLine.position = pos;
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(CheckReapDistance));
        }
    }
}
