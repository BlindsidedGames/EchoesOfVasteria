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
    internal struct SKPaymentTransactionData
    {
        #region Properties

        public IntPtr NativeObjectPtr
        {
            get;
            internal set;
        }

        public IntPtr IdentifierPtr
        {
            get;
            internal set;
        }

        public IntPtr DatePtr
        {
            get;
            internal set;
        }

        public BillingTransactionState TransactionState
        {
            get;
            internal set;
        }

        public IntPtr ReceiptPtr
        {
            get;
            internal set;
        }

        public BillingEnvironment Environment
        {
            get;
            internal set;
        }

        public IntPtr ApplicationBundleIdentifierPtr
        {
            get;
            internal set;
        }

        public IntPtr ProductIdentifierPtr
        {
            get;
            internal set;
        }

        public BillingProductType ProductType
        {
            get;
            internal set;
        }

        public int RequestedQuantity
        {
            get;
            internal set;
        }

        public int PurchasedQuantity
        {
            get;
            internal set;
        }

        public IntPtr RevocationDatePtr
        {
            get;
            internal set;
        }

        public BillingProductRevocationReason RevocationReason
        {
            get;
            internal set;
        }

        public IntPtr PurchaseTagPtr
        {
            get;
            internal set;
        }

        public IntPtr SubscriptionStatusPtr
        {
            get;
            internal set;
        }

        public IntPtr RawDataPtr
        {
            get;
            internal set;
        }

        public NativeError ErrorData
        {
            get;
            internal set;
        }

        #endregion
    }
}
#endif