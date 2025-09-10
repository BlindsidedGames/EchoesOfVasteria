using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.CoreLibrary.NativePlugins
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeError
    {
        #region Properties

        public int Code { get; set; }

        public IntPtr DescriptionPtr { get; set; }

        #endregion
    }
}
