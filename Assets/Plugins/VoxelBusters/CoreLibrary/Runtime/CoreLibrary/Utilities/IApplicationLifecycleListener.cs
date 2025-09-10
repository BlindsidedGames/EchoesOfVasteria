namespace VoxelBusters.CoreLibrary
{
    public interface IApplicationLifecycleListener
    {
        void OnApplicationFocus(bool hasFocus);
        void OnApplicationPause(bool pauseStatus);
        void OnApplicationQuit();
    }
}