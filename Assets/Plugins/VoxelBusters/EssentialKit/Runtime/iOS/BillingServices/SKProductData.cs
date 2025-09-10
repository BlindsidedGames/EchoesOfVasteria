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
    internal struct SKProductData
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

        public IntPtr LocalizedTitlePtr
        {
            get;
            internal set;
        }

        public IntPtr LocalizedDescriptionPtr
        {
            get;
            internal set;
        }

        public SKPriceData Price
        {
            get;
            internal set;
        }

        public IntPtr SubscriptionInfoPtr
        {
            get;
            internal set;
        }

        public NativeArray OffersArray
        {
            get;
            internal set;
        }

        #endregion
    }
}
#endif