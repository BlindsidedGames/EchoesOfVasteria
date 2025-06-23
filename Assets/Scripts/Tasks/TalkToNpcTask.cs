using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for interacting with a specific NPC.
    /// </summary>
    public class TalkToNpcTask : MonoBehaviour, ITask
    {
        [SerializeField] private Transform npc;
        private bool talked;

        public Transform Target => npc;

        public void StartTask()
        {
            talked = false;
        }

        public void Interact()
        {
            talked = true;
        }

        public bool IsComplete()
        {
            return talked;
        }
    }
}
