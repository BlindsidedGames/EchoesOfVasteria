#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public class NativeNotificationServices : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Private properties
        private NativeActivity Activity
        {
            get;
            set;
        }
        #endregion

        #region Constructor

        public NativeNotificationServices(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
        {
            Activity    = new NativeActivity(context);
        }

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
        public static void ProcessLaunchNotification(NativeNotification notification)
        {
            GetClass().CallStatic(Native.Method.kProcessLaunchNotification, new object[] { notification.NativeObject } );
        }

        #endregion
        #region Public methods

        public bool AreNotificationsAllowedByUser()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : AreNotificationsAllowedByUser]");
#endif
            return Call<bool>(Native.Method.kAreNotificationsAllowedByUser);
        }
        public bool AreRemoteNotificationsAvailable()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : AreRemoteNotificationsAvailable]");
#endif
            return Call<bool>(Native.Method.kAreRemoteNotificationsAvailable);
        }
        public bool AreRemoteNotificationsRegistered()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : AreRemoteNotificationsRegistered]");
#endif
            return Call<bool>(Native.Method.kAreRemoteNotificationsRegistered);
        }
        public bool AreSoundsEnabledByUser()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : AreSoundsEnabledByUser]");
#endif
            return Call<bool>(Native.Method.kAreSoundsEnabledByUser);
        }
        public bool CanScheduleExactTimingNotifications()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : CanScheduleExactTimingNotifications]");
#endif
            return Call<bool>(Native.Method.kCanScheduleExactTimingNotifications);
        }
        public void CancelAllScheduledNotifications()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : CancelAllScheduledNotifications]");
#endif
            Call(Native.Method.kCancelAllScheduledNotifications);
        }
        public void CancelScheduledNotification(string id)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : CancelScheduledNotification]");
#endif
            Call(Native.Method.kCancelScheduledNotification, new object[] { id } );
        }
        public void ClearAllActiveNotifications()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : ClearAllActiveNotifications]");
#endif
            Call(Native.Method.kClearAllActiveNotifications);
        }
        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public long GetNotificationType()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : GetNotificationType]");
#endif
            return Call<long>(Native.Method.kGetNotificationType);
        }
        public void RefreshActiveNotificationsStore()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : RefreshActiveNotificationsStore]");
#endif
            Call(Native.Method.kRefreshActiveNotificationsStore);
        }
        public void RegisterRemoteNotifications(NativeRegisterRemoteNotificationsListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : RegisterRemoteNotifications]");
#endif
            Call(Native.Method.kRegisterRemoteNotifications, new object[] { listener } );
        }
        public void RequestActiveNotifications(NativeNotificationsRequestListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : RequestActiveNotifications]");
#endif
            Call(Native.Method.kRequestActiveNotifications, new object[] { listener } );
        }
        public void RequestNotificationPermissions(NativeRequestNotificationPermissionsListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : RequestNotificationPermissions]");
#endif
            Call(Native.Method.kRequestNotificationPermissions, new object[] { listener } );
        }
        public void RequestScheduledNotifications(NativeNotificationsRequestListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : RequestScheduledNotifications]");
#endif
            Call(Native.Method.kRequestScheduledNotifications, new object[] { listener } );
        }
        public void ScheduleNotification(NativeNotification notification, NativeScheduleNotificationListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : ScheduleNotification]");
#endif
            Call(Native.Method.kScheduleNotification, new object[] { notification?.NativeObject, listener } );
        }
        public void SetApplicationIconBadgeNumber(int count)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeNotificationServices][Method(RunOnUiThread) : SetApplicationIconBadgeNumber]");
#endif
                Call(Native.Method.kSetApplicationIconBadgeNumber, new object[] { count } );
            });
        }
        public void SetNotificationListener(NativeNotificationReceivedListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : SetNotificationListener]");
#endif
            Call(Native.Method.kSetNotificationListener, new object[] { listener } );
        }
        public void SetNotificationType(long type)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : SetNotificationType]");
#endif
            Call(Native.Method.kSetNotificationType, new object[] { type } );
        }
        public void UnregisterRemoteNotifications(NativeUnregisterRemoteNotificationServiceListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeNotificationServices][Method : UnregisterRemoteNotifications]");
#endif
            Call(Native.Method.kUnregisterRemoteNotifications, new object[] { listener } );
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.notificationservices.NotificationServices";

            internal class Method
            {
                internal const string kUnregisterRemoteNotifications = "UnregisterRemoteNotifications";
                internal const string kScheduleNotification = "scheduleNotification";
                internal const string kCanScheduleExactTimingNotifications = "canScheduleExactTimingNotifications";
                internal const string kAreSoundsEnabledByUser = "areSoundsEnabledByUser";
                internal const string kGetNotificationType = "getNotificationType";
                internal const string kSetNotificationType = "setNotificationType";
                internal const string kRefreshActiveNotificationsStore = "refreshActiveNotificationsStore";
                internal const string kCancelAllScheduledNotifications = "cancelAllScheduledNotifications";
                internal const string kAreRemoteNotificationsAvailable = "areRemoteNotificationsAvailable";
                internal const string kProcessLaunchNotification = "processLaunchNotification";
                internal const string kSetNotificationListener = "setNotificationListener";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kRequestActiveNotifications = "requestActiveNotifications";
                internal const string kClearAllActiveNotifications = "clearAllActiveNotifications";
                internal const string kRegisterRemoteNotifications = "registerRemoteNotifications";
                internal const string kAreNotificationsAllowedByUser = "areNotificationsAllowedByUser";
                internal const string kCancelScheduledNotification = "cancelScheduledNotification";
                internal const string kRequestScheduledNotifications = "requestScheduledNotifications";
                internal const string kSetApplicationIconBadgeNumber = "setApplicationIconBadgeNumber";
                internal const string kRequestNotificationPermissions = "requestNotificationPermissions";
                internal const string kAreRemoteNotificationsRegistered = "areRemoteNotificationsRegistered";
            }

        }
    }
}
#endif