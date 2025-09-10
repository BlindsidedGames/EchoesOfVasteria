#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;


namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SKBuyProductOptionsData
    {
        public int Quantity
        {
            get;
            internal set;
        }


        public string Tag
        {
            get;
            internal set;
        }

        public SKBillingProductOfferRedeemDetailsData OfferRedeemDetails
        {
            get;
            internal set;
        }
    }
}
#endif