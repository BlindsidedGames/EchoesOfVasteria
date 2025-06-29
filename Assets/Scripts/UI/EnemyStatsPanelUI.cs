using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using Blindsided.Utilities;

namespace TimelessEchoes.UI
{
    public class EnemyStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        [SerializeField] private EnemyKillTracker killTracker;

        private readonly Dictionary<EnemyStats, EnemyStatEntryUIReferences> entries = new();
        private List<EnemyStats> defaultOrder = new();

        public enum SortMode
        {
            Default,
            Damage,
            Health,
            AttackSpeed,
            MoveSpeed
        }

        [SerializeField] private SortMode sortMode = SortMode.Default;

        private void Awake()
        {
            if (references == null)
                references = GetComponent<StatPanelReferences>();
            if (killTracker == null)
                killTracker = FindFirstObjectByType<EnemyKillTracker>();
            BuildEntries();
        }

        private void OnEnable()
        {
            UpdateEntries();
        }

        private void Update()
        {
            UpdateEntries();
            SortEntries();
        }

        public void SetSortMode(SortMode mode)
        {
            sortMode = mode;
            SortEntries();
        }

        private void BuildEntries()
        {
            if (references == null || references.enemyEntryParent == null || references.enemyEntryPrefab == null)
                return;

            foreach (Transform child in references.enemyEntryParent)
                Destroy(child.gameObject);

            var allStats = Resources.LoadAll<EnemyStats>("");
            var sorted = allStats
                .OrderBy(s => s.displayOrder)
                .ThenBy(s => s.enemyName)
                .ToList();
            defaultOrder = sorted;
            entries.Clear();

            foreach (var stats in sorted)
            {
                var obj = Instantiate(references.enemyEntryPrefab.gameObject, references.enemyEntryParent);
                var ui = obj.GetComponent<EnemyStatEntryUIReferences>();
                if (ui == null) continue;
                entries[stats] = ui;
            }

            SortEntries();
        }

        private void UpdateEntries()
        {
            foreach (var pair in entries)
                UpdateEntry(pair.Key, pair.Value);
        }

        private void UpdateEntry(EnemyStats stats, EnemyStatEntryUIReferences ui)
        {
            if (stats == null || ui == null) return;
            double kills = killTracker ? killTracker.GetKills(stats) : 0;
            int reveal = killTracker ? killTracker.GetRevealLevel(stats) : 0;
            float bonus = (killTracker ? killTracker.GetDamageMultiplier(stats) : 1f) - 1f;

            if (ui.enemyIconImage != null)
            {
                ui.enemyIconImage.sprite = stats.icon;
                ui.enemyIconImage.enabled = stats.icon != null;
            }

            if (ui.enemyNameText != null)
                ui.enemyNameText.text = stats.enemyName;

            string hp = reveal >= 2 ? CalcUtils.FormatNumber(stats.maxHealth, true, 400f, false) : "???";
            string dmg = reveal >= 1 ? CalcUtils.FormatNumber(stats.damage, true, 400f, false) : "???";
            ui.hitpointsAndDamageText.text = $"Hitpoints: {hp}\nDamage: {dmg}";

            string move = reveal >= 3 ? CalcUtils.FormatNumber(stats.moveSpeed, true, 400f, false) : "???";
            string atk = reveal >= 4 ? CalcUtils.FormatNumber(stats.attackSpeed, true, 400f, false) : "???";
            ui.movementAndAttackRateText.text = $"Move Speed: {move}\nAttack Rate: {atk}";

            string killsText = CalcUtils.FormatNumber(kills, true, 400f, false);
            if (reveal < EnemyKillTracker.Thresholds.Length)
            {
                int next = EnemyKillTracker.Thresholds[reveal];
                string nextStr = CalcUtils.FormatNumber(next, true, 400f, false);
                killsText += $" / {nextStr}";
                if (ui.progressBar != null)
                {
                    ui.progressBar.SetActive(true);
                    ui.nextRevealProgressBar.fillAmount = Mathf.Clamp01((float)(kills / next));
                }
            }
            else
            {
                if (ui.progressBar != null)
                    ui.progressBar.SetActive(false);
            }

            if (ui.killsAndNextAndBonusText != null)
                ui.killsAndNextAndBonusText.text = $"Kills: {killsText}\nBonus Damage: {(bonus * 100f):0}%";
        }

        private void SortEntries()
        {
            if (entries.Count == 0 || defaultOrder.Count == 0)
                return;

            if (sortMode == SortMode.Default)
            {
                ApplyOrder(defaultOrder);
                return;
            }

            int threshold = sortMode switch
            {
                SortMode.Damage => 1,
                SortMode.Health => 2,
                SortMode.MoveSpeed => 3,
                SortMode.AttackSpeed => 4,
                _ => 0
            };

            float GetValue(EnemyStats s) => sortMode switch
            {
                SortMode.Damage => s.damage,
                SortMode.Health => s.maxHealth,
                SortMode.MoveSpeed => s.moveSpeed,
                SortMode.AttackSpeed => s.attackSpeed,
                _ => 0
            };

            IEnumerable<EnemyStats> known = defaultOrder;
            IEnumerable<EnemyStats> unknown = Enumerable.Empty<EnemyStats>();
            if (killTracker != null)
            {
                known = defaultOrder.Where(s => killTracker.GetRevealLevel(s) >= threshold);
                unknown = defaultOrder.Where(s => killTracker.GetRevealLevel(s) < threshold);
            }

            var sortedKnown = known
                .OrderByDescending(GetValue)
                .ThenBy(s => s.displayOrder)
                .ToList();

            var finalOrder = sortedKnown.Concat(unknown).ToList();
            ApplyOrder(finalOrder);
        }

        private void ApplyOrder(IList<EnemyStats> order)
        {
            int index = 0;
            foreach (var stats in order)
            {
                if (entries.TryGetValue(stats, out var ui))
                    ui.transform.SetSiblingIndex(index++);
            }
        }
    }
}
