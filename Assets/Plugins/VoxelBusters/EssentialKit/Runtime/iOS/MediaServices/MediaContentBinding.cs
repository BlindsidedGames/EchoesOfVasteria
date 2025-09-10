#if UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
{
    internal static class MediaContentBinding
    {
        [DllImport("__Internal")]
        public static extern void NPMediaContentAsRawData(IntPtr mediaContentPtr, IntPtr tag, MediaContentGetRawDataNativeCallback callback);

        [DllImport("__Internal")]
        public static extern void NPMediaContentAsFilePath(IntPtr mediaContentPtr, string directoryName, string fileName, IntPtr tag, MediaContentAsFilePathNativeCallback callback);
    }
}
#endif