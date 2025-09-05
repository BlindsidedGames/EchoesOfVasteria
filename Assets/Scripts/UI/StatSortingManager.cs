using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    ///     Instantiates and manages sorting buttons based on the active stats tab.
    /// </summary>
    public class StatSortingManager : MonoBehaviour
    {
        [SerializeField] private Transform buttonParent;
        [SerializeField] private StatSortButton buttonPrefab;

        [SerializeField] private Button generalButton;
        [SerializeField] private Button graphsButton;
        [SerializeField] private Button enemiesButton;
        [SerializeField] private Button tasksButton;
        [SerializeField] private Button itemsButton;

        [SerializeField] private EnemyStatsPanelUI enemyPanel;
        [SerializeField] private TaskStatsPanelUI taskPanel;
        [SerializeField] private ItemStatsPanelUI itemPanel;
        [SerializeField] private RunStatsPanelUI runStatsPanel;
        [SerializeField] private GameObject enemyDistanceControls; // Slider GameObject to toggle with Enemies tab

        private readonly List<StatSortButton> buttons = new();
        private readonly Dictionary<StatSortButton, Enum> buttonModes = new();

        private int currentTab = -1;
        private EnemyStatsPanelUI.SortMode enemyMode = EnemyStatsPanelUI.SortMode.Default;
        private TaskStatsPanelUI.SortMode taskMode = TaskStatsPanelUI.SortMode.Default;
        private ItemStatsPanelUI.SortMode itemMode = ItemStatsPanelUI.SortMode.Default;
        private RunStatsPanelUI.GraphMode runMode = RunStatsPanelUI.GraphMode.Distance;

        private void Awake()
        {
            if (generalButton != null)
                generalButton.onClick.AddListener(ShowGeneral);
            if (graphsButton != null)
                graphsButton.onClick.AddListener(ShowGraphs);
            if (enemiesButton != null)
                enemiesButton.onClick.AddListener(ShowEnemies);
            if (tasksButton != null)
                tasksButton.onClick.AddListener(ShowTasks);
            if (itemsButton != null)
                itemsButton.onClick.AddListener(ShowItems);
        }

        private void Start()
        {
            if (runStatsPanel != null)
                runStatsPanel.SetGraphMode(runMode);
            SetTab(0);
        }

        private void OnDestroy()
        {
            if (generalButton != null)
                generalButton.onClick.RemoveListener(ShowGeneral);
            if (graphsButton != null)
                graphsButton.onClick.RemoveListener(ShowGraphs);
            if (enemiesButton != null)
                enemiesButton.onClick.RemoveListener(ShowEnemies);
            if (tasksButton != null)
                tasksButton.onClick.RemoveListener(ShowTasks);
            if (itemsButton != null)
                itemsButton.onClick.RemoveListener(ShowItems);
        }

        private void ShowGeneral()
        {
            SetTab(0);
        }

        private void ShowGraphs()
        {
            SetTab(1);
        }

        private void ShowEnemies()
        {
            SetTab(2);
        }

        private void ShowTasks()
        {
            SetTab(3);
        }

        private void ShowItems()
        {
            SetTab(4);
        }

        private void SetTab(int tab)
        {
            if (currentTab == tab) return;
            currentTab = tab;
            BuildButtons();
            // Toggle the distance slider controls based on active tab
            if (enemyDistanceControls != null)
                enemyDistanceControls.SetActive(currentTab == 2);
        }

        private void BuildButtons()
        {
            ClearButtons();
            switch (currentTab)
            {
                case 0:
                    // General tab has no sorting buttons
                    break;
                case 1:
                    BuildGraphButtons();
                    break;
                case 2:
                    BuildEnemyButtons();
                    break;
                case 3:
                    BuildTaskButtons();
                    break;
                case 4:
                    BuildItemButtons();
                    break;
            }
        }

        private void ClearButtons()
        {
            foreach (var b in buttons)
                if (b != null)
                    Destroy(b.gameObject);
            buttons.Clear();
            buttonModes.Clear();
        }

        private void BuildEnemyButtons()
        {
            // Order buttons by unlock order of stats
            var order = new Dictionary<EnemyStatsPanelUI.SortMode, int>
            {
                { EnemyStatsPanelUI.SortMode.Default, 0 },
                { EnemyStatsPanelUI.SortMode.Damage, 1 },
                { EnemyStatsPanelUI.SortMode.Health, 2 },
                { EnemyStatsPanelUI.SortMode.Defense, 3 },
                { EnemyStatsPanelUI.SortMode.AttackRate, 4 },
                { EnemyStatsPanelUI.SortMode.MoveSpeed, 5 },
                { EnemyStatsPanelUI.SortMode.Vision, 6 },
            };

            var modes = (EnemyStatsPanelUI.SortMode[])Enum.GetValues(typeof(EnemyStatsPanelUI.SortMode));
            Array.Sort(modes, (a, b) => order[a].CompareTo(order[b]));

            foreach (var mode in modes)
            {
                var captured = mode;
                CreateButton(mode, () =>
                {
                    enemyMode = captured;
                    if (enemyPanel != null) enemyPanel.SetSortMode(captured);
                    UpdateButtonStates();
                });
            }
            UpdateButtonStates();
        }

        private void BuildTaskButtons()
        {
            foreach (TaskStatsPanelUI.SortMode mode in Enum.GetValues(typeof(TaskStatsPanelUI.SortMode)))
                CreateButton(mode, () =>
                {
                    taskMode = mode;
                    if (taskPanel != null) taskPanel.SetSortMode(mode);
                    UpdateButtonStates();
                });
            UpdateButtonStates();
        }

        private void BuildItemButtons()
        {
            foreach (ItemStatsPanelUI.SortMode mode in Enum.GetValues(typeof(ItemStatsPanelUI.SortMode)))
                CreateButton(mode, () =>
                {
                    itemMode = mode;
                    if (itemPanel != null) itemPanel.SetSortMode(mode);
                    UpdateButtonStates();
                });
            UpdateButtonStates();
        }

        private void BuildGraphButtons()
        {
            CreateButton(RunStatsPanelUI.GraphMode.Distance, () =>
            {
                runMode = RunStatsPanelUI.GraphMode.Distance;
                if (runStatsPanel != null) runStatsPanel.SetGraphMode(runMode);
                UpdateButtonStates();
            });
            CreateButton(RunStatsPanelUI.GraphMode.Duration, () =>
            {
                runMode = RunStatsPanelUI.GraphMode.Duration;
                if (runStatsPanel != null) runStatsPanel.SetGraphMode(runMode);
                UpdateButtonStates();
            });
            CreateButton(RunStatsPanelUI.GraphMode.Resources, () =>
            {
                runMode = RunStatsPanelUI.GraphMode.Resources;
                if (runStatsPanel != null) runStatsPanel.SetGraphMode(runMode);
                UpdateButtonStates();
            });
            CreateButton(RunStatsPanelUI.GraphMode.Kills, () =>
            {
                runMode = RunStatsPanelUI.GraphMode.Kills;
                if (runStatsPanel != null) runStatsPanel.SetGraphMode(runMode);
                UpdateButtonStates();
            });
            UpdateButtonStates();
        }

        private void CreateButton(Enum mode, UnityAction action)
        {
            if (buttonParent == null || buttonPrefab == null) return;

            var btn = Instantiate(buttonPrefab, buttonParent);
            btn.SetLabel(PrettyLabel(mode));
            btn.Button.onClick.AddListener(action);
            buttons.Add(btn);
            buttonModes[btn] = mode;
        }

        private static string PrettyLabel(Enum mode)
        {
            // Insert spaces between PascalCase words, e.g., "AttackRate" -> "Attack Rate"
            var name = mode.ToString();
            if (string.IsNullOrEmpty(name)) return string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length + 4);
            sb.Append(name[0]);
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                char p = name[i - 1];
                if (char.IsUpper(c) && !char.IsWhiteSpace(p))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        private void UpdateButtonStates()
        {
            if (buttons.Count == 0) return;

            Enum selected = null;
            switch (currentTab)
            {
                case 1:
                    selected = runMode;
                    break;
                case 2:
                    selected = enemyMode;
                    break;
                case 3:
                    selected = taskMode;
                    break;
                case 4:
                    selected = itemMode;
                    break;
            }

            if (selected == null) return;

            foreach (var pair in buttonModes)
                pair.Key.SetInteractable(!Equals(pair.Value, selected));
        }
    }
}
