using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Instantiates and manages sorting buttons based on the active stats tab.
    /// </summary>
    public class StatSortingManager : MonoBehaviour
    {
        [SerializeField] private Transform buttonParent;
        [SerializeField] private StatSortButton buttonPrefab;

        [SerializeField] private EnemyStatsPanelUI enemyPanel;
        [SerializeField] private TaskStatsPanelUI taskPanel;
        [SerializeField] private ItemStatsPanelUI itemPanel;

        private readonly List<StatSortButton> buttons = new();
        private readonly Dictionary<StatSortButton, Enum> buttonModes = new();

        private int currentTab = -1;
        private EnemyStatsPanelUI.SortMode enemyMode = EnemyStatsPanelUI.SortMode.Default;
        private TaskStatsPanelUI.SortMode taskMode = TaskStatsPanelUI.SortMode.Default;
        private ItemStatsPanelUI.SortMode itemMode = ItemStatsPanelUI.SortMode.Default;

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
            if (enemyPanel != null && enemyPanel.gameObject.activeInHierarchy) return 1;
            if (taskPanel != null && taskPanel.gameObject.activeInHierarchy) return 2;
            if (itemPanel != null && itemPanel.gameObject.activeInHierarchy) return 3;
            return 0;
        }

        private void BuildButtons()
        {
            ClearButtons();
            switch (currentTab)
            {
                case 1:
                    BuildEnemyButtons();
                    break;
                case 2:
                    BuildTaskButtons();
                    break;
                case 3:
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
            foreach (EnemyStatsPanelUI.SortMode mode in Enum.GetValues(typeof(EnemyStatsPanelUI.SortMode)))
                CreateButton(mode, () =>
                {
                    enemyMode = mode;
                    if (enemyPanel != null) enemyPanel.SetSortMode(mode);
                    UpdateButtonStates();
                });
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

        private void CreateButton(Enum mode, UnityAction action)
        {
            if (buttonParent == null || buttonPrefab == null) return;

            var btn = Instantiate(buttonPrefab, buttonParent);
            btn.SetLabel(mode.ToString());
            btn.Button.onClick.AddListener(action);
            buttons.Add(btn);
            buttonModes[btn] = mode;
        }

        private void UpdateButtonStates()
        {
            if (buttons.Count == 0) return;

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
                pair.Key.SetInteractable(!Equals(pair.Value, selected));
        }
    }
}
