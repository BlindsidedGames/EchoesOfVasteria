#if UNITY_ANDROID
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;


namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    internal sealed class BillingTransaction : BillingTransactionBase
    {
        #region Fields

        private NativeBillingTransaction m_instance;
        private Error m_error;

        #endregion

        #region Constructors

        public BillingTransaction(IBillingProduct product, NativeBillingTransaction nativeBillingTransaction)
            : this(product, nativeBillingTransaction, null)
        {
           
        }

        public BillingTransaction(IBillingProduct product, NativeBillingTransaction nativeBillingTransaction, Error error)
            : base(transactionId: nativeBillingTransaction.GetId(), product: product)
        {
            m_instance = nativeBillingTransaction;
            m_error = error;
        }

        ~BillingTransaction()
        {
            Dispose(false);
        }

        #endregion

        #region Base methods

        protected override DateTime GetTransactionDateUTCInternal()
        {
            DateTime? dateTime = m_instance.GetDate().GetDateTimeOptional(DateTime.MinValue);
            return dateTime.Value;
        }

        protected override BillingTransactionState GetTransactionStateInternal()
        {
            return Converter.From(m_instance.GetState());
        }

        protected override BillingReceiptVerificationState GetReceiptVerificationStateInternal()
        {
            return Converter.From(m_instance.GetVerificationState());
        }

        protected override BillingEnvironment GetEnvironmentInternal()
        {
            return (BillingEnvironment)m_instance.GetEnvironment();
        }

        protected override string GetApplicationBundleIdentifierInternal()
        {
            return Application.identifier;
        }

        protected override int GetPurchasedQuantityInternal()
        {
            return m_instance.GetPurchasedQuantity();
        }

        protected override BillingProductRevocationInfo GetRevocationInfoInternal()
        {
            NativeBillingProductRevocationInfo nativeInfo = m_instance.GetRevocationInfo();
            return nativeInfo.IsNull() ? null : new BillingProductRevocationInfo(dateUTC: nativeInfo.GetDate().GetDateTime(), reason: (BillingProductRevocationReason)nativeInfo.GetReason());
        }

        protected override BillingProductSubscriptionStatus GetSubscriptionStatusInternal()
        {
            return Converter.From(m_instance.GetSubscriptionStatus());
        }

        protected override string GetRawDataInternal()
        {
            return m_instance.GetRawData();
        }

        protected override string GetReceiptInternal()
        {
            return m_instance.GetReceipt();
        }

        protected override Error GetErrorInternal()
        {
            return m_error;
        }

        protected override void SetReceiptVerificationStateInternal(BillingReceiptVerificationState value)
        {
            m_instance.SetVerificationState(Converter.From(value));
        }

        protected override int GetRequestedQuantityInternal()
        {
            return m_instance.GetRequestedQuantity();
        }

        protected override string GetTagInternal()
        {
            return m_instance.GetPurchaseTag();
        }

        #endregion
    }
}
#endif