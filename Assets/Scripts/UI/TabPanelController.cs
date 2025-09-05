using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Controls six tabs (General, Rank, Graphs, Enemies, Tasks and Items).
    ///     Selecting a tab enables its objects while disabling the others.
    /// </summary>
    public class TabPanelController : MonoBehaviour
    {
        [SerializeField] private Button generalButton;
        [SerializeField] private List<GameObject> generalObjects = new();

        [SerializeField] private Button rankButton;
        [SerializeField] private List<GameObject> rankObjects = new();

        [SerializeField] private Button graphsButton;
        [SerializeField] private List<GameObject> graphsObjects = new();

        [SerializeField] private Button enemiesButton;
        [SerializeField] private List<GameObject> enemiesObjects = new();

        [SerializeField] private Button tasksButton;
        [SerializeField] private List<GameObject> tasksObjects = new();

        [SerializeField] private Button itemsButton;
        [SerializeField] private List<GameObject> itemsObjects = new();

        private List<GameObject>[] groups;

        private void Awake()
        {
            groups = new[]
            {
                generalObjects,
                rankObjects,
                graphsObjects,
                enemiesObjects,
                tasksObjects,
                itemsObjects
            };

            if (generalButton != null)
                generalButton.onClick.AddListener(ShowGeneral);
            if (rankButton != null)
                rankButton.onClick.AddListener(ShowRank);
            if (graphsButton != null)
                graphsButton.onClick.AddListener(ShowGraphs);
            if (enemiesButton != null)
                enemiesButton.onClick.AddListener(ShowEnemies);
            if (tasksButton != null)
                tasksButton.onClick.AddListener(ShowTasks);
            if (itemsButton != null)
                itemsButton.onClick.AddListener(ShowItems);
        }

        private void OnDestroy()
        {
            if (generalButton != null)
                generalButton.onClick.RemoveListener(ShowGeneral);
            if (rankButton != null)
                rankButton.onClick.RemoveListener(ShowRank);
            if (graphsButton != null)
                graphsButton.onClick.RemoveListener(ShowGraphs);
            if (enemiesButton != null)
                enemiesButton.onClick.RemoveListener(ShowEnemies);
            if (tasksButton != null)
                tasksButton.onClick.RemoveListener(ShowTasks);
            if (itemsButton != null)
                itemsButton.onClick.RemoveListener(ShowItems);
        }

        private void ShowGeneral() => ActivateGroup(0);
        private void ShowRank() => ActivateGroup(1);
        private void ShowGraphs() => ActivateGroup(2);
        private void ShowEnemies() => ActivateGroup(3);
        private void ShowTasks() => ActivateGroup(4);
        private void ShowItems() => ActivateGroup(5);

        private void ActivateGroup(int index)
        {
            if (groups == null) return;

            for (var i = 0; i < groups.Length; i++)
            {
                bool active = i == index;
                foreach (var obj in groups[i])
                    if (obj != null)
                        obj.SetActive(active);
            }
        }
    }
}
