#if UNITY_ANDROID
using System.Collections.Generic;
using System.Linq;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    internal sealed class BillingProduct : BillingProductBase
    {
#region Fields
        
        private     NativeBillingProduct      m_instance;

#endregion

#region Constructors

        public BillingProduct(string id, BillingProductType type, NativeBillingProduct nativeBillingProduct,
            IEnumerable<BillingProductPayoutDefinition> payouts)
            : base(id: id, platformId: nativeBillingProduct.GetIdentifier(), type, payouts: payouts)
        {
            m_instance = nativeBillingProduct;
        }

        ~BillingProduct()
        {
            Dispose(false);
        }

#endregion

#region Base methods

        protected override string GetLocalizedTitleInternal()
        {
            return m_instance.GetLocalizedTitle();
        }

        protected override string GetLocalizedDescriptionInternal()
        {
            return m_instance.GetLocalizedDescription();
        }

        protected override BillingPrice GetPriceInternal()
        {
            NativeBillingPrice nativePrice = m_instance.GetPrice();
            return new BillingPrice(nativePrice.GetPrice(), nativePrice.GetCurrencyCode(), nativePrice.GetCurrencySymbol(), nativePrice.GetLocalizedDisplay());
        }

        protected override BillingProductSubscriptionInfo GetSubscriptionInfoInternal()
        {
            NativeBillingProductSubscriptionInfo nativeInfo = m_instance.GetSubscriptionInfo();

            if(nativeInfo.IsNull())
                return null;

            string groupId = nativeInfo.GetGroupId();
            string localizedGroupTitle = nativeInfo.GetLocalizedGroupTitle();
            int level = nativeInfo.GetLevel();

            NativeBillingPeriod nativePeriod = nativeInfo.GetPeriod();
            BillingPeriod period = new BillingPeriod(nativePeriod.GetDuration(), (BillingPeriodUnit)nativePeriod.GetUnit());

            return new BillingProductSubscriptionInfo(groupId, localizedGroupTitle, level, period);
        }

        protected override IEnumerable<BillingProductOffer> GetOffersInternal()
        {
            var nativeOffers = m_instance.GetOffers().Get();
            List<BillingProductOffer> offers = new List<BillingProductOffer>();

            foreach (var nativeOffer in nativeOffers)
            {
                var id = nativeOffer.GetId();
                var nativePricingPhases = nativeOffer.GetPricingPhases().Get();
                List<BillingProductOfferPricingPhase> pricingPhases = new List<BillingProductOfferPricingPhase>();
                foreach (var nativePricingPhase in nativePricingPhases)
                {
                    var paymentMode = (BillingProductOfferPaymentMode)nativePricingPhase.GetPaymentMode();
                    var nativePrice = nativePricingPhase.GetPrice();
                    var price = new BillingPrice(nativePrice.GetPrice(), nativePrice.GetCurrencyCode(), nativePrice.GetCurrencySymbol(), nativePrice.GetLocalizedDisplay());
                    var nativePeriod = nativePricingPhase.GetPeriod();
                    var period = new BillingPeriod(nativePeriod.GetDuration(), (BillingPeriodUnit)nativePeriod.GetUnit());
                    var repeatCount = nativePricingPhase.GetRepeatCount();
                    pricingPhases.Add(new BillingProductOfferPricingPhase(paymentMode, price, period, repeatCount));
                }

                var category = (BillingProductOfferCategory)nativeOffer.GetCategory();
                offers.Add(new BillingProductOffer(id, category, pricingPhases));
            }

            return offers.AsEnumerable();
        }

        #endregion
    }
}
#endif