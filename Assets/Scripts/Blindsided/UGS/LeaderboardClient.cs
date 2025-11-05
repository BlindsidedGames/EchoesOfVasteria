#nullable enable
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using Unity.Services.Core;

namespace Blindsided.UGS
{
    public static class LeaderboardClient
    {
        private const string DefaultId = UgsLeaderboardIds.CompletionTime;

        public static async Task SubmitAsync(double score, string leaderboardId = DefaultId)
        {
#if UNITY_EDITOR
            if (!UgsLeaderboardSettings.IsSubmissionEnabled)
                return;
#endif
            await UgsInitializer.EnsureInitializedAsync();
            // Include version metadata as an object (UGS expects JSON object)
            var metadata = LeaderboardMetadata.Build();
            var options = new AddPlayerScoreOptions { Metadata = metadata };
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
        }

        public static async Task<LeaderboardScoresPage> GetTopAsync(int limit = 50, string leaderboardId = DefaultId)
        {
            await UgsInitializer.EnsureInitializedAsync();
            return await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId,
                new GetScoresOptions { Limit = limit, IncludeMetadata = true });
        }

        public static async Task<LeaderboardScores> GetAroundPlayerAsync(int range = 50, string leaderboardId = DefaultId)
        {
            await UgsInitializer.EnsureInitializedAsync();
            return await LeaderboardsService.Instance.GetPlayerRangeAsync(
                leaderboardId,
                new GetPlayerRangeOptions { RangeLimit = range, IncludeMetadata = true });
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
                return await LeaderboardsService.Instance.GetPlayerScoreAsync(
                    leaderboardId,
                    new GetPlayerScoreOptions { IncludeMetadata = true });
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
