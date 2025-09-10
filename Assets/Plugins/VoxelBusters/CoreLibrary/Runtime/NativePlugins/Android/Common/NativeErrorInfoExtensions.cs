#if UNITY_ANDROID
using System;

namespace VoxelBusters.CoreLibrary.NativePlugins.Android
{
    public static class NativeErrorInfoExtensions
    {
        public static Error Convert(this NativeErrorInfo nativeError, string domain = null)
        {
            return new Error(domain, nativeError.GetCode(), nativeError.GetDescription());
        }
    }
}
#endif