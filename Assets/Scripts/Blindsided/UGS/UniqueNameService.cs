using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.Authentication;

namespace Blindsided.UGS
{
    public static class UniqueNameService
    {
        // Sets the player's public display name (Player Accounts) and mirrors to Cloud Save
        public static async Task<(bool ok, string message)> SetUniqueAsync(string desiredName)
        {
            await UgsInitializer.EnsureInitializedAsync();

            try
            {
                // Update the public Player Name used by services like Leaderboards
                await AuthenticationService.Instance.UpdatePlayerNameAsync(desiredName);

                // Also persist to Cloud Save for local/profile reads already wired up
                var data = new Dictionary<string, object> { { "display_name", desiredName } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                return (true, desiredName);
            }
            catch (System.Exception)
            {
                return (false, "Could not set name. Please try again.");
            }
        }
    }
}
