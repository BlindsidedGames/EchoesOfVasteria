using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    public interface INativeRateMyAppInterface : INativeFeatureInterface
    {
        void RequestStoreReview(string storeId = null);
    }
}