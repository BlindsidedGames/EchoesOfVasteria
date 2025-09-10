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
    internal struct SKSubscriptionStatusData
    {
        #region Properties

        public IntPtr GroupIdentifierPtr
        {
            get;
            internal set;
        }
        public IntPtr RenewalInfoPtr
        {
            get;
            internal set;
        }

        public IntPtr ExpirationDatePtr
        {
            get;
            internal set;
        }

        public int IsUpgraded
        {
            get;
            internal set;
        }

        public IntPtr AppliedOfferIdentifier
        {
            get;
            internal set;
        }

        public BillingProductOfferCategory AppliedOfferCategory
        {
            get;
            internal set;
        }

        #endregion
    }
}
#endif