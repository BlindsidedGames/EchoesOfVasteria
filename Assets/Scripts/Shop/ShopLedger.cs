using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TimelessEchoes.Shop
{
    // Installation-level store evidence, deliberately separate from shareable gameplay saves.
    public sealed class ShopLedger
    {
        private readonly string path;
        public ShopOwnership Current { get; private set; } = new();
        public ShopLedger(string path)
        {
            this.path = path;
            try { var text = ShopFiles.Read(path, 256 * 1024); if (text != null) Current = JsonConvert.DeserializeObject<ShopOwnership>(text) ?? new(); }
            catch { }
        }
        public bool Update(Action<ShopOwnership> change)
        {
            var next = JsonConvert.DeserializeObject<ShopOwnership>(JsonConvert.SerializeObject(Current));
            change(next);
            try { ShopFiles.Write(path, JsonConvert.SerializeObject(next)); }
            catch { return false; }
            Current = next; return true;
        }
        public bool Fulfill(ShopSettings settings, string productId, string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;
            if (productId == settings.permanentProductId) return Update(next => next.Permanent = true);
            if (Array.IndexOf(settings.supporterProductIds, productId) < 0) return false;
            if (Array.IndexOf(Current.Transactions ?? Array.Empty<string>(), transactionId) >= 0) return true;
            return Update(next =>
            {
                var transactions = new List<string>(next.Transactions ?? Array.Empty<string>()) { transactionId };
                next.Transactions = transactions.ToArray(); next.SupporterPurchases++;
            });
        }
    }
}
