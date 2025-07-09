using UnityEngine;
using TimelessEchoes.Audio;

namespace TimelessEchoes.Tasks
{
    public class MiningTask : ContinuousTask
    {
        protected override string AnimationName => "Mining";
        protected override string InterruptTriggerName => "StopMining";
        protected override AudioManager.TaskType TaskType => AudioManager.TaskType.Mining;

        public override Transform Target => transform;
    }}