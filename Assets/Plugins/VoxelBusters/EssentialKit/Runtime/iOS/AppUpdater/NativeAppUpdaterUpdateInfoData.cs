#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeAppUpdaterUpdateInfoData
    {
        public NativeUpdateInfoStatus Status
        {
            get;
            internal set;
        }
        public int BuildTag
        {
            get;
            internal set;
        }
    }
}
#endif