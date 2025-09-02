#if UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;


namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SKPriceData
    {
        public double Value
        {
            get;
            internal set;
        }

        public IntPtr CurrencyCodePtr
        {
            get;
            internal set;            
        }

        public IntPtr CurrencySymbolPtr
        {
            get;
            internal set;
        }

        public IntPtr LocalizedPricePtr
        {
            get;
            internal set;
        }
    }
}
#endif