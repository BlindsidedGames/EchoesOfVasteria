#if UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;
using VoxelBusters.CoreLibrary.NativePlugins;


namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SKProductOfferData
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

        public int Category
        {
            get;
            internal set;
        }
        public NativeArray PricingPhasesArray 
        { 
            get; 
            internal set; 
        }

        #endregion
    }
}
#endif