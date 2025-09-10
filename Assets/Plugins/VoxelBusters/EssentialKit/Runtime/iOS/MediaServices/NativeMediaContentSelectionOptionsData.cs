#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeMediaContentSelectionOptionsData
    {
        public string Title
        {
            get;
            internal set;
        }

        public string AllowedMimeType
        {
            get;
            internal set;
        }

        public int MaxAllowed
        {
            get;
            internal set;
        }

        public bool ShowPrepermissionDialog
        {
            get;
            internal set;
        }
    }
}
#endif