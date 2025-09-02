using System;

namespace VoxelBusters.CoreLibrary.NativePlugins
{
    public static class NativeErrorExtensions
    {
        public static Error Convert(this NativeError nativeError, string domain = null)
        {
            if(nativeError.DescriptionPtr == IntPtr.Zero)
            {
                return null;
            }

            return new Error(domain, nativeError.Code, nativeError.DescriptionPtr.AsString());
        }
    }
}
