#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.AppUpdaterCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeAppUpdaterPromptOptionsData
    {
        public bool IsForceUpdate
        {
            get;
            internal set;
        }

        public string Title
        {
            get;
            internal set;
        }

        public string Message
        {
            get;
            internal set;
        }

        public bool AllowInstallationIfDownloaded
        {
            get; 
            internal set;
        }
    }
}
#endif