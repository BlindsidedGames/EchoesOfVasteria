#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.AddressBookCore.Android
{
    public class NativeAddressBookReadOptions : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public int Limit
        {
            get
            {
                return Get<int>("limit");
            }

            set
            {
                Set<int>("limit", value);
            }
        }


        public int Offset
        {
            get
            {
                return Get<int>("offset");
            }

            set
            {
                Set<int>("offset", value);
            }
        }


        public long Constraints
        {
            get
            {
                return Get<long>("constraints");
            }

            set
            {
                Set<long>("constraints", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativeAddressBookReadOptions(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeAddressBookReadOptions(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeAddressBookReadOptions() : base(Native.kClassName)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeAddressBookReadOptions()
        {
            DebugLogger.Log("Disposing NativeAddressBookReadOptions");
        }
#endif
        #endregion
        #region Static methods
        private static AndroidJavaClass GetClass()
        {
            if (m_nativeClass == null)
            {
                m_nativeClass = new AndroidJavaClass(Native.kClassName);
            }
            return m_nativeClass;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.addressbook.AddressBookReadOptions";

            internal class Method
            {
            }

        }
    }
}
#endif