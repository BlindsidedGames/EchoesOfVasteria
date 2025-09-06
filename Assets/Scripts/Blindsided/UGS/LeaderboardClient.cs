#nullable enable
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Core;

namespace Blindsided.UGS
{
    public static class LeaderboardClient
    {
        private const string DefaultId = UgsLeaderboardIds.DistanceReached;

        public static async Task SubmitAsync(double score, string leaderboardId = DefaultId)
        {
            await UgsInitializer.EnsureInitializedAsync();
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
        }

        public static async Task<LeaderboardScoresPage> GetTopAsync(int limit = 50, string leaderboardId = DefaultId)
        {
            await UgsInitializer.EnsureInitializedAsync();
            return await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId,
                new GetScoresOptions { Limit = limit });
        }

        public static async Task<LeaderboardScores> GetAroundPlayerAsync(int range = 50, string leaderboardId = DefaultId)
        {
            await UgsInitializer.EnsureInitializedAsync();
            return await LeaderboardsService.Instance.GetPlayerRangeAsync(
                leaderboardId,
                new GetPlayerRangeOptions { RangeLimit = range });
        }

        /// <summary>
        /// Get the signed-in player's current score entry.
        /// Returns null if the player has not posted a score yet.
        /// </summary>
        public static async Task<LeaderboardEntry?> GetMyScoreAsync(string leaderboardId = DefaultId)
        {
            try
            {
                await UgsInitializer.EnsureInitializedAsync();
                return await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == 404)
            {
                // Player has no score yet
                return null;
            }
        }

        /// <summary>
        /// Returns the total number of entries for a leaderboard.
        /// Uses a lightweight page request (limit=1) to read the total count.
        /// </summary>
        public static async Task<int?> GetTotalCountAsync(string leaderboardId = DefaultId)
        {
            try
            {
                await UgsInitializer.EnsureInitializedAsync();
                var page = await LeaderboardsService.Instance.GetScoresAsync(
                    leaderboardId,
                    new GetScoresOptions { Limit = 1 });

                // LeaderboardScoresPage typically exposes Total (total number of scores)
                return page?.Total;
            }
            catch
            {
                return null;
            }
        }
    }
}
