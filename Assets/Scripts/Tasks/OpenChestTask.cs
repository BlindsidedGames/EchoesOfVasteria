using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Task for opening a chest in the scene.
    /// </summary>
    public class OpenChestTask : MonoBehaviour, ITask
    {
        [SerializeField] private Transform chest;
        private bool opened;

        public Transform Target => chest;

        public void StartTask()
        {
            opened = false;
        }

        public void Open()
        {
            opened = true;
        }

        public bool IsComplete()
        {
            return opened;
        }
    }
}
