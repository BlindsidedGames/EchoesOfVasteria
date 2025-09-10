using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    [Serializable]
    public sealed class BillingProductPlain : BillingProductBase
    {
        #region Fields

        private     string              m_localizedTitle;
        
        private     string              m_localizedDescription;
        
        private     BillingPrice        m_price;
        
        private     BillingProductSubscriptionInfo m_subscriptionInfo;

        private     IEnumerable<BillingProductOffer>                m_offers;
        private     IEnumerable<BillingProductPayoutDefinition>     m_payouts;

        #endregion

        #region Constructors

        public BillingProductPlain(string id, 
                                   string platformId, 
                                   BillingProductType type,
                                   string localizedTitle, 
                                   string localizedDescription, 
                                   BillingPrice price,
                                   BillingProductSubscriptionInfo subscriptionInfo,
                                   IEnumerable<BillingProductOffer> offers,
                                   IEnumerable<BillingProductPayoutDefinition> payouts)
            : base(id: id, platformId: platformId, type: type, payouts: payouts)
        {
            // set properties
            m_localizedTitle        = localizedTitle;
            m_localizedDescription  = localizedDescription;
            m_price                 = price;
            m_subscriptionInfo      = subscriptionInfo;
            m_offers                = offers;
            m_payouts               = payouts;
        }

        ~BillingProductPlain()
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

        protected override BillingProductSubscriptionInfo GetSubscriptionInfoInternal()
        {
            return m_subscriptionInfo;
        }

        protected override IEnumerable<BillingProductOffer> GetOffersInternal()
        {
            return m_offers;
        }

        #endregion
    }
}