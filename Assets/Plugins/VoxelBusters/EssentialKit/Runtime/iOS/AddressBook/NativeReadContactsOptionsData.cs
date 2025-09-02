#if UNITY_IOS || UNITY_TVOS
using System.Runtime.InteropServices;

namespace VoxelBusters.EssentialKit.AddressBookCore.iOS
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeReadContactsOptionsData
    {
        #region Properties

        public int Limit
        {
            get;
            internal set;
        }

        public int Offset
        {
            get;
            internal set;
        }

        public ReadContactsConstraint Constraints
        {
            get;
            internal set;
        }

        #endregion
    }
}
#endif