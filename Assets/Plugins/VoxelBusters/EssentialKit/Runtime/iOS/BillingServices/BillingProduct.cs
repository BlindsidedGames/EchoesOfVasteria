#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    internal sealed class BillingProduct : BillingProductBase
    {
        #region Fields
        
        private     string      m_localizedTitle;

        private     string      m_localizedDescription;

        private     BillingPrice      m_price;

        private readonly List<BillingProductOffer> m_offers;

        private     BillingProductSubscriptionInfo m_subscriptionInfo;

        #endregion

        #region Constructors

        public BillingProduct(IntPtr nativeObjectPtr, string id, 
            string platformId, BillingProductType type, string localizedTitle, 
            string localizedDescription, BillingPrice price,
            BillingProductSubscriptionInfo subscriptionInfo,
            List<BillingProductOffer> offers,
            IEnumerable<BillingProductPayoutDefinition> payouts)
            : base(id: id, platformId: platformId, type: type, payouts: payouts)
        {
            // set properties
            NativeObjectRef         = new IosNativeObjectRef(nativeObjectPtr);
            m_localizedTitle        = localizedTitle;
            m_localizedDescription  = localizedDescription;
            m_price                 = price;
            m_offers                = offers;
            m_subscriptionInfo      = subscriptionInfo;
        }

        ~BillingProduct()
        {
            Dispose(false);
        }

        #endregion

        #region Base methods

        protected override string GetLocalizedTitleInternal()
        {
            return m_localizedTitle;
        }

        protected override string GetLocalizedDescriptionInternal()
        {
            return m_localizedDescription;
        }

        protected override BillingPrice GetPriceInternal()
        {
            return m_price;
        }

        protected override IEnumerable<BillingProductOffer> GetOffersInternal()
        {
            return m_offers;
        }

        protected override BillingProductSubscriptionInfo GetSubscriptionInfoInternal()
        {
            return m_subscriptionInfo;
        }


        #endregion
    }
}
#endif