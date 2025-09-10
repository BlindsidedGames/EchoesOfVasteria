#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public class NativeNotificationBuilder : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Constructor

        // Default constructor
        public NativeNotificationBuilder(AndroidJavaObject androidJavaObject) : base(Native.kClassName, androidJavaObject)
        {
        }
        public NativeNotificationBuilder(NativeAndroidJavaObjectWrapper wrapper) : base(wrapper)
        {
        }
        public NativeNotificationBuilder(string id) : base(Native.kClassName ,(object)id)
        {
        }

#if NATIVE_PLUGINS_DEBUG_ENABLED
        ~NativeNotificationBuilder()
        {
            DebugLogger.Log("Disposing NativeNotificationBuilder");
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
        #region Public methods

        public NativeNotification Build()
        {
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kBuild);
            NativeNotification data  = new  NativeNotification(nativeObj);
            return data;
        }
        public void SetBadge(int value)
        {
            Call(Native.Method.kSetBadge, value);
        }
        public void SetBigPicture(string name)
        {
            Call(Native.Method.kSetBigPicture, name);
        }
        public void SetBody(string body, bool isEncoded)
        {
            Call(Native.Method.kSetBody, body, isEncoded);
        }
        public void SetBody(string body)
        {
            Call(Native.Method.kSetBody, body);
        }
        public void SetChannel(NativeNotificationChannelInfo channel)
        {
            Call(Native.Method.kSetChannel, channel.NativeObject);
        }
        public void SetLargeIcon(string name)
        {
            Call(Native.Method.kSetLargeIcon, name);
        }
        public void SetPriority(NativeNotificationPriority priority)
        {
            Call(Native.Method.kSetPriority, NativeNotificationPriorityHelper.CreateWithValue(priority));
        }
        public void SetSoundFileName(string name)
        {
            Call(Native.Method.kSetSoundFileName, name);
        }
        public void SetTag(string tag)
        {
            Call(Native.Method.kSetTag, tag);
        }
        public void SetTitle(string title)
        {
            Call(Native.Method.kSetTitle, title);
        }
        public void SetTitle(string title, bool isEncoded)
        {
            Call(Native.Method.kSetTitle, title, isEncoded);
        }
        public void SetTrigger(NativeLocationNotificationTrigger trigger)
        {
            Call(Native.Method.kSetTrigger, trigger.NativeObject);
        }
        public void SetTrigger(NativeCalendarNotificationTrigger trigger)
        {
            Call(Native.Method.kSetTrigger, trigger.NativeObject);
        }
        public void SetTrigger(NativeTimeIntervalNotificationTrigger trigger)
        {
            Call(Native.Method.kSetTrigger, trigger.NativeObject);
        }
        public void SetUserInfo(string json)
        {
            Call(Native.Method.kSetUserInfo, json);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.notificationservices.datatypes.NotificationBuilder";

            internal class Method
            {
                internal const string kSetTitle = "setTitle";
                internal const string kSetBadge = "setBadge";
                internal const string kSetLargeIcon = "setLargeIcon";
                internal const string kSetBigPicture = "setBigPicture";
                internal const string kSetBody = "setBody";
                internal const string kSetPriority = "setPriority";
                internal const string kSetUserInfo = "setUserInfo";
                internal const string kSetChannel = "setChannel";
                internal const string kSetTrigger = "setTrigger";
                internal const string kSetTag = "setTag";
                internal const string kSetSoundFileName = "setSoundFileName";
                internal const string kBuild = "build";
            }

        }
    }
}
#endif