#if UNITY_ANDROID
using System.Diagnostics;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.NotificationServicesCore.Android
{
    public sealed class NotificationCenterInterface : NativeNotificationCenterInterfaceBase, INativeNotificationCenterInterface
    {
#region Fields

        private NativeNotificationServices m_instance = null;
        private NotificationPermissionOptions m_options;

#endregion

#region Constructors

        public NotificationCenterInterface()
            : base(isAvailable: true)
        {
            m_instance = new NativeNotificationServices(NativeUnityPluginUtility.GetContext());
            m_instance.SetNotificationListener(new NativeNotificationReceivedListener()
            {
                onNotificationReceivedCallback = (NativeNotification nativeNotification) =>
                {
                    DebugLogger.Log("Received Notification");
                    SendNotificationReceivedEvent(new Notification(nativeNotification));
                }
            });
        }

#endregion

#region Base class methods

        public override void RequestPermission(NotificationPermissionOptions options, RequestPermissionInternalCallback callback)
        {
            this.m_options = options;
            m_instance.SetNotificationType((long)options);

            m_instance.RequestNotificationPermissions(new NativeRequestNotificationPermissionsListener()
            {
                onSuccessCallback = (NativeNotificationAccessState state) =>
                {
                    callback(state == NativeNotificationAccessState.Authorized ? NotificationPermissionStatus.Authorized : NotificationPermissionStatus.Denied, null); 
                },
                onFailureCallback = (error) =>
                {
                    callback(NotificationPermissionStatus.NotDetermined, error.Convert(NotificationServicesError.kDomain));
                }

            });
        }

        public override void GetSettings(GetSettingsInternalCallback callback)
        {
            bool hasExactTimingSetting = (m_options & NotificationPermissionOptions.ExactTiming) != 0;
            bool allowedByUser = m_instance.AreNotificationsAllowedByUser() && (hasExactTimingSetting ? m_instance.CanScheduleExactTimingNotifications() : true);
            bool arePermissionsUnknown = (m_instance.GetNotificationType() == (int)NativeNotificationType.None);
            bool areAlertsAllowed = (m_options & (NotificationPermissionOptions.Alert | NotificationPermissionOptions.CarPlay | NotificationPermissionOptions.ProvidesAppNotificationSettings | NotificationPermissionOptions.Provisional | NotificationPermissionOptions.CriticalAlert | NotificationPermissionOptions.Announcement)) != 0;

            NotificationSettingsInternal settings = new NotificationSettingsInternal(
                permissionStatus: arePermissionsUnknown ? NotificationPermissionStatus.NotDetermined : allowedByUser ? NotificationPermissionStatus.Authorized : NotificationPermissionStatus.Denied,
                alertSetting: areAlertsAllowed ? NotificationSettingStatus.Enabled : NotificationSettingStatus.Disabled,
                badgeSetting: (m_options & NotificationPermissionOptions.Badge) != 0 ? NotificationSettingStatus.Enabled : EssentialKit.NotificationSettingStatus.Disabled,
                carPlaySetting: NotificationSettingStatus.NotSupported,
                lockScreenSetting: NotificationSettingStatus.NotAccessible,
                notificationCenterSetting: NotificationSettingStatus.Enabled,
                soundSetting: (m_options & NotificationPermissionOptions.Sound) != 0 ? NotificationSettingStatus.Enabled : EssentialKit.NotificationSettingStatus.Disabled,
                criticalAlertSetting: NotificationSettingStatus.NotSupported,
                announcementSetting: NotificationSettingStatus.NotSupported,
                alertStyle: NotificationAlertStyle.Banner,
                previewStyle: NotificationPreviewStyle.NotAccessible,
                exactTimingSetting:  hasExactTimingSetting ? NotificationSettingStatus.Enabled : NotificationSettingStatus.Disabled 
            );

            callback(settings);
        }

        public override IMutableNotification CreateMutableNotification(string notificationId)
        {
            return new MutableNotification(notificationId);
        }

        public override void ScheduleNotification(INotification notification, ScheduleNotificationInternalCallback callback)
        {
            MutableNotification mutableNotification = (MutableNotification)notification;
            NativeNotification nativeNotification = mutableNotification.Build();
            m_instance.ScheduleNotification(nativeNotification, new NativeScheduleNotificationListener()
            {
                onSuccessCallback = () => callback(true, null),
                onFailureCallback = (error) => callback(false, error.Convert(NotificationServicesError.kDomain))
            });
        }

        public override void GetScheduledNotifications(GetNotificationsInternalCallback callback)
        {
            m_instance.RequestScheduledNotifications(new NativeNotificationsRequestListener()
            {
                onSuccessCallback = (nativeNotifications) =>
                {
                    Notification[] notifications = NativeUnityPluginUtility.Map<NativeNotification, Notification>(nativeNotifications.Get());
                    callback(notifications, null);
                },
                onFailureCallback = (error) => callback(null, error.Convert(NotificationServicesError.kDomain))
            });
        }

        public override void CancelScheduledNotification(string notificationId)
        {
            m_instance.CancelScheduledNotification(notificationId);
        }

        public override void CancelAllScheduledNotifications()
        {
            m_instance.CancelAllScheduledNotifications();
        }

        public override void GetDeliveredNotifications(GetNotificationsInternalCallback callback)
        {
            m_instance.RequestActiveNotifications(new NativeNotificationsRequestListener()
            {
                onSuccessCallback = (nativeNotifications) =>
                {
                    Notification[] notifications = NativeUnityPluginUtility.Map<NativeNotification, Notification>(nativeNotifications.Get());
                    callback(notifications, null);
                },
                onFailureCallback = (error) => callback(null, error.Convert(NotificationServicesError.kDomain))
            });
        }

        public override void RemoveAllDeliveredNotifications()
        {
            m_instance.ClearAllActiveNotifications();
        }

        public override void RegisterForPushNotifications(RegisterForPushNotificationsInternalCallback callback)
        {
            m_instance.RegisterRemoteNotifications(new NativeRegisterRemoteNotificationsListener()
            {
                onSuccessCallback = (token) => callback(token, null),
                onFailureCallback = (error) => callback(null, error.Convert(NotificationServicesError.kDomain))
            });
        }

        public override void UnregisterForPushNotifications()
        {
            m_instance.UnregisterRemoteNotifications(null);
        }

        public override bool IsRegisteredForPushNotifications()
        {
            return m_instance.AreRemoteNotificationsRegistered();
        }

        public override void SetApplicationIconBadgeNumber(int count)
        {
            m_instance.SetApplicationIconBadgeNumber(count);
        }

#endregion
    }
}
#endif