#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeMediaContentSaveOptionsData
    {
        public string DirectoryName
        {
            get;
            internal set;
        }

        public string FileName
        {
            get;
            internal set;
        }
    }
}
#endif