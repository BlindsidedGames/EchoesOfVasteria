using TimelessEchoes.Hero;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TimelessEchoes.Tasks
{
    /// <summary>
    /// Allows the player to click and hold on a task to manually advance
    /// its progress.
    /// </summary>
    public class ManualTaskProgress : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private MonoBehaviour taskObject;

        private ITask task;
        private bool held;
        private bool arrived;

        private void Awake()
        {
            if (taskObject == null)
                taskObject = GetComponent<MonoBehaviour>();
            task = taskObject as ITask ?? taskObject?.GetComponent<ITask>();
        }

        private void OnDisable()
        {
            held = false;
            arrived = false;
        }

        private void Update()
        {
            if (!held || task == null)
                return;

            var hero = HeroController.Instance ?? FindFirstObjectByType<HeroController>();
            if (hero == null)
                return;

            if (!arrived)
            {
                task.OnArrival(hero);
                arrived = true;
            }

            task.Tick(hero);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            held = true;
            arrived = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            held = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            held = false;
        }
    }
}
