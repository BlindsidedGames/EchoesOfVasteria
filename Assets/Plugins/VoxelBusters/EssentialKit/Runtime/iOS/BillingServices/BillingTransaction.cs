#if UNITY_IOS || UNITY_TVOS
using System;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    internal sealed class BillingTransaction : BillingTransactionBase
    {
        #region Fields

        private readonly    int                                 m_requestedQuantity;
        private readonly    string                              m_tag;

        private readonly    BillingTransactionState             m_transactionState;
        private             BillingReceiptVerificationState     m_verificationState;
        private readonly    DateTime                            m_date;
        private readonly    string                              m_receipt;
        private readonly    BillingEnvironment                  m_environment;
        private readonly    string                              m_applicationBundleIdentifier;
        private readonly    BillingProductType                  m_productType;
        private readonly    int                                 m_purchasedQuantity;
        private readonly    BillingProductRevocationInfo        m_revocationInfo;
        private readonly    BillingProductSubscriptionStatus    m_subscriptionStatus;
        private readonly    string                              m_rawData;
        private readonly    Error                               m_error;

        #endregion

        #region Constructors

        public BillingTransaction(IntPtr nativeObjectPtr, string transactionId, IBillingProduct product,
                                    int requestedQuantity, string tag,
                                    BillingTransactionState transactionState,
                                    BillingReceiptVerificationState verificationState, 
                                    DateTime date,
                                    string receipt, 
                                    BillingEnvironment environment,
                                    string applicationBundleIdentifier,
                                    int purchasedQuantity,
                                    BillingProductRevocationInfo revocationInfo,
                                    BillingProductSubscriptionStatus subscriptionStatus,
                                    string rawData,
                                    Error error)
            : base(transactionId: transactionId, product: product)
        {
            // set properties
            NativeObjectRef                 = new IosNativeObjectRef(nativeObjectPtr, false);
            m_requestedQuantity             = requestedQuantity;
            m_tag                           = tag;
            m_transactionState              = transactionState;
            m_verificationState             = verificationState;
            m_date                          = date;
            m_receipt                       = receipt;
            m_environment                   = environment;
            m_applicationBundleIdentifier   = applicationBundleIdentifier;
            m_purchasedQuantity             = purchasedQuantity;
            m_revocationInfo                = revocationInfo;
            m_subscriptionStatus            = subscriptionStatus;
            m_rawData                       = rawData; 
            m_error                         = error;
        }

        ~BillingTransaction()
        {
            Dispose(false);
        }

        #endregion

        #region Base methods

        protected override DateTime GetTransactionDateUTCInternal()
        {
            return m_date;
        }

        protected override BillingTransactionState GetTransactionStateInternal()
        {
            return m_transactionState;
        }

        protected override BillingReceiptVerificationState GetReceiptVerificationStateInternal()
        {
            return m_verificationState;
        }

        protected override void SetReceiptVerificationStateInternal(BillingReceiptVerificationState value)
        {
            m_verificationState = value;
        }

        protected override string GetReceiptInternal()
        {
            return m_receipt;
        }

        protected override Error GetErrorInternal()
        {
            return m_error;
        }

        protected override BillingEnvironment GetEnvironmentInternal()
        {
            return m_environment;
        }

        protected override string GetApplicationBundleIdentifierInternal()
        {
            return m_applicationBundleIdentifier;
        }

        protected override int GetPurchasedQuantityInternal()
        {
            return m_purchasedQuantity;
        }

        protected override BillingProductRevocationInfo GetRevocationInfoInternal()
        {
            return m_revocationInfo;
        }
        
        protected override BillingProductSubscriptionStatus GetSubscriptionStatusInternal()
        {
            return m_subscriptionStatus;
        }

        protected override string GetRawDataInternal()
        {
            return m_rawData;
        }

        protected override int GetRequestedQuantityInternal()
        {
            return m_requestedQuantity;
        }

        protected override string GetTagInternal()
        {
            return m_tag;
        }


        #endregion
    }
}
#endif