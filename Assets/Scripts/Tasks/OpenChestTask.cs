using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for opening a chest in the scene.
    /// </summary>
    public class OpenChestTask : BaseTask
    {
        [SerializeField] private Transform chest;
        private bool opened;

        public override Transform Target => chest;

        public override void StartTask()
        {
            opened = false;
        }

        public void Open()
        {
            opened = true;
        }

        public override bool IsComplete()
        {
            return opened;
        }
    }
}
