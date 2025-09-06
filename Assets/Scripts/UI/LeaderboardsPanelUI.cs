using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blindsided.UGS;
using TMPro;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Controls the Leaderboards UI: shows 17 entries and a separate colored player row.
    /// Supports four leaderboards and a toggle for Top vs Around Player.
    /// </summary>
    public class LeaderboardsPanelUI : MonoBehaviour
    {
        public enum Board
        {
            DistanceReached,
            DistanceTravelled,
            Kills,
            Tasks
        }

        [Header("Entry Rows")] 
        [SerializeField] private Transform entriesContainer; // Parent to hold pooled rows
        [SerializeField] private LeaderboardEntryUI entryPrefab; // Prefab for a single entry
        [SerializeField] private int poolSize = 50; // excludes the separate player row
        [SerializeField] private LeaderboardEntryUI playerEntry; // separate player entry row
        private List<LeaderboardEntryUI> entrySlots = new List<LeaderboardEntryUI>(50); // runtime pool (not serialized)

        [Header("Board Buttons")] [SerializeField] private Button distanceReachedButton;
        [SerializeField] private Button distanceTravelledButton;
        [SerializeField] private Button killsButton;
        [SerializeField] private Button tasksButton;

        [Header("Toggle (Top vs Around)")] [SerializeField] private Button toggleButton;
        [SerializeField] private Image toggleImage;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        [Header("Scrolling")] [SerializeField] private ScrollRect scrollRect; // optional; if null, try to find in parent

        [Header("Optional Header Text")] [SerializeField] private TMP_Text boardLabel;

        [SerializeField] private Board currentBoard = Board.DistanceReached;
        [SerializeField] private bool showTop = false; // default OFF => around player

        private bool refreshing;
        private bool forceScrollTopNext;

        private void Awake()
        {
            if (distanceReachedButton != null) distanceReachedButton.onClick.AddListener(() => OnBoardClicked(Board.DistanceReached));
            if (distanceTravelledButton != null) distanceTravelledButton.onClick.AddListener(() => OnBoardClicked(Board.DistanceTravelled));
            if (killsButton != null) killsButton.onClick.AddListener(() => OnBoardClicked(Board.Kills));
            if (tasksButton != null) tasksButton.onClick.AddListener(() => OnBoardClicked(Board.Tasks));
            if (toggleButton != null) toggleButton.onClick.AddListener(OnToggleClicked);
            BuildPoolIfNeeded();

            // Optional auto-wire for scroll rect
            if (scrollRect == null)
            {
                scrollRect = GetComponentInParent<ScrollRect>();
            }
        }

        private void OnEnable()
        {
            UpdateBoardLabel();
            UpdateToggleVisual();
            UpdateButtonStates();
            _ = RefreshAsync();
        }

        private void OnDestroy()
        {
            if (distanceReachedButton != null) distanceReachedButton.onClick.RemoveAllListeners();
            if (distanceTravelledButton != null) distanceTravelledButton.onClick.RemoveAllListeners();
            if (killsButton != null) killsButton.onClick.RemoveAllListeners();
            if (tasksButton != null) tasksButton.onClick.RemoveAllListeners();
            if (toggleButton != null) toggleButton.onClick.RemoveAllListeners();
        }

        private void OnBoardClicked(Board board)
        {
            if (currentBoard == board) return;
            currentBoard = board;
            UpdateBoardLabel();
            UpdateButtonStates();
            if (showTop)
            {
                // When switching boards in Top mode, ensure the list jumps to the top
                forceScrollTopNext = true;
            }
            _ = RefreshAsync();
        }

        private void OnToggleClicked()
        {
            showTop = !showTop; // ON = show top ranked
            UpdateToggleVisual();
            if (showTop)
            {
                // If switching into Top mode without changing board, jump to top after refresh
                forceScrollTopNext = true;
            }
            _ = RefreshAsync();
        }

        private void UpdateToggleVisual()
        {
            if (toggleImage != null)
                toggleImage.sprite = showTop ? onSprite : offSprite;
        }

        private void UpdateBoardLabel()
        {
            if (boardLabel == null) return;
            var text = currentBoard switch
            {
                Board.DistanceReached => "Distance Reached",
                Board.DistanceTravelled => "Distance Travelled",
                Board.Kills => "Kills",
                Board.Tasks => "Tasks",
                _ => string.Empty
            };
            boardLabel.text = text;
        }

        private void UpdateButtonStates()
        {
            if (distanceReachedButton != null) distanceReachedButton.interactable = currentBoard != Board.DistanceReached;
            if (distanceTravelledButton != null) distanceTravelledButton.interactable = currentBoard != Board.DistanceTravelled;
            if (killsButton != null) killsButton.interactable = currentBoard != Board.Kills;
            if (tasksButton != null) tasksButton.interactable = currentBoard != Board.Tasks;
        }

        public void RefreshNow()
        {
            _ = RefreshAsync();
        }

        private string LeaderboardId(Board board)
        {
            return board switch
            {
                Board.DistanceReached => UgsLeaderboardIds.DistanceReached,
                Board.DistanceTravelled => UgsLeaderboardIds.DistanceTravelled,
                Board.Kills => UgsLeaderboardIds.Kills,
                Board.Tasks => UgsLeaderboardIds.Tasks,
                _ => UgsLeaderboardIds.DistanceReached
            };
        }

        private async Task RefreshAsync()
        {
            if (refreshing) return;
            refreshing = true;
            try
            {
                // Ensure pool exists (no-op if already built)
                BuildPoolIfNeeded();

                var id = LeaderboardId(currentBoard);

                if (showTop)
                {
                    var page = await LeaderboardClient.GetTopAsync(limit: poolSize, leaderboardId: id);
                    var results = page?.Results ?? new List<LeaderboardEntry>();
                    var my = await LeaderboardClient.GetMyScoreAsync(id);
                    await PopulateTopAsync(results, my);
                }
                else
                {
                    var around = await LeaderboardClient.GetAroundPlayerAsync(range: poolSize, leaderboardId: id);
                    var results = around?.Results ?? new List<LeaderboardEntry>();
                    var my = await LeaderboardClient.GetMyScoreAsync(id);

                    int? total = null;
                    try
                    {
                        var aroundType = around?.GetType();
                        var totalProp = aroundType?.GetProperty("Total");
                        if (totalProp != null)
                        {
                            var val = totalProp.GetValue(around);
                            if (val is int ti) total = ti;
                        }
                    }
                    catch { }
                    if (total == null)
                    {
                        total = await LeaderboardClient.GetTotalCountAsync(id);
                    }

                    await PopulateAroundAsync(results, my, total);
                }
            }
            catch (Exception)
            {
                // On error, disable all entries but keep panel responsive.
                for (int i = 0; i < entrySlots.Count; i++)
                    if (entrySlots[i] != null) entrySlots[i].SetActive(false);
                if (playerEntry != null) playerEntry.Set("- / -", await GetMyNameSafeAsync(), "-");
            }
            finally
            {
                refreshing = false;
            }
        }

        private async Task PopulateTopAsync(IList<LeaderboardEntry> results, LeaderboardEntry my)
        {
            if (results == null) results = Array.Empty<LeaderboardEntry>();
            var ordered = results.OrderBy(e => e.Rank).ToList();

            // If my rank is within the returned window, inject the dedicated player row
            // and use the pooled rows for the remaining entries (max poolSize in total).
            int? myRank0 = my != null ? (int?)((int)my.Rank) : null;
            bool myInTop = myRank0.HasValue && ordered.Any(e => e.Rank == myRank0.Value);

            if (!myInTop)
            {
                if (playerEntry != null) playerEntry.SetActive(false);

                int count = Math.Min(ordered.Count, poolSize);
                for (int i = 0; i < entrySlots.Count; i++)
                {
                    var slot = entrySlots[i];
                    if (slot == null) continue;
                    if (i < count)
                    {
                        var e = ordered[i];
                        SetSlotSafe(slot, e);
                        slot.SetActive(true);
                    }
                    else
                    {
                        slot.SetActive(false);
                    }
                }
                // Order slots sequentially when player row is hidden
                OrderSlotsSequentially();
                if (forceScrollTopNext)
                {
                    ScrollToTop();
                    forceScrollTopNext = false;
                }
                return;
            }

            // Player is within top; place player row at correct rank and surround with others.
            if (playerEntry != null)
            {
                string rankStr = (my.Rank + 1).ToString("N0");
                string name = !string.IsNullOrWhiteSpace(my.PlayerName) ? my.PlayerName : await GetMyNameSafeAsync();
                string scoreStr = FormatScore(my.Score, currentBoard);
                playerEntry.Set(rankStr, name, scoreStr);
                playerEntry.SetActive(true);
            }

            // Build lists above/below player
            var above = ordered.Where(e => e.Rank < myRank0.Value).OrderBy(e => e.Rank).ToList();
            var below = ordered.Where(e => e.Rank > myRank0.Value).OrderBy(e => e.Rank).ToList();

            // Limit to poolSize-1 rows total since player row occupies one slot visually
            TrimAround(ref above, ref below, Math.Max(0, poolSize - 1));

            // Assign data to slots
            int idx = 0;
            for (; idx < above.Count && idx < entrySlots.Count; idx++)
            {
                entrySlots[idx].SetActive(true);
                SetSlotSafe(entrySlots[idx], above[idx]);
            }
            int belowStart = idx;
            for (int j = 0; j < below.Count && (belowStart + j) < entrySlots.Count; j++)
            {
                var slot = entrySlots[belowStart + j];
                slot.SetActive(true);
                SetSlotSafe(slot, below[j]);
            }
            // Hide any remaining slots
            for (int k = belowStart + below.Count; k < entrySlots.Count; k++)
            {
                entrySlots[k].SetActive(false);
            }

            // Set sibling order if player row shares the same parent
            ArrangeAroundPlayer(above.Count, below.Count);
            if (forceScrollTopNext)
            {
                ScrollToTop();
                forceScrollTopNext = false;
            }
            else
            {
                ScrollToPlayer(above.Count, below.Count);
            }
        }

        // New overload that also accepts total count so we can format "Rank / Total"
        private async Task PopulateAroundAsync(IList<LeaderboardEntry> results, LeaderboardEntry my, int? totalCount)
        {
            // Ensure player row is visible, even if no score yet.
            if (playerEntry != null)
            {
                string rankOnly = my != null ? (my.Rank + 1).ToString("N0") : "-";
                string totalStr = totalCount.HasValue ? totalCount.Value.ToString("N0") : "?";
                string rankStr = $"{rankOnly} / {totalStr}";
                string name = my != null && !string.IsNullOrWhiteSpace(my.PlayerName)
                    ? my.PlayerName
                    : await GetMyNameSafeAsync();
                string scoreStr = my != null ? FormatScore(my.Score, currentBoard) : "-";
                playerEntry.Set(rankStr, name ?? "Player", scoreStr);
                playerEntry.SetActive(true);
            }

            // Order neighbors by ascending rank and exclude player's own entry
            var neighbors = results ?? Array.Empty<LeaderboardEntry>();
            int? myRank0 = my != null ? (int?)((int)my.Rank) : null; // normalize to int for comparisons
            var ordered = neighbors
                .Where(e => !myRank0.HasValue || e.Rank != myRank0.Value)
                .OrderBy(e => e.Rank)
                .ToList();

            // Split neighbors around player's rank for layout
            var above = myRank0.HasValue ? ordered.Where(e => e.Rank < myRank0.Value).ToList() : new List<LeaderboardEntry>();
            var below = myRank0.HasValue ? ordered.Where(e => e.Rank > myRank0.Value).ToList() : ordered.ToList();

            // Sort both parts by ascending rank
            above.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            below.Sort((a, b) => a.Rank.CompareTo(b.Rank));

            // Choose how many to show above and below based on boundaries
            TrimAround(ref above, ref below, poolSize);

            // Fill slots: first above, then below
            int idx = 0;
            for (; idx < above.Count && idx < entrySlots.Count; idx++)
            {
                var slot = entrySlots[idx];
                if (slot == null) continue;
                SetSlotSafe(slot, above[idx]);
                slot.SetActive(true);
            }
            int belowStart = idx;
            for (int j = 0; j < below.Count && (belowStart + j) < entrySlots.Count; j++)
            {
                var slot = entrySlots[belowStart + j];
                if (slot == null) continue;
                SetSlotSafe(slot, below[j]);
                slot.SetActive(true);
            }
            for (int k = belowStart + below.Count; k < entrySlots.Count; k++)
            {
                if (entrySlots[k] != null) entrySlots[k].SetActive(false);
            }

            ArrangeAroundPlayer(above.Count, below.Count);
            ScrollToPlayer(above.Count, below.Count);
        }

        private void SetSlotSafe(LeaderboardEntryUI slot, LeaderboardEntry entry)
        {
            if (slot == null || entry == null) return;
            var rankStr = (entry.Rank + 1).ToString("N0");
            var name = string.IsNullOrWhiteSpace(entry.PlayerName) ? "-" : entry.PlayerName;
            var scoreStr = FormatScore(entry.Score, currentBoard);
            slot.Set(rankStr, name, scoreStr);
        }

        private void BuildPoolIfNeeded()
        {
            if (entrySlots == null) entrySlots = new List<LeaderboardEntryUI>(poolSize);

            // If we don't have a container or prefab, try to populate from any existing children
            if (entriesContainer == null || entryPrefab == null)
            {
                RebuildSlotsFromChildren();
                return;
            }

            // Reuse existing child rows first
            RebuildSlotsFromChildren();

            // Instantiate only the missing amount, never exceed poolSize
            int missing = poolSize - entrySlots.Count;
            for (int i = 0; i < missing; i++)
            {
                var row = Instantiate(entryPrefab, entriesContainer);
                row.Set("", "", "");
                row.SetActive(false);
                entrySlots.Add(row);
            }

            // Ensure we never keep more than poolSize references
            if (entrySlots.Count > poolSize)
            {
                entrySlots.RemoveRange(poolSize, entrySlots.Count - poolSize);
            }
        }

        private void RebuildSlotsFromChildren()
        {
            entrySlots ??= new List<LeaderboardEntryUI>(poolSize);
            entrySlots.Clear();

            if (entriesContainer == null) return;

            int childCount = entriesContainer.childCount;
            for (int i = 0; i < childCount && entrySlots.Count < poolSize; i++)
            {
                var child = entriesContainer.GetChild(i);
                var row = child.GetComponent<LeaderboardEntryUI>();
                if (row != null && row != playerEntry)
                {
                    entrySlots.Add(row);
                }
            }
        }

        private async Task<string> GetMyNameSafeAsync()
        {
            try
            {
                var n = await LocalProfile.GetMyDisplayNameAsync();
                return string.IsNullOrWhiteSpace(n) ? "Player" : n;
            }
            catch
            {
                return "Player";
            }
        }

        private void SetSlot(LeaderboardEntryUI slot, LeaderboardEntry entry, bool isPlayer)
        {
            // Legacy wrapper: route to the safe setter
            SetSlotSafe(slot, entry);
        }

        private static string FormatScore(double score, Board board)
        {
            switch (board)
            {
                case Board.DistanceTravelled:
                    // UGS stores as kilometers (int). Convert back to steps.
                    var steps = Math.Floor(score * 1000.0);
                    return steps.ToString("N0") + " Steps";
                case Board.Kills:
                    return Math.Floor(score).ToString("N0") + " Kills";
                case Board.Tasks:
                    return Math.Floor(score).ToString("N0") + " Tasks";
                case Board.DistanceReached:
                default:
                    return Math.Floor(score).ToString("N0");
            }
        }

        private void ArrangeAroundPlayer(int aboveCount, int belowCount)
        {
            if (entriesContainer == null || entrySlots == null || playerEntry == null) return;

            // Only arrange if player row is a child of the same container
            if (playerEntry.transform.parent != entriesContainer) return;

            // Determine the base index (we'll pack the used rows tightly around the player row)
            // First, ensure player row is active and part of the layout
            playerEntry.gameObject.SetActive(true);

            // Move player to the middle of the used block
            // We'll put `aboveCount` rows before it, and `belowCount` rows after it.
            // To make ordering deterministic, set sibling indices in sequence.
            int startIndex = 0;

            // Place the above rows
            for (int i = 0; i < aboveCount && i < entrySlots.Count; i++)
            {
                var row = entrySlots[i];
                if (row == null) continue;
                row.transform.SetParent(entriesContainer, false);
                row.transform.SetSiblingIndex(startIndex);
                startIndex++;
            }

            // Place the player row
            playerEntry.transform.SetSiblingIndex(startIndex);
            startIndex++;

            // Place the below rows
            for (int j = 0; j < belowCount && (j) < entrySlots.Count; j++)
            {
                int idx = aboveCount + j;
                if (idx >= entrySlots.Count) break;
                var row = entrySlots[idx];
                if (row == null) continue;
                row.transform.SetParent(entriesContainer, false);
                row.transform.SetSiblingIndex(startIndex);
                startIndex++;
            }

            // Any remaining pooled rows (unused) can sit after; ensure they're disabled
            for (int k = aboveCount + belowCount; k < entrySlots.Count; k++)
            {
                var row = entrySlots[k];
                if (row != null) row.SetActive(false);
            }
        }

        private void OrderSlotsSequentially()
        {
            if (entriesContainer == null) return;
            int cursor = 0;
            for (int i = 0; i < entrySlots.Count; i++)
            {
                var row = entrySlots[i];
                if (row == null || !row.gameObject.activeSelf) continue;
                row.transform.SetSiblingIndex(cursor);
                cursor++;
            }
        }

        private static void TrimAround(ref List<LeaderboardEntry> above, ref List<LeaderboardEntry> below, int limit)
        {
            if (above == null) above = new List<LeaderboardEntry>();
            if (below == null) below = new List<LeaderboardEntry>();

            int total = above.Count + below.Count;
            if (total <= limit) return;

            // Aim for a balanced window; prefer more below when near the top and more above when near the bottom
            int targetAbove = Math.Min(above.Count, limit / 2);
            int targetBelow = Math.Min(below.Count, limit - targetAbove);

            // If still short, try to take more from the side that has remaining entries
            while (targetAbove + targetBelow < limit)
            {
                if (below.Count > targetBelow)
                    targetBelow++;
                else if (above.Count > targetAbove)
                    targetAbove++;
                else
                    break;
            }

            // Keep only the last N from above (closest to player) and first N from below
            if (above.Count > targetAbove)
                above = above.Skip(Math.Max(0, above.Count - targetAbove)).ToList();
            if (below.Count > targetBelow)
                below = below.Take(targetBelow).ToList();
        }

        private void ScrollToTop()
        {
            if (scrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void ScrollToPlayer(int aboveCount, int belowCount)
        {
            if (scrollRect == null || entriesContainer == null || playerEntry == null)
                return;
            if (!playerEntry.gameObject.activeInHierarchy)
                return;
            if (playerEntry.transform.parent != entriesContainer)
                return; // Only scroll when player row is within the same content

            int totalRows = Mathf.Clamp(aboveCount + 1 + belowCount, 1, poolSize);
            int playerIndex = Mathf.Clamp(aboveCount, 0, totalRows - 1);

            // Normalize so 1 = top, 0 = bottom
            float normalized = (totalRows <= 1) ? 1f : 1f - (playerIndex / (float)(totalRows - 1));
            normalized = Mathf.Clamp01(normalized);

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = normalized;
        }
    }
}

