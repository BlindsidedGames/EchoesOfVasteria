#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;
namespace VoxelBusters.EssentialKit.NativeUICore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeTextInputFieldOptions
    {
        #region Properties

        public string               Text { get; set; }
        public string               PlaceholderText { get; set; }
        public int                  IsSecured { get; set; }
        public KeyboardInputType    KeyboardInputType { get; set; }

        #endregion
    }
}
#endif