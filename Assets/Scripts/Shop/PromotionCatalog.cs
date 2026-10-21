using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
namespace TimelessEchoes.Shop
{
    public sealed class PromotionCopy { public string title, description; }
    public sealed class PromotionGame
    {
        public string id, banner;
        public bool enabled;
        public Dictionary<string, PromotionCopy> copy;
        public Dictionary<string, string> links;
        public PromotionCopy Copy(string locale) => copy.FirstOrDefault(p => p.Key.Equals(locale, StringComparison.OrdinalIgnoreCase)).Value
            ?? copy.FirstOrDefault(p => p.Key.Equals((locale ?? "en").Split('-')[0], StringComparison.OrdinalIgnoreCase)).Value ?? copy["en"];
    }
    public sealed class PromotionCatalog
    {
        public const string Origin = "https://www.blindsidedgames.com", Url = Origin + "/promotions/v1/catalog.json";
        public int schemaVersion;
        public string revision;
        public PromotionGame[] games;
        private static readonly HashSet<string> Origins = new(StringComparer.OrdinalIgnoreCase)
        { "https://apps.apple.com", "https://play.google.com", "https://store.steampowered.com", Origin, "https://ids.blindsidedgames.com" };
        private static readonly Regex IdPattern = new("^[a-z0-9-]{1,80}$", RegexOptions.CultureInvariant);
        private static readonly Regex BannerPattern = new("^/promotions/images/[a-z0-9-]+\\.[a-f0-9]{12}\\.webp$", RegexOptions.CultureInvariant);
        public static PromotionCatalog Parse(string json)
        {
            if (string.IsNullOrEmpty(json) || System.Text.Encoding.UTF8.GetByteCount(json) > 512 * 1024) throw new FormatException("Catalog size");
            var result = JsonConvert.DeserializeObject<PromotionCatalog>(json, new JsonSerializerSettings { MaxDepth = 16, TypeNameHandling = TypeNameHandling.None });
            if (result == null || result.schemaVersion != 1 || !Text(result.revision, 100) || result.games == null || result.games.Length > 100) throw new FormatException("Catalog version");
            var ids = new HashSet<string>();
            foreach (var game in result.games)
            {
                if (game == null || game.id == null || !IdPattern.IsMatch(game.id) || !ids.Add(game.id) || game.banner == null || !BannerPattern.IsMatch(game.banner) || game.copy == null || !game.copy.ContainsKey("en") || game.links == null) throw new FormatException("Catalog game");
                foreach (var copy in game.copy.Values) if (copy == null || !Text(copy.title, 150) || !Text(copy.description, 1000)) throw new FormatException("Catalog copy");
                foreach (var link in game.links)
                {
                    if (!new[] { "ios", "android", "web", "desktop" }.Contains(link.Key)) continue;
                    if (!Text(link.Value, 1000) || !Uri.TryCreate(link.Value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0 || !Origins.Contains(uri.GetLeftPart(UriPartial.Authority)) || link.Key == "ios" && uri.Host != "apps.apple.com" || link.Key == "android" && uri.Host != "play.google.com") throw new FormatException("Catalog link");
                }
            }
            return result;
        }
        private static bool Text(string value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
        public PromotionGame[] Eligible(string platform) => games.Where(g => g.enabled && g.id != "echoes" && g.links.TryGetValue(platform, out var link) && !string.IsNullOrEmpty(link)).ToArray();
    }
}
