using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.CoreLibrary.NativePlugins
{
    public static class IntPtrUtility
    {
        private const string kZuluFormat = "yyyy-MM-dd HH:mm:ss zzz";

        public static string AsString(this IntPtr ptr)
        {
            return MarshalUtility.ToString(ptr);
        }

        public static DateTime AsDateTime(this IntPtr ptr)
        {
            var value = ptr.AsString();
            
            if (value != null)
            {
                return DateTime.ParseExact(value, kZuluFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
            }

            return default;
        }

        public static DateTime? AsOptionalDateTime(this IntPtr ptr)
        {
            if(ptr == IntPtr.Zero)
                return null;

            return AsDateTime(ptr);
        }
        
        public static T AsStruct<T>(this IntPtr ptr) where T : struct
        {
            return MarshalUtility.PtrToStructure<T>(ptr);
        }
    }
}