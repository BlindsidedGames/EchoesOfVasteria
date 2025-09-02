using System;
using System.Collections.Generic;
using System.Text;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    public abstract class BillingProductBase : NativeObjectBase, IBillingProduct
    {
        #region Constructors

        protected BillingProductBase(string id, string platformId, BillingProductType type,
            IEnumerable<BillingProductPayoutDefinition> payouts, bool isAvailable = true)
        {
            // Set properties
            Id              = id;
            PlatformId      = platformId;
            @Type           = type;
            Payouts         = payouts;
            IsAvailable     = isAvailable;
        }

        ~BillingProductBase()
        {
            Dispose(false);
        }

        #endregion

        #region Abstract methods

        protected abstract string GetLocalizedTitleInternal();

        protected abstract string GetLocalizedDescriptionInternal();

        protected abstract BillingPrice GetPriceInternal();

        protected abstract BillingProductSubscriptionInfo GetSubscriptionInfoInternal();

        protected abstract IEnumerable<BillingProductOffer> GetOffersInternal();

        #endregion

        #region Base methods

        public override string ToString()
        {
            //Id, PlatformId, LocalizedTitle, LocalizedDescription, Price, IsAvailable, SubscriptionInfo, Offers, Payouts, Tag
            return string.Format("[Id={0}, PlatformId={1}, LocalizedTitle={2}, LocalizedDescription={3}, Price={4}, IsAvailable={5}, SubscriptionInfo={6}, Offers={7}, Payouts={8}, Tag={9}]",
                Id, PlatformId, LocalizedTitle, LocalizedDescription, Price, IsAvailable, SubscriptionInfo, string.Join(", ", Offers), string.Join(", ", Payouts), Tag);
        }

        #endregion

        #region IBillingProduct implementation

        public string Id { get; private set; }

        public string PlatformId { get; private set; }

        public BillingProductType Type { get; private set; }

        public string LocalizedTitle => GetLocalizedTitleInternal();

        public string LocalizedDescription => GetLocalizedDescriptionInternal();

        public BillingPrice Price => GetPriceInternal();

        public string LocalizedPrice => Price.LocalizedText;

        public string PriceCurrencyCode => Price.Code;

        public bool IsAvailable { get; private set; }

        public IEnumerable<BillingProductPayoutDefinition> Payouts { get; private set; }

        public object Tag => null;

        public string PriceCurrencySymbol => Price.Symbol;

        public BillingProductSubscriptionInfo SubscriptionInfo => GetSubscriptionInfoInternal();

        public IEnumerable<BillingProductOffer> Offers => GetOffersInternal();


        #endregion
    }
}