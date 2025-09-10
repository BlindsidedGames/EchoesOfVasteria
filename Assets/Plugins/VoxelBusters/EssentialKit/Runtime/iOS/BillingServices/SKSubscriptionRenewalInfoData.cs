#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SKSubscriptionRenewalInfoData
    {
        #region Properties

        public BillingProductSubscriptionRenewalState State
        {
            get;
            internal set;
        }

        public IntPtr ApplicableOfferIdentifierPtr
        {
            get;
            internal set;
        }

        public BillingProductOfferCategory ApplicableOfferCategory
        {
            get;
            internal set;
        }

        public IntPtr LastRenewedDatePtr
        {
            get;
            internal set;
        }

        public IntPtr LastRenewalIdPtr
        {
            get;
            internal set;
        }

        public int IsAutoRenewEnabled
        {
            get;
            internal set;
        }

        public BillingProductSubscriptionExpirationReason ExpirationReason
        {
            get;
            internal set;
        }

        public IntPtr RenewalDatePtr
        {
            get;
            internal set;
        }

        public IntPtr GracePeriodExpirationDatePtr
        {
            get;
            internal set;
        }

        public BillingProductSubscriptionPriceIncreaseStatus PriceIncreaseStatus
        {
            get;
            internal set;
        }
        #endregion
    }
}
#endif