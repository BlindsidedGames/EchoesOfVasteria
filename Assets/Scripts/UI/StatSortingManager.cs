using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Manages sorting buttons based on the active stats tab.
    /// </summary>
    public class StatSortingManager : MonoBehaviour
    {
        [SerializeField] private GeneralStatsPanelUI generalPanel;
        [SerializeField] private EnemyStatsPanelUI enemyPanel;
        [SerializeField] private TaskStatsPanelUI taskPanel;
        [SerializeField] private ItemStatsPanelUI itemPanel;

        [Serializable]
        private struct EnemyButton
        {
            public EnemyStatsPanelUI.SortMode mode;
            public StatSortButton button;
        }

        [Serializable]
        private struct TaskButton
        {
            public TaskStatsPanelUI.SortMode mode;
            public StatSortButton button;
        }

        [Serializable]
        private struct ItemButton
        {
            public ItemStatsPanelUI.SortMode mode;
            public StatSortButton button;
        }

        [SerializeField] private List<EnemyButton> enemyButtons = new();
        [SerializeField] private List<TaskButton> taskButtons = new();
        [SerializeField] private List<ItemButton> itemButtons = new();

        private readonly Dictionary<StatSortButton, Enum> buttonModes = new();
        private readonly Dictionary<StatSortButton, UnityAction> buttonActions = new();

        private int currentTab = -1;
        private EnemyStatsPanelUI.SortMode enemyMode = EnemyStatsPanelUI.SortMode.Default;
        private TaskStatsPanelUI.SortMode taskMode = TaskStatsPanelUI.SortMode.Default;
        private ItemStatsPanelUI.SortMode itemMode = ItemStatsPanelUI.SortMode.Default;

        private void Awake()
        {
            SetupEnemyButtons();
            SetupTaskButtons();
            SetupItemButtons();
            HideAllButtons();
        }

        private void OnDestroy()
        {
            foreach (var pair in buttonActions)
                if (pair.Key != null)
                    pair.Key.Button.onClick.RemoveListener(pair.Value);
        }

        private void Update()
        {
            int tab = GetActiveTab();
            if (tab != currentTab)
            {
                currentTab = tab;
                BuildButtons();
            }
        }

        private int GetActiveTab()
        {
            if (generalPanel != null && generalPanel.gameObject.activeInHierarchy) return 0;
            if (enemyPanel != null && enemyPanel.gameObject.activeInHierarchy) return 1;
            if (taskPanel != null && taskPanel.gameObject.activeInHierarchy) return 2;
            if (itemPanel != null && itemPanel.gameObject.activeInHierarchy) return 3;
            return -1;
        }

        private void BuildButtons()
        {
            HideAllButtons();
            switch (currentTab)
            {
                case 1:
                    ShowButtons(enemyButtons);
                    break;
                case 2:
                    ShowButtons(taskButtons);
                    break;
                case 3:
                    ShowButtons(itemButtons);
                    break;
            }
            UpdateButtonStates();
        }

        private void HideAllButtons()
        {
            foreach (var pair in buttonModes)
                if (pair.Key != null)
                    pair.Key.gameObject.SetActive(false);
        }

        private void SetupEnemyButtons()
        {
            foreach (var entry in enemyButtons)
            {
                if (entry.button == null) continue;
                entry.button.SetLabel(entry.mode.ToString());
                UnityAction action = () =>
                {
                    enemyMode = entry.mode;
                    if (enemyPanel != null) enemyPanel.SetSortMode(entry.mode);
                    UpdateButtonStates();
                };
                entry.button.Button.onClick.AddListener(action);
                buttonModes[entry.button] = entry.mode;
                buttonActions[entry.button] = action;
            }
        }

        private void SetupTaskButtons()
        {
            foreach (var entry in taskButtons)
            {
                if (entry.button == null) continue;
                entry.button.SetLabel(entry.mode.ToString());
                UnityAction action = () =>
                {
                    taskMode = entry.mode;
                    if (taskPanel != null) taskPanel.SetSortMode(entry.mode);
                    UpdateButtonStates();
                };
                entry.button.Button.onClick.AddListener(action);
                buttonModes[entry.button] = entry.mode;
                buttonActions[entry.button] = action;
            }
        }

        private void SetupItemButtons()
        {
            foreach (var entry in itemButtons)
            {
                if (entry.button == null) continue;
                entry.button.SetLabel(entry.mode.ToString());
                UnityAction action = () =>
                {
                    itemMode = entry.mode;
                    if (itemPanel != null) itemPanel.SetSortMode(entry.mode);
                    UpdateButtonStates();
                };
                entry.button.Button.onClick.AddListener(action);
                buttonModes[entry.button] = entry.mode;
                buttonActions[entry.button] = action;
            }
        }

        private void ShowButtons<T>(IEnumerable<T> list) where T : struct
        {
            foreach (var entry in list)
            {
                StatSortButton btn = default;
                if (entry is EnemyButton e) btn = e.button;
                else if (entry is TaskButton t) btn = t.button;
                else if (entry is ItemButton i) btn = i.button;
                if (btn != null) btn.gameObject.SetActive(true);
            }
        }

        private void UpdateButtonStates()
        {
            if (buttonModes.Count == 0) return;

            Enum selected = null;
            switch (currentTab)
            {
                case 1:
                    selected = enemyMode;
                    break;
                case 2:
                    selected = taskMode;
                    break;
                case 3:
                    selected = itemMode;
                    break;
            }
            if (selected == null) return;

            foreach (var pair in buttonModes)
                if (pair.Key != null && pair.Key.gameObject.activeSelf)
                    pair.Key.SetInteractable(!Equals(pair.Value, selected));
        }
    }
}
