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
    internal struct SKSubscriptionInfoData
    {
        #region Properties

        public IntPtr NativeObjectPtr
        {
            get;
            internal set;
        }

        public IntPtr GroupIdentifierPtr
        {
            get;
            internal set;
        }

        public IntPtr LocalizedGroupTitlePtr
        {
            get;
            internal set;
        }

        public int Level
        {
            get;
            internal set;
        }

        public SKBillingPeriodData Period
        {
            get;
            internal set;
        }

        #endregion
    }
}
#endif