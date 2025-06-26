using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    ///     Task for fishing at a single location.
    ///     The hero handles movement and timing while this task tracks
    ///     completion status.
    /// </summary>
    public class FishingTask : BaseTask
    {
        [SerializeField] private float fishTime = 2f;
        [SerializeField] private SlicedFilledImage progressBar;
        [SerializeField] private Transform fishingPoint;

        private bool complete;

        public float FishTime => fishTime;
        public SlicedFilledImage ProgressBar => progressBar;

        public override Transform Target => fishingPoint != null ? fishingPoint : transform;

        public override void StartTask()
        {
            complete = false;
            if (progressBar != null)
                progressBar.fillAmount = 1f;
        }

        public override bool IsComplete()
        {
            return complete;
        }

        public void CompleteTask()
        {
            if (complete) return;
            complete = true;
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
        }
    }
}
