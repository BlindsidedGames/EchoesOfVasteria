using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for interacting with a specific NPC.
    /// </summary>
    public class TalkToNpcTask : BaseTask
    {
        [SerializeField] private Transform npc;
        private bool talked;

        public override Transform Target => npc;

        public override void StartTask()
        {
            talked = false;
        }

        public void Interact()
        {
            talked = true;
        }

        public override bool IsComplete()
        {
            return talked;
        }
    }
}
