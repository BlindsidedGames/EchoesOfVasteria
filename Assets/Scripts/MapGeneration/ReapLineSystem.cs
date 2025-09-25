using TimelessEchoes.Buffs;
using TimelessEchoes.Stats;
using TimelessEchoes;
using UnityEngine;

namespace TimelessEchoes.MapGeneration
{
    /// <summary>
    ///     Keeps the reap line positioned at the current reaping distance.
    ///     Attach to an always-present object (e.g. GameManager).
    /// </summary>
    public class ReapLineSystem : MonoBehaviour
    {
        [SerializeField] private Transform reapLine;
        private float cachedDistance;
        private const float CheckInterval = 0.2f; // 5 checks per second

        private void Start()
        {
            UpdateReapLineState(true);
            InvokeRepeating(nameof(CheckReapDistance), CheckInterval, CheckInterval);
        }

        private void CheckReapDistance()
        {
            UpdateReapLineState();
        }

        private void UpdateReapLineState(bool forceUpdate = false)
        {
            var killModeActive = IsKillScalingActive();
            if (reapLine != null)
                reapLine.gameObject.SetActive(!killModeActive);

            if (killModeActive)
                return;

            var current = ComputeReapDistance();
            if (forceUpdate || !Mathf.Approximately(current, cachedDistance))
            {
                cachedDistance = current;
                UpdateLine();
            }
        }

        private static bool IsKillScalingActive()
        {
            var manager = GameManager.Instance;
            return manager != null && manager.IsKillScalingMode;
        }

        private float ComputeReapDistance()
        {
            var tracker = GameplayStatTracker.Instance;
            if (tracker == null) return 0f;

            var buff = BuffManager.Instance;
            var baseDist = tracker.MaxRunDistance;
            var buffed = baseDist;
            if (buff != null) buffed = baseDist * buff.MaxDistanceMultiplier + buff.MaxDistanceFlatBonus;

            var oc = Blindsided.Oracle.oracle;
            var isDemo = oc != null && oc.demo;
            return isDemo ? Mathf.Min(buffed, 300f) : buffed;
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
