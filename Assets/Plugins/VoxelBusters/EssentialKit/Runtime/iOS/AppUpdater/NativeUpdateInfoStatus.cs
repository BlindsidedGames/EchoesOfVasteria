#if UNITY_IOS || UNITY_TVOS
namespace VoxelBusters.EssentialKit.AppUpdaterCore.iOS
{
    public enum NativeUpdateInfoStatus
    {
        Unknown = 0,
        UpdateAvailable = 1,
        UpdateNotAvailable = 2,
        UpdateInProgress = 3
    }
}
#endif