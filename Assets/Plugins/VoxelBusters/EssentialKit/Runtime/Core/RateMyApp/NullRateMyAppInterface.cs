using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.RateMyAppCore
{
    public class NullRateMyAppInterface : NativeRateMyAppInterfaceBase
    {
        #region Constructors

        public NullRateMyAppInterface()
            : base(isAvailable: false)
        { }

        #endregion

        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("NetworkServices");
        }

        #endregion

        #region Base methods

        public override void RequestStoreReview(string storeId = null)
        {
           LogNotSupported();
        }

        #endregion
    
    }
}