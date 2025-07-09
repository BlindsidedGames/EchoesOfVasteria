using UnityEngine;

namespace TimelessEchoes.Tasks
{
    public class MiningTask : ContinuousTask
    {
        protected override string AnimationName => "Mining";
        protected override string InterruptTriggerName => "StopMining";

        public override Transform Target => transform;    }}