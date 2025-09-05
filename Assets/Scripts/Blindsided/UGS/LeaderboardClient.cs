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

        public static async Task<LeaderboardScoresPage> GetTopAsync(int limit = 20, string leaderboardId = DefaultId)
        {
            await UgsInitializer.EnsureInitializedAsync();
            return await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId,
                new GetScoresOptions { Limit = limit });
        }

        public static async Task<LeaderboardScores> GetAroundPlayerAsync(int range = 10, string leaderboardId = DefaultId)
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
    }
}
