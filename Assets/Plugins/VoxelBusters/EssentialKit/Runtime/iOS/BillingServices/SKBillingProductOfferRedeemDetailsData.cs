#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;


namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SKBillingProductOfferRedeemDetailsData
    {
        public string OfferId
        {
            get;
            internal set;
        }

        public string KeyId
        {
            get;
            internal set;
        }

        public string Nonce
        {
            get;
            internal set;
        }

        public string Signature
        {
            get;
            internal set;
        }

        public long Timestamp
        {
            get;
            internal set;
        }
    }
}
#endif