#if UNITY_ANDROID
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.RateMyAppCore.Android
{
    public class RateMyAppInterface : NativeRateMyAppInterfaceBase
    {
        #region Fields

        private NativeStoreReview m_storeReview;

        #endregion

        #region Constructors

        public RateMyAppInterface()
            : base(isAvailable: true)
        {
            m_storeReview = new NativeStoreReview(NativeUnityPluginUtility.GetContext());
        }

        #endregion

        #region Base methods

        public override void RequestStoreReview(string storeId)
        {
            m_storeReview.RequestStoreReview(null);
        }

        #endregion
    }
}
#endif