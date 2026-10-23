using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
namespace TimelessEchoes.Shop
{
    // Same v1 catalogue as IDS. Nothing is fetched during startup or a simulation tick.
    public sealed class PromotionClient : MonoBehaviour
    {
        public event Action Changed;
        public PromotionCatalog Catalog { get; private set; }
        public string Platform => Application.platform == RuntimePlatform.IPhonePlayer ? "ios" : Application.platform == RuntimePlatform.Android ? "android" : "desktop";
        private PromotionCatalog bundled;
        private double checkedAt, attemptedAt = double.NegativeInfinity;
        private bool refreshing;
        private readonly List<string> history = new();
        private string CacheRoot => Path.Combine(TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotRoot ?? Application.persistentDataPath, "promotions-v1");
        private static double Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public void Initialize()
        {
            if (Catalog != null) return;
            var asset = Resources.Load<TextAsset>("Shop/Promotions/catalog");
            bundled = PromotionCatalog.Parse(asset.text); Resources.UnloadAsset(asset); Catalog = bundled;
            try
            {
                var path = Path.Combine(CacheRoot, "catalog.json");
                var json = ShopFiles.Read(path, 512 * 1024);
                if (json != null) Catalog = PromotionCatalog.Parse(json);
                checkedAt = Math.Min(Now, File.Exists(path) ? new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds() : 0);
                var saved = ShopFiles.Read(Path.Combine(CacheRoot, "history.json"), 16384);
                if (saved != null) history.AddRange((JsonConvert.DeserializeObject<string[]>(saved) ?? Array.Empty<string>()).Where(s => s != null).Take(100));
            }
            catch { Catalog = bundled; checkedAt = 0; }
        }
        public PromotionGame[] Games { get { Initialize(); return Catalog.Eligible(Platform); } }
        public PromotionGame Next()
        {
            var game = Games.OrderBy(g => history.IndexOf(g.id)).FirstOrDefault();
            if (game == null) return null;
            history.Remove(game.id); history.Add(game.id);
            try { ShopFiles.Write(Path.Combine(CacheRoot, "history.json"), JsonConvert.SerializeObject(history)); } catch { }
            return game;
        }
        public void RefreshCatalog(bool force = false)
        {
            Initialize();
            if (TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotSession) return;
            if (refreshing || Now - attemptedAt < 60 || !force && Now - checkedAt < 21600) return;
            StartCoroutine(Refresh());
        }
        private IEnumerator Refresh()
        {
            refreshing = true; attemptedAt = Now;
            byte[] bytes = null;
            yield return Fetch(PromotionCatalog.Url, 512 * 1024, "application/json", value => bytes = value);
            if (bytes != null)
            {
                try
                {
                    var text = Encoding.UTF8.GetString(bytes);
                    var next = PromotionCatalog.Parse(text);
                    Catalog = next; checkedAt = Now;
                    try
                    {
                        ShopFiles.Write(Path.Combine(CacheRoot, "catalog.json"), text);
                        var keep = new HashSet<string>(Games.Select(g => Path.GetFileName(g.banner)));
                        foreach (var path in Directory.EnumerateFiles(CacheRoot, "*.webp")) if (!keep.Contains(Path.GetFileName(path))) File.Delete(path);
                    } catch { }
                    Changed?.Invoke();
                }
                catch { /* A bad response never replaces the last valid catalog. */ }
            }
            refreshing = false;
        }
        public IEnumerator LoadImage(PromotionGame game, Action<Texture2D> done)
        {
            byte[] bytes = null;
            var file = Path.Combine(CacheRoot, Path.GetFileName(game.banner));
            try { if (File.Exists(file) && new FileInfo(file).Length <= 250 * 1024) bytes = File.ReadAllBytes(file); } catch { }
            var texture = Decode(bytes);
            if (texture) { done(texture); yield break; }
            if (bytes != null) try { File.Delete(file); } catch { }
            bytes = null;
            if (bundled.games.Any(g => g.banner == game.banner))
            {
                var asset = Resources.Load<TextAsset>("Shop/Promotions/" + Path.GetFileName(game.banner));
                if (asset) { bytes = asset.bytes; Resources.UnloadAsset(asset); }
                texture = Decode(bytes);
                if (texture) { done(texture); yield break; }
            }
            bytes = null;
            if (TimelessEchoes.Platform.StartupDiagnostics.EditorScreenshotSession) { done(null); yield break; }
            yield return Fetch(PromotionCatalog.Origin + game.banner, 250 * 1024, "image/webp", value => bytes = value);
            texture = Decode(bytes);
            if (texture)
            {
                try
                {
                    Directory.CreateDirectory(CacheRoot);
                    var size = Directory.EnumerateFiles(CacheRoot, "*.webp").Sum(p => new FileInfo(p).Length);
                    if (size + bytes.Length <= 20 * 1024 * 1024) File.WriteAllBytes(file, bytes);
                } catch { }
            }
            else
            {
                var fallback = bundled.games.FirstOrDefault(g => g.id == game.id);
                var asset = fallback == null ? null : Resources.Load<TextAsset>("Shop/Promotions/" + Path.GetFileName(fallback.banner));
                if (asset) { texture = Decode(asset.bytes); Resources.UnloadAsset(asset); }
            }
            done(texture);
        }
        private static Texture2D Decode(byte[] bytes)
        {
            if (bytes == null) return null;
            try
            {
                WebP.Texture2DExt.GetWebPDimensions(bytes, out var width, out var height);
                if (width > 0 && height > 0 && width <= 2048 && height <= 2048)
                    return WebP.Texture2DExt.CreateTexture2DFromWebP(bytes, false, false, out _);
            }
            catch { /* Invalid cached art is discarded; text and claiming remain usable. */ }
            return null;
        }
        private static IEnumerator Fetch(string url, int limit, string contentType, Action<byte[]> done)
        {
            using var handler = new BoundedDownload(limit);
            using var request = new UnityWebRequest(url, "GET", handler, null) { timeout = 10, redirectLimit = 0 };
            request.SetRequestHeader("Accept", contentType);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success && (request.GetResponseHeader("Content-Type") ?? "").Split(';')[0].Trim().Equals(contentType, StringComparison.OrdinalIgnoreCase)) done(handler.Bytes);
        }
        private sealed class BoundedDownload : DownloadHandlerScript
        {
            private readonly int limit;
            private readonly MemoryStream bytes = new();
            public byte[] Bytes => bytes.ToArray();
            public BoundedDownload(int limit) : base(new byte[8192]) { this.limit = limit; }
            protected override bool ReceiveData(byte[] data, int count)
            {
                if (data == null || count < 0 || bytes.Length + count > limit) return false;
                bytes.Write(data, 0, count); return true;
            }
            public override void Dispose() { bytes.Dispose(); base.Dispose(); }
        }
    }
}
