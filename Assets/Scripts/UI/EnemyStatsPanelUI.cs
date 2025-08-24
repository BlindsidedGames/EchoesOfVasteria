using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using Blindsided.Utilities;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.UI
{
    public class EnemyStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        private EnemyKillTracker killTracker;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

        private readonly Dictionary<EnemyData, EnemyStatEntryUIReferences> entries = new();
        private readonly Dictionary<EnemyData, (double kills, int reveal, float bonus)> lastDisplayed = new();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(128);
        private List<EnemyData> defaultOrder = new();

        public enum SortMode
        {
            Default,
            Damage,
            Health,
            AttackRate,
            MoveSpeed
        }

        [SerializeField] private SortMode sortMode = SortMode.Default;

        private void Awake()
        {
            if (references == null)
                references = GetComponent<StatPanelReferences>();
            killTracker = EnemyKillTracker.Instance;
            if (killTracker == null)
                TELogger.Log("EnemyKillTracker missing", TELogCategory.Combat, this);
            BuildEntries();
        }

        private void OnEnable()
        {
            UpdateEntries();
            SortEntries();
            UITicker.Instance?.Subscribe(RefreshTick, updateInterval);
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(RefreshTick);
        }

        private void RefreshTick()
        {
            if (!IsPanelVisible()) return;
            UpdateEntries();
            SortEntries();
        }

        private bool IsPanelVisible()
        {
            if (references != null && references.enemyEntryParent != null)
                return references.enemyEntryParent.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
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

            UIUtils.ClearChildren(references.enemyEntryParent);

            var allStats = Blindsided.Utilities.AssetCache.GetAll<EnemyData>("");
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

        private void UpdateEntry(EnemyData stats, EnemyStatEntryUIReferences ui)
        {
            if (stats == null || ui == null) return;
            double kills = killTracker ? killTracker.GetKills(stats) : 0;
            int reveal = killTracker ? killTracker.GetRevealLevel(stats) : 0;
            float bonus = (killTracker ? killTracker.GetDamageMultiplier(stats) : 1f) - 1f;

            if (lastDisplayed.TryGetValue(stats, out var last)
                && last.kills == kills && last.reveal == reveal && Mathf.Approximately(last.bonus, bonus))
                return;
            lastDisplayed[stats] = (kills, reveal, bonus);

            if (ui.enemyIconImage != null)
            {
                bool encountered = kills > 0;
                Sprite sprite = encountered ? stats.icon : null;
                ui.enemyIconImage.sprite = sprite;
                if (sprite != null)
                    ui.enemyIconImage.SetNativeSize();
                ui.enemyIconImage.enabled = encountered && sprite != null;
            }

            if (ui.enemyNameText != null)
            {
                if (kills > 0)
                    ui.enemyNameText.text = stats.enemyName;
                else
                    ui.enemyNameText.text = "???";
            }

            if (ui.enemyIDText != null)
                ui.enemyIDText.text = $"#{stats.displayOrder}";

            string hp = reveal >= 2 ? CalcUtils.FormatNumber(stats.maxHealth, true, 400f, false) : "???";
            string dmg = reveal >= 1 ? CalcUtils.FormatNumber(stats.damage, true, 400f, false) : "???";
            _sb.Clear();
            _sb.Append("Health: "); _sb.Append(hp); _sb.Append('\n');
            _sb.Append("Damage: "); _sb.Append(dmg);
            ui.hitpointsAndDamageText.SetText(_sb);

            string move = reveal >= 3 ? CalcUtils.FormatNumber(stats.moveSpeed, true, 400f, false) : "???";
            string atk = reveal >= 4 ? CalcUtils.FormatNumber(stats.attackSpeed, true, 400f, false) : "???";
            _sb.Clear();
            _sb.Append("Move Speed: "); _sb.Append(move); _sb.Append('\n');
            _sb.Append("Attack Rate: "); _sb.Append(atk);
            ui.movementAndAttackRateText.SetText(_sb);

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
            {
                _sb.Clear();
                _sb.Append("Kills: "); _sb.Append(killsText); _sb.Append('\n');
                _sb.Append("Bonus Damage: "); _sb.Append((bonus * 100f).ToString("0")); _sb.Append('%');
                ui.killsAndNextAndBonusText.SetText(_sb);
            }
        }

        private void SortEntries()
        {
            if (entries.Count == 0 || defaultOrder.Count == 0)
                return;

            if (sortMode == SortMode.Default)
            {
                IEnumerable<EnemyData> known = defaultOrder;
                IEnumerable<EnemyData> unknown = Enumerable.Empty<EnemyData>();
                if (killTracker != null)
                {
                    known = defaultOrder.Where(s => killTracker.GetKills(s) > 0);
                    unknown = defaultOrder.Where(s => killTracker.GetKills(s) <= 0);
                }

                var sortedKnown = known
                    .OrderBy(s => s.displayOrder)
                    .ThenBy(s => s.enemyName)
                    .ToList();
                var sortedUnknown = unknown
                    .OrderBy(s => s.displayOrder)
                    .ThenBy(s => s.enemyName)
                    .ToList();

                var finalDefault = sortedKnown.Concat(sortedUnknown).ToList();
                ApplyOrder(finalDefault);
                return;
            }

            int threshold = sortMode switch
            {
                SortMode.Damage => 1,
                SortMode.Health => 2,
                SortMode.MoveSpeed => 3,
                SortMode.AttackRate => 4,
                _ => 0
            };

            float GetValue(EnemyData s) => sortMode switch
            {
                SortMode.Damage => s.damage,
                SortMode.Health => s.maxHealth,
                SortMode.MoveSpeed => s.moveSpeed,
                SortMode.AttackRate => s.attackSpeed,
                _ => 0
            };

            IEnumerable<EnemyData> revealed = defaultOrder;
            IEnumerable<EnemyData> unrevealed = Enumerable.Empty<EnemyData>();
            if (killTracker != null)
            {
                revealed = defaultOrder.Where(s => killTracker.GetRevealLevel(s) >= threshold);
                unrevealed = defaultOrder.Where(s => killTracker.GetRevealLevel(s) < threshold);
            }

            var sortedRevealed = revealed
                .OrderByDescending(GetValue)
                .ThenBy(s => s.displayOrder)
                .ToList();

            var finalOrder = sortedRevealed.Concat(unrevealed).ToList();
            ApplyOrder(finalOrder);
        }

        private void ApplyOrder(IList<EnemyData> order)
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
