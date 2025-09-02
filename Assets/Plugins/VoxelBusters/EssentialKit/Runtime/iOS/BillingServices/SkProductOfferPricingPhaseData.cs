#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SkProductOfferPricingPhaseData
    {
        public int PaymentMode
        {
            get;
            internal set;
        }

        public SKPriceData Price
        {
            get;
            internal set;
        }

        public SKBillingPeriodData Period
        {
            get;
            internal set;
        }

        public int RepeatCount
        {
            get;
            internal set;
        }
    }
}
#endif