#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;


namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SKBillingPeriodData
    {
        public double Duration
        {
            get;
            internal set;
        }


        public int Unit
        {
            get;
            internal set;
        }
    }
}
#endif