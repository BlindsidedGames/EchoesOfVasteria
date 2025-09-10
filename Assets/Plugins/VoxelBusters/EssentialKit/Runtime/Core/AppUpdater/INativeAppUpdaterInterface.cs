using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AppUpdaterCore
{
    public interface INativeAppUpdaterInterface : INativeFeatureInterface
    {
        #region Methods

        void RequestUpdateInfo(EventCallback<AppUpdaterUpdateInfo> callback);
                
        void PromptUpdate(PromptUpdateOptions options, EventCallback<float> callback);

        #endregion
    }
}