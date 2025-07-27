using TimelessEchoes.Hero;
using UnityEngine;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Allows the player to click on a task's collider and hold the mouse
    /// button to manually progress the task.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ColliderTaskProgress : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour taskObject;

        private ITask task;
        private bool held;
        private bool arrived;
        private HeroController hero;

        private void Awake()
        {
            if (taskObject == null)
                taskObject = GetComponent<MonoBehaviour>();
            task = taskObject as ITask ?? taskObject?.GetComponent<ITask>();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Update()
        {
            if (!held || task == null)
                return;

            if (hero == null)
                hero = HeroController.Instance ?? FindFirstObjectByType<HeroController>();
            if (hero == null)
                return;

            if (task is BaseTask baseTask)
                baseTask.Claim(hero);

            if (!arrived)
            {
                task.OnArrival(hero);
                arrived = true;
            }

            task.Tick(hero);
        }

        private void OnMouseDown()
        {
            held = true;
            arrived = false;
            hero = null;
        }

        private void OnMouseUp()
        {
            Release();
        }

        private void Release()
        {
            if (held && task is BaseTask baseTask && hero != null)
                baseTask.ReleaseClaim(hero);

            held = false;
            arrived = false;
        }
    }
}

