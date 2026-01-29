using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Enemies;
using TimelessEchoes.Stats;
using Blindsided.Utilities;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;
using TimelessEchoes.UI.Core;
using UnityEngine.UI;
using TMPro;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Event-driven enemy stats panel. Subscribes to EnemyKillTracker and GameplayStatTracker
    /// events instead of polling via UITicker.
    /// </summary>
    public class EnemyStatsPanelUI : EventDrivenStatsPanelUI
    {
        [SerializeField] private StatPanelReferences references;
        [Header("Distance Preview")]
        [SerializeField] private Slider distanceSlider;
        [SerializeField] private TMP_Text distanceText;
        private EnemyKillTracker killTracker;
        private float lastKnownMaxDistance;

        private readonly Dictionary<EnemyData, EnemyStatEntryUIReferences> entries = new();
        private readonly Dictionary<EnemyData, (double kills, int reveal, float bonus, int level, bool spawnable)> lastDisplayed = new();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(128);
        private List<EnemyData> defaultOrder = new();

        // Scratch lists for allocation-free sorting
        private readonly List<EnemyData> _scratchKnown = new();
        private readonly List<EnemyData> _scratchUnknown = new();
        private readonly List<EnemyData> _scratchFinal = new();
        private bool _sortDirty = true;
        private SortMode _lastAppliedSortMode;

        // Static comparison delegates to avoid closure allocations
        private static readonly System.Comparison<EnemyData> CompareByDisplayOrderThenName =
            (a, b) =>
            {
                int cmp = a.displayOrder.CompareTo(b.displayOrder);
                return cmp != 0 ? cmp : string.Compare(a.enemyName, b.enemyName, System.StringComparison.Ordinal);
            };

        public enum SortMode
        {
            Default,
            Damage,
            Health,
            Defense,
            AttackRate,
            MoveSpeed,
            Vision
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

        protected override bool IsPanelVisible()
        {
            if (references != null && references.enemyEntryParent != null)
                return references.enemyEntryParent.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        protected override void SubscribeToEvents()
        {
            // Re-acquire trackers in case they weren't ready at Awake
            if (killTracker == null)
                killTracker = EnemyKillTracker.Instance;
            
            if (killTracker != null)
            {
                killTracker.OnKillRegistered += HandleKillRegistered;
            }
            
            var statTracker = GameplayStatTracker.Instance;
            if (statTracker != null)
            {
                statTracker.OnMaxRunDistanceChanged += OnMaxRunDistanceChanged;
            }
            
            SetupDistanceSlider();
        }

        protected override void UnsubscribeFromEvents()
        {
            if (killTracker != null)
            {
                killTracker.OnKillRegistered -= HandleKillRegistered;
            }
            
            var statTracker = GameplayStatTracker.Instance;
            if (statTracker != null)
            {
                statTracker.OnMaxRunDistanceChanged -= OnMaxRunDistanceChanged;
            }
            
            if (distanceSlider != null)
                distanceSlider.onValueChanged.RemoveListener(OnDistanceSliderChanged);
        }

        // Event handlers
        private void HandleKillRegistered(EnemyData enemy)
        {
            _sortDirty = true; // Kill may change known/unknown status
            OnDataChanged();
        }

        private void OnMaxRunDistanceChanged(float newMax)
        {
            // Update slider bounds and value without jumping unless the user was at max
            if (distanceSlider != null)
            {
                bool wasAtMax = Mathf.Approximately(distanceSlider.value, lastKnownMaxDistance);
                distanceSlider.maxValue = newMax;
                if (wasAtMax || distanceSlider.value > newMax)
                    distanceSlider.SetValueWithoutNotify(newMax);
            }
            lastKnownMaxDistance = newMax;
            UpdateDistanceLabel();
            OnDataChanged();
        }

        private void OnDistanceSliderChanged(float _)
        {
            UpdateDistanceLabel();
            OnDataChanged();
        }

        private void SetupDistanceSlider()
        {
            var tracker = GameplayStatTracker.Instance;
            float max = tracker != null ? tracker.MaxRunDistance : 0f;
            lastKnownMaxDistance = max;
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
        }

        private float GetPreviewDistance()
        {
            var tracker = GameplayStatTracker.Instance;
            if (distanceSlider != null)
                return Mathf.Clamp(distanceSlider.value, 0f, tracker != null ? tracker.MaxRunDistance : float.MaxValue);
            return tracker != null ? tracker.MaxRunDistance : 0f;
        }

        public Slider DistanceSlider => distanceSlider;
        public TMP_Text DistanceText => distanceText;

        private void UpdateDistanceLabel()
        {
            if (distanceText != null)
            {
                float dist = GetPreviewDistance();
                distanceText.text = $"Distance | {dist:N0}";
            }
        }

        public void SetSortMode(SortMode mode)
        {
            if (sortMode == mode) return;
            sortMode = mode;
            _sortDirty = true;
            if (IsPanelVisible())
                RefreshUI();
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
            _sortDirty = true; // Ensure initial sort happens

            foreach (var stats in sorted)
            {
                var obj = Instantiate(references.enemyEntryPrefab.gameObject, references.enemyEntryParent);
                var ui = obj.GetComponent<EnemyStatEntryUIReferences>();
                if (ui == null) continue;
                entries[stats] = ui;
            }
        }

        protected override void RefreshUI()
        {
            UpdateEntries();
            SortEntries();
            isDirty = false;
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

            if (lastDisplayed.TryGetValue(stats, out var last))
            {
                // Detect transition from unknown to known - requires re-sort
                if (last.kills == 0 && kills > 0)
                    _sortDirty = true;

                if (last.kills == kills && last.reveal == reveal && Mathf.Approximately(last.bonus, bonus)
                    && last.level == level && last.spawnable == spawnable)
                    return;
            }
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

            // Early exit if no re-sort needed
            if (!_sortDirty && sortMode == _lastAppliedSortMode)
                return;

            _sortDirty = false;
            _lastAppliedSortMode = sortMode;

            // Clear scratch lists for reuse
            _scratchKnown.Clear();
            _scratchUnknown.Clear();
            _scratchFinal.Clear();

            if (sortMode == SortMode.Default)
            {
                // Partition into known/unknown
                for (int i = 0; i < defaultOrder.Count; i++)
                {
                    var enemy = defaultOrder[i];
                    if (killTracker != null && killTracker.GetKills(enemy) > 0)
                        _scratchKnown.Add(enemy);
                    else
                        _scratchUnknown.Add(enemy);
                }

                // Sort each partition
                _scratchKnown.Sort(CompareByDisplayOrderThenName);
                _scratchUnknown.Sort(CompareByDisplayOrderThenName);

                // Combine: known first, then unknown
                _scratchFinal.AddRange(_scratchKnown);
                _scratchFinal.AddRange(_scratchUnknown);
                ApplyOrderScratch();
                return;
            }

            // Stat-based sorting modes
            int threshold = sortMode switch
            {
                SortMode.Damage => 1,
                SortMode.Health => 2,
                SortMode.Defense => 3,
                SortMode.MoveSpeed => 5,
                SortMode.AttackRate => 4,
                SortMode.Vision => 6,
                _ => 0
            };

            // Partition into revealed/unrevealed based on threshold
            for (int i = 0; i < defaultOrder.Count; i++)
            {
                var enemy = defaultOrder[i];
                if (killTracker != null && killTracker.GetRevealLevel(enemy) >= threshold)
                    _scratchKnown.Add(enemy);
                else
                    _scratchUnknown.Add(enemy);
            }

            // Sort revealed by stat value (descending), then by display order
            var currentMode = sortMode;
            _scratchKnown.Sort((a, b) =>
            {
                float valA = GetStatValue(a, currentMode);
                float valB = GetStatValue(b, currentMode);
                int cmp = valB.CompareTo(valA); // Descending
                return cmp != 0 ? cmp : a.displayOrder.CompareTo(b.displayOrder);
            });

            // Combine: revealed first, then unrevealed (unrevealed keeps default order)
            _scratchFinal.AddRange(_scratchKnown);
            _scratchFinal.AddRange(_scratchUnknown);
            ApplyOrderScratch();
        }

        private static float GetStatValue(EnemyData s, SortMode mode) => mode switch
        {
            SortMode.Damage => s.damage,
            SortMode.Health => s.maxHealth,
            SortMode.Defense => s.defense,
            SortMode.MoveSpeed => s.moveSpeed,
            SortMode.AttackRate => s.attackSpeed,
            SortMode.Vision => s.visionRange,
            _ => 0
        };

        private void ApplyOrderScratch()
        {
            for (int i = 0; i < _scratchFinal.Count; i++)
            {
                if (entries.TryGetValue(_scratchFinal[i], out var ui))
                    ui.transform.SetSiblingIndex(i);
            }
        }
    }
}
