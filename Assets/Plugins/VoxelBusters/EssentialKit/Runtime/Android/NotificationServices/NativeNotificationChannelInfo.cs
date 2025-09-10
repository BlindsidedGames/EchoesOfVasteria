#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public class NativeNotificationChannelInfo : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Public properties

        public string Id
        {
            get
            {
                return Get<string>("id");
            }

            set
            {
                Set<string>("id", value);
            }
        }


        public string Name
        {
            get
            {
                return Get<string>("name");
            }

            set
            {
                Set<string>("name", value);
            }
        }


        public string Description
        {
            get
            {
                return Get<string>("description");
            }

            set
            {
                Set<string>("description", value);
            }
        }


        public string OldId
        {
            get
            {
                return Get<string>("oldId");
            }

            set
            {
                Set<string>("oldId", value);
            }
        }

        #endregion
        #region Constructor

        // Default constructor
        public NativeNotificationChannelInfo(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeNotificationChannelInfo(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeNotificationChannelInfo(string id, string name, string description, string oldId) : base(Native.kClassName ,(object)id, (object)name, (object)description, (object)oldId)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeNotificationChannelInfo()
        {
            DebugLogger.Log("Disposing NativeNotificationChannelInfo");
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
        public static NativeNotificationChannelInfo FromJson(NativeJSONObject jsonObject)
        {
            AndroidJavaObject nativeObj = GetClass().CallStatic<AndroidJavaObject>(Native.Method.kFromJson, jsonObject.NativeObject);
            NativeNotificationChannelInfo data  = new  NativeNotificationChannelInfo(nativeObj);
            return data;
        }

        #endregion
        #region Public methods

        public NativeJSONObject ToJson()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kToJson);
            NativeJSONObject data  = new  NativeJSONObject(nativeObj);
            return data;
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.notificationservices.datatypes.NotificationChannelInfo";

            internal class Method
            {
                internal const string kFromJson = "fromJson";
                internal const string kToJson = "toJson";
            }

        }
    }
}
#endif