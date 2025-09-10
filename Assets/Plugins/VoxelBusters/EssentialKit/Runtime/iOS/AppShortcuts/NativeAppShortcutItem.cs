#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;
namespace VoxelBusters.EssentialKit.AppShortcutsCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeAppShortcutItem
    {
        #region Properties

        public string Identifier
        {
            get;
            internal set;
        }

        public string Title
        {
            get;
            internal set;
        }

        public string Subtitle
        {
            get;
            internal set;
        }

        public string IconFileName
        {
            get;
            internal set;
        }

        #endregion
    }
}
#endif