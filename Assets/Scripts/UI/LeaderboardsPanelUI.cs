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
        [SerializeField] private List<LeaderboardEntryUI> entrySlots = new List<LeaderboardEntryUI>(17);
        [SerializeField] private LeaderboardEntryUI playerEntry;
        [SerializeField] private int centerIndex = 8; // 0-based; middle of 17

        [Header("Board Buttons")] [SerializeField] private Button distanceReachedButton;
        [SerializeField] private Button distanceTravelledButton;
        [SerializeField] private Button killsButton;
        [SerializeField] private Button tasksButton;

        [Header("Toggle (Top vs Around)")] [SerializeField] private Button toggleButton;
        [SerializeField] private Image toggleImage;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;

        [Header("Optional Header Text")] [SerializeField] private TMP_Text boardLabel;

        [SerializeField] private Board currentBoard = Board.DistanceReached;
        [SerializeField] private bool showTop = false; // default OFF => around player

        private bool refreshing;

        private void Awake()
        {
            if (distanceReachedButton != null) distanceReachedButton.onClick.AddListener(() => OnBoardClicked(Board.DistanceReached));
            if (distanceTravelledButton != null) distanceTravelledButton.onClick.AddListener(() => OnBoardClicked(Board.DistanceTravelled));
            if (killsButton != null) killsButton.onClick.AddListener(() => OnBoardClicked(Board.Kills));
            if (tasksButton != null) tasksButton.onClick.AddListener(() => OnBoardClicked(Board.Tasks));
            if (toggleButton != null) toggleButton.onClick.AddListener(OnToggleClicked);
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
            _ = RefreshAsync();
        }

        private void OnToggleClicked()
        {
            showTop = !showTop; // ON = show top ranked
            UpdateToggleVisual();
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
                // Safety: ensure slot count
                EnsureSlotListSize();

                var id = LeaderboardId(currentBoard);

                if (showTop)
                {
                    var page = await LeaderboardClient.GetTopAsync(limit: 17, leaderboardId: id);
                    var results = page?.Results ?? new List<LeaderboardEntry>();
                    PopulateTop(results);
                }
                else
                {
                    var around = await LeaderboardClient.GetAroundPlayerAsync(range: 17, leaderboardId: id);
                    var results = around?.Results ?? new List<LeaderboardEntry>();
                    var my = await LeaderboardClient.GetMyScoreAsync(id);
                    await PopulateAroundAsync(results, my);
                }
            }
            catch (Exception)
            {
                // On error, disable all entries but keep panel responsive.
                for (int i = 0; i < entrySlots.Count; i++)
                    if (entrySlots[i] != null) entrySlots[i].SetActive(false);
                if (playerEntry != null) playerEntry.Set("-", await GetMyNameSafeAsync(), "—");
            }
            finally
            {
                refreshing = false;
            }
        }

        private void PopulateTop(IList<LeaderboardEntry> results)
        {
            // In top mode, hide the colored player row and use all 17 slots for returned entries.
            if (playerEntry != null) playerEntry.SetActive(false);

            int count = Mathf.Min(17, results != null ? results.Count : 0);
            for (int i = 0; i < entrySlots.Count; i++)
            {
                var slot = entrySlots[i];
                if (slot == null) continue;
                if (i < count)
                {
                    var e = results[i];
                    SetSlot(slot, e, isPlayer: false);
                    slot.SetActive(true);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
        }

        private async Task PopulateAroundAsync(IList<LeaderboardEntry> results, LeaderboardEntry my)
        {
            // Ensure player row is visible, even if no score yet.
            if (playerEntry != null)
            {
                string rankStr = my != null ? (my.Rank + 1).ToString("N0") : "-";
                string name = my != null && !string.IsNullOrWhiteSpace(my.PlayerName)
                    ? my.PlayerName
                    : await GetMyNameSafeAsync();
                string scoreStr = my != null ? FormatScore(my.Score, currentBoard) : "—";
                playerEntry.Set(rankStr, name ?? "Player", scoreStr);
                playerEntry.SetActive(true);
            }

            // Partition neighbors above/below the player (excluding player's own row)
            var neighbors = results ?? Array.Empty<LeaderboardEntry>();
            int myRank = my != null ? (int)my.Rank : int.MinValue; // UGS Rank is 0-based

            var above = my != null
                ? neighbors.Where(e => e.Rank < myRank).OrderByDescending(e => e.Rank).ToList()
                : neighbors.ToList();
            var below = my != null
                ? neighbors.Where(e => e.Rank > myRank).OrderBy(e => e.Rank).ToList()
                : new List<LeaderboardEntry>();

            int slotsAbove = Mathf.Clamp(centerIndex, 0, entrySlots.Count);
            int slotsBelow = Mathf.Clamp(entrySlots.Count - centerIndex - 1, 0, entrySlots.Count);

            // Fill above (closest ranks nearest the player)
            for (int i = 0; i < slotsAbove; i++)
            {
                var slot = entrySlots[i];
                if (slot == null) continue;
                if (i < above.Count)
                {
                    // above list is descending; map furthest to top
                    int srcIndex = above.Count - 1 - i;
                    SetSlot(slot, above[srcIndex], isPlayer: false);
                    slot.SetActive(true);
                }
                else
                {
                    slot.SetActive(false);
                }
            }

            // Fill below
            for (int i = 0; i < slotsBelow; i++)
            {
                int slotIndex = centerIndex + 1 + i;
                if (slotIndex < 0 || slotIndex >= entrySlots.Count) break;
                var slot = entrySlots[slotIndex];
                if (slot == null) continue;
                if (i < below.Count)
                {
                    SetSlot(slot, below[i], isPlayer: false);
                    slot.SetActive(true);
                }
                else
                {
                    slot.SetActive(false);
                }
            }

            // Hide the static center slot (occupied visually by the colored player row in scene)
            if (centerIndex >= 0 && centerIndex < entrySlots.Count && entrySlots[centerIndex] != null)
                entrySlots[centerIndex].SetActive(false);
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
            if (slot == null || entry == null) return;

            var rankStr = (entry.Rank + 1).ToString("N0"); // UGS ranks are 0-based
            var name = string.IsNullOrWhiteSpace(entry.PlayerName) ? "—" : entry.PlayerName;
            var scoreStr = FormatScore(entry.Score, currentBoard);
            slot.Set(rankStr, name, scoreStr);
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

        private void EnsureSlotListSize()
        {
            if (entrySlots == null)
                entrySlots = new List<LeaderboardEntryUI>(17);
            // Do not instantiate here; the scene will wire exactly 17 entries.
            // Ensure list capacity but leave nulls untouched if fewer assigned yet.
            if (entrySlots.Count < 17)
            {
                while (entrySlots.Count < 17) entrySlots.Add(null);
            }
        }
    }
}
