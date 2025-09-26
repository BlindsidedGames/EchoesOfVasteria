using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models; // <-- Make sure this is imported
using Unity.Services.Authentication;

namespace Blindsided.UGS
{
    public static class LocalProfile
    {
        public static async Task<string> GetMyDisplayNameAsync()
        {
            // Ensure UGS is initialized and the player is signed in
            await UgsInitializer.EnsureInitializedAsync();

            var keys = new HashSet<string> { "display_name" };

            // Loads protected player data entries as Items
            Dictionary<string, Item> data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (data != null && data.TryGetValue("display_name", out var item))
            {
                // Convert the item to string
                return item.Value.GetAsString();
            }

            return null;
        }

        public static async Task<string> GetDisplayNameOrGeneratedAsync()
        {
            var storedName = await GetMyDisplayNameAsync();
            if (!string.IsNullOrWhiteSpace(storedName))
            {
                return storedName;
            }

            try
            {
                var generated = AuthenticationService.Instance.PlayerName;
                return string.IsNullOrWhiteSpace(generated) ? null : generated;
            }
            catch
            {
                return null;
            }
        }
    }
}

