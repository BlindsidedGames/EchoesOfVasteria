using UnityEngine;
using TimelessEchoes.Audio;

namespace TimelessEchoes.Tasks
{
    public class FishingTask : ContinuousTask
    {
        [SerializeField] private Transform fishingPoint;
        public override Transform Target => fishingPoint != null ? fishingPoint : transform;

        protected override string AnimationName => "Fishing";
        protected override string InterruptTriggerName => "CatchFish";
        protected override AudioManager.TaskType TaskType => AudioManager.TaskType.Fishing;
    }}