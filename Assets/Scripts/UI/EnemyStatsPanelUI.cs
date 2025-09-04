using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using Blindsided.Utilities;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;
using UnityEngine.UI;
using TMPro;

namespace TimelessEchoes.UI
{
    public class EnemyStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        [Header("Distance Preview")]
        [SerializeField] private Slider distanceSlider;
        [SerializeField] private TMP_Text distanceText;
        private EnemyKillTracker killTracker;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

        private readonly Dictionary<EnemyData, EnemyStatEntryUIReferences> entries = new();
        private readonly Dictionary<EnemyData, (double kills, int reveal, float bonus, int level, bool spawnable)> lastDisplayed = new();
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
            SetupDistanceSlider();
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(RefreshTick);
            if (distanceSlider != null)
                distanceSlider.onValueChanged.RemoveListener(OnDistanceSliderChanged);
        }

        private void RefreshTick()
        {
            if (!IsPanelVisible()) return;
            UpdateEntries();
            SortEntries();
        }

        private void SetupDistanceSlider()
        {
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            float max = tracker != null ? tracker.MaxRunDistance : 0f;
            if (distanceSlider != null)
            {
                distanceSlider.minValue = 0f;
                distanceSlider.maxValue = max;
                // Default to max run distance per requirements
                distanceSlider.SetValueWithoutNotify(max);
                distanceSlider.onValueChanged.RemoveListener(OnDistanceSliderChanged);
                distanceSlider.onValueChanged.AddListener(OnDistanceSliderChanged);
            }
            UpdateDistanceLabel();
            UpdateEntries();
        }

        private void OnDistanceSliderChanged(float _)
        {
            UpdateDistanceLabel();
            UpdateEntries();
        }

        private float GetPreviewDistance()
        {
            var tracker = TimelessEchoes.Stats.GameplayStatTracker.Instance;
            if (distanceSlider != null)
                return Mathf.Clamp(distanceSlider.value, 0f, tracker != null ? tracker.MaxRunDistance : float.MaxValue);
            return tracker != null ? tracker.MaxRunDistance : 0f;
        }

        private void UpdateDistanceLabel()
        {
            if (distanceText != null)
            {
                float dist = GetPreviewDistance();
                distanceText.text = $"Distance | {dist:N0}";
            }
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
            float previewDist = GetPreviewDistance();
            // Compute level based on distance relative to minX
            float relative = Mathf.Max(0f, previewDist - stats.minX);
            int level = stats.GetLevel(relative);
            bool withinMax = float.IsInfinity(stats.maxX) || previewDist <= stats.maxX;
            bool spawnable = previewDist >= stats.minX && withinMax;

            if (lastDisplayed.TryGetValue(stats, out var last)
                && last.kills == kills && last.reveal == reveal && Mathf.Approximately(last.bonus, bonus)
                && last.level == level && last.spawnable == spawnable)
                return;
            lastDisplayed[stats] = (kills, reveal, bonus, level, spawnable);

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
                {
                    string lvlText = spawnable ? level.ToString() : "-";
                    ui.enemyNameText.text = $"{stats.enemyName} | {lvlText}";
                }
                else
                    ui.enemyNameText.text = "???";
            }

            if (ui.enemyIDText != null)
                ui.enemyIDText.text = $"#{stats.displayOrder}";

            // Health/Damage (scaled by level). If revealed but not spawnable at distance, show '-'
            string hp = "???";
            if (reveal >= 2)
            {
                hp = spawnable ? CalcUtils.FormatNumber(stats.GetMaxHealthForLevel(level), true, 400f, false) : "-";
            }
            string dmg = "???";
            if (reveal >= 1)
            {
                dmg = spawnable ? CalcUtils.FormatNumber(stats.GetDamageForLevel(level), true, 400f, false) : "-";
            }
            _sb.Clear();
            _sb.Append("Health: "); _sb.Append(hp); _sb.Append('\n');
            _sb.Append("Damage: "); _sb.Append(dmg);
            ui.hitpointsAndDamageText.SetText(_sb);

            // Defense/Attack Rate
            string def = "???";
            if (reveal >= 3)
            {
                if (!spawnable) def = "-";
                else
                {
                    float defVal = stats.GetDefenseForLevel(level);
                    float frac = TimelessEchoes.Combat.ApplyDefense(1f, defVal);
                    float reduction = (1f - frac) * 100f;
                    def = reduction.ToString("0") + "%";
                }
            }
            string atk = reveal >= 4 ? (spawnable ? CalcUtils.FormatNumber(stats.attackSpeed, true, 400f, false) : "-") : "???";
            _sb.Clear();
            _sb.Append("Defense: "); _sb.Append(def); _sb.Append('\n');
            _sb.Append("Attack Rate: "); _sb.Append(atk);
            ui.movementAndAttackRateText.SetText(_sb);

            // Movement/Vision in a dedicated (optional) field
            if (ui.movementAndVisionText != null)
            {
                string move = reveal >= 5 ? (spawnable ? CalcUtils.FormatNumber(stats.moveSpeed, true, 400f, false) : "-") : "???";
                string vis = reveal >= 6 ? (spawnable ? CalcUtils.FormatNumber(stats.visionRange, true, 400f, false) : "-") : "???";
                _sb.Clear();
                _sb.Append("Movement: "); _sb.Append(move); _sb.Append('\n');
                _sb.Append("Vision: "); _sb.Append(vis);
                ui.movementAndVisionText.SetText(_sb);
            }

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
                // Movement reveal now at threshold 5
                SortMode.MoveSpeed => 5,
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
