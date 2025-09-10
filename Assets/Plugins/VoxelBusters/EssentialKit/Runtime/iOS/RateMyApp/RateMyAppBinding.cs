#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.RateMyAppCore.iOS
{
    internal static class RateMyAppBinding 
    {
        [DllImport("__Internal")]
        public static extern void NPStoreReviewRequestReview(string storeId);
    }
}
#endif