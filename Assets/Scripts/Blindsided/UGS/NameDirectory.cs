using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

namespace Blindsided.UGS
{
    public static class NameDirectory
    {
        public static async Task<Dictionary<string, string>> GetDisplayNamesAsync(IEnumerable<string> playerIds)
        {
            await UgsInitializer.EnsureInitializedAsync();
            var ids = playerIds?.Distinct().ToList() ?? new List<string>();
            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var args = new Dictionary<string, object> { { "playerIds", ids } };

            try
            {
                var result = await CloudCodeService.Instance
                    .CallEndpointAsync<Dictionary<string, string>>("GetDisplayNames", args);

                return result ?? new Dictionary<string, string>();
            }
            catch (CloudCodeException ex)
            {
                Debug.LogWarning($"GetDisplayNames Cloud Code failed: {ex.Message}. Falling back to anonymous ids.");
                return new Dictionary<string, string>();
            }
        }
    }
}
