using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
// key namespaces
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit;
// internal namespace
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
    public class NotificationServicesDemo : DemoActionPanelBase<NotificationServicesDemoAction, NotificationServicesDemoActionType>
    {
        #region Fields

        [SerializeField]
        private     RectTransform[]     m_accessDependentObjects                = null;

        [SerializeField]
        private     InputField          m_idInputField                          = null;

        [SerializeField]
        private     InputField          m_titleInputField                       = null;

        [SerializeField]
        private     InputField          m_timeIntervalInputField                = null;

        [SerializeField]
        private     CalendarTriggerFields m_calendarTriggerFields               = null;

        [SerializeField]
        private     InputField          m_cancelNotificationIdInputField        = null;

        [SerializeField]
        private     Dropdown            m_notificationPriorityDropdown          = null;

        [SerializeField]
        private     Toggle              m_repeatToggle                          = null;

        #endregion

        #region Base class methods

        protected override void Start()
        {
            base.Start();

            // set object state
            NotificationServices.GetSettings((result) => OnPermissionStatusChange(result.Settings.PermissionStatus));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // register for events
            NotificationServices.OnNotificationReceived += OnNotificationReceived;
            NotificationServices.OnSettingsUpdate += OnSettingsUpdate;

        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // register for events
            NotificationServices.OnNotificationReceived -= OnNotificationReceived;
            NotificationServices.OnSettingsUpdate -= OnSettingsUpdate;
        }

        protected override void OnActionSelectInternal(NotificationServicesDemoAction selectedAction)
        {
            string notificationId = m_idInputField.text;
            string notificationTitle = m_titleInputField.text;
            NotificationPriority priority = GetNotificationPriority();

            switch (selectedAction.ActionType)
            {
                case NotificationServicesDemoActionType.RequestAccess:
                    NotificationServices.RequestPermission(NotificationPermissionOptions.Alert | NotificationPermissionOptions.Sound | NotificationPermissionOptions.Badge | NotificationPermissionOptions.ProvidesAppNotificationSettings, callback: (result, error) =>
                    {
                        Log("Request for access finished.");
                        Log("Notification access status: " + result.PermissionStatus);

                        // update ui 
                        OnPermissionStatusChange(result.PermissionStatus);
                    });
                    break;

                case NotificationServicesDemoActionType.GetSettings:
                    NotificationServices.GetSettings((result) =>
                    {
                        var settings = result.Settings;
                        // update console
                        Log(settings.ToString());

                        // update ui 
                        OnPermissionStatusChange(settings.PermissionStatus);
                    });
                    break;

                case NotificationServicesDemoActionType.ScheduleNotificationNow:
                    if (string.IsNullOrEmpty(notificationId))
                    {
                        Log("Provide notification id.");
                        return;
                    }
                    if (string.IsNullOrEmpty(notificationTitle))
                    {
                        Log("Provide notification title.");
                        return;
                    }
                    // create notification
                    //Note: As there can be only one notification channel on Android,only one priority can be used at a time. 
                    //Note: Use High priority only if its really important, else use Medium (default).

                    /*
                    NotificationAndroidProperties androidProperties = new NotificationAndroidProperties();
                    androidProperties.Channel = new NotificationChannel() { Id = "channelId",   Name = "channelName",   Description = "channelDescription",     OldId = "Your old id here which was used earlier before change and want to delete - optional param" };
                    */
                    var scheduleNowNotification = NotificationBuilder.CreateNotification(notificationId)
                        .SetTitle(notificationTitle)
                        .SetUserInfo(new System.Collections.Generic.Dictionary<string, string> { { "Test", "Value"} })
                        .SetBadge(10)
                        .SetPriority(priority)
                        //.SetAndroidProperties(androidProperties)
                        .Create();
                    // schedule
                    NotificationServices.ScheduleNotification(scheduleNowNotification, (success, error) =>
                    {
                        if (success)
                        {
                            Log("Request to schedule notification finished successfully.");
                        }
                        else
                        {
                            Log("Request to schedule notification failed with error. Error: " + error);
                        }
                    });
                    break;

                case NotificationServicesDemoActionType.ScheduleTimeIntervalTriggerNotification:
                    long    timeInterval;
                    long.TryParse(m_timeIntervalInputField.text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out timeInterval);
                    if (string.IsNullOrEmpty(notificationId))
                    {
                        Log("Provide notification id.");
                        return;
                    }
                    if (string.IsNullOrEmpty(notificationTitle))
                    {
                        Log("Provide notification title.");
                        return;
                    }
                    if (timeInterval < 1)
                    {
                        Log("Provide a valid time interval value.");
                        return;
                    }
                    // create notification
                    //Note: As there can be only one notification channel on Android,only one priority can be used at a time. 
                    //Note: Use High priority (default) only if its really important, else use Medium. 
                    var     timeIntervalTriggerNotification        = NotificationBuilder.CreateNotification(notificationId)
                        .SetTitle(notificationTitle)
                        .SetTimeIntervalNotificationTrigger(timeInterval, repeats: m_repeatToggle.isOn)
                        .SetPriority(priority)
                        //.SetSoundFileName("notification.mp3")
                        .Create();

                    // schedule notification
                    NotificationServices.ScheduleNotification(timeIntervalTriggerNotification, (success, error) =>
                    {
                        if (success)
                        {
                            Log("Request to schedule notification completed successfully.");
                        }
                        else
                        {
                            Log("Request to schedule notification failed with error. Error: " + error);
                        }
                    });
                    break;

                case NotificationServicesDemoActionType.ScheduleCalendarTriggerNotification:
                    if (string.IsNullOrEmpty(notificationId))
                    {
                        Log("Provide notification id.");
                        return;
                    }
                    if (string.IsNullOrEmpty(notificationTitle))
                    {
                        Log("Provide notification title.");
                        return;
                    }

                    // create notification
                    //Note: As there can be only one notification channel on Android,only one priority can be used at a time. 
                    //Note: Use High priority (default) only if its really important, else use Medium. 
                    var     calendarTriggerNotification        = NotificationBuilder.CreateNotification(notificationId)
                        .SetTitle(notificationTitle)
                        .SetCalendarNotificationTrigger(m_calendarTriggerFields.GetDateComponents(), repeats: m_repeatToggle.isOn)
                        .SetPriority(priority)
                        //.SetSoundFileName("notification.mp3")
                        .Create();

                    // schedule notification
                    NotificationServices.ScheduleNotification(calendarTriggerNotification, (success, error) =>
                    {
                        if (success)
                        {
                            Log("Request to schedule notification completed successfully.");
                        }
                        else
                        {
                            Log("Request to schedule notification failed with error. Error: " + error);
                        }
                    });
                    break;
                case NotificationServicesDemoActionType.GetScheduledNotifications:
                    NotificationServices.GetScheduledNotifications((result, error) =>
                    {
                        if (error == null)
                        {
                            // show console messages
                            var     notifications   = result.Notifications;
                            Log("Request for fetch scheduled notifications finished successfully.");
                            Log("Total notifications scheduled: " + notifications.Length);
                            Log("Below are the notifications:");
                            for (int iter = 0; iter < notifications.Length; iter++)
                            {
                                var     notification    = notifications[iter];
                                Log(string.Format("[{0}]: {1}", iter, notification));
                                Debug.Log("User info : " + notification.UserInfo);
                            }
                        }
                        else
                        {
                            Log("Request for fetch scheduled notifications failed with error. Error: " + error);
                        }
                    });
                    break;

                case NotificationServicesDemoActionType.CancelScheduledNotification:
                    string  cancelNotificationID    = m_cancelNotificationIdInputField.text;
                    if (string.IsNullOrEmpty(cancelNotificationID))
                    {
                        Log("Provide notification id.");
                        return;
                    }
                    NotificationServices.CancelScheduledNotification(cancelNotificationID);
                    Log("Cancelling notification with id: " + cancelNotificationID);
                    break;

                case NotificationServicesDemoActionType.CancelAllScheduledNotifications:
                    NotificationServices.CancelAllScheduledNotifications();
                    Log("Cancelling all the notifications.");
                    break;

                case NotificationServicesDemoActionType.GetDeliveredNotifications:
                    NotificationServices.GetDeliveredNotifications((result, error) =>
                    {
                        if (error == null)
                        {
                            // show console messages
                            var     notifications   = result.Notifications;
                            Log("Request for fetch delivered notifications finished successfully.");
                            Log("Total notifications received: " + notifications.Length);
                            Log("Below are the notifications:");
                            for (int iter = 0; iter < notifications.Length; iter++)
                            {
                                var     notification    = notifications[iter];
                                Log(string.Format("[{0}]: {1}", iter, notification));
                            }
                        }
                        else
                        {
                            Log("Request for fetch delivered notifications failed with error. Error: " + error);
                        }
                    });
                    break;

                case NotificationServicesDemoActionType.RemoveAllDeliveredNotifications:
                    NotificationServices.RemoveAllDeliveredNotifications();
                    Log("Removing all the delivered notifications.");
                    break;

                case NotificationServicesDemoActionType.DeviceToken:
                    string  deviceToken1    = NotificationServices.CachedSettings.DeviceToken;
                    Log("Device token: " + deviceToken1);
                    break;

                case NotificationServicesDemoActionType.RegisterForRemoteNotifications:
                    NotificationServices.RegisterForPushNotifications((result, error) =>
                    {
                        if (error == null)
                        {
                            Log("Remote notification registration finished successfully. Device token: " + result.DeviceToken);
                        }
                        else
                        {
                            Log("Remote notification registration failed with error. Error: " + error.Description);
                        }
                    });
                    break;

                case NotificationServicesDemoActionType.IsRegisteredForRemoteNotifications:
                    bool    isRegistered    = NotificationServices.IsRegisteredForPushNotifications();
                    Log("Is registered for remote notifications: " + isRegistered);
                    break;

                case NotificationServicesDemoActionType.UnregisterForRemoteNotifications:
                    NotificationServices.UnregisterForPushNotifications();
                    Log("Unregistering from receiving remote notifications.");
                    break;

                case NotificationServicesDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kNotificationServices);
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Private methods

        private void OnPermissionStatusChange(NotificationPermissionStatus newStatus)
        {
            // update UI
            bool    active  = (newStatus == NotificationPermissionStatus.Authorized);
            foreach (var rect in m_accessDependentObjects)
            {
                rect.gameObject.SetActive(active);
            }
        }

        private void OnNotificationReceived(NotificationServicesNotificationReceivedResult data)
        {
            Log(string.Format("{0} received.", data.Notification));
            Log($"Notification received: {data.Notification} | Is Launched Notification: {data.Notification.IsLaunchNotification}");
        }

        private void OnSettingsUpdate(NotificationSettings settings)
        {
            Log(string.Format("Settings Update: {0}", settings));
        }

        private DateComponents CreateDateComponents()
        {
            // 10th minute and 30th second on current hour. If repeats is set to true, repeats on 10th minute and 30th second of every hour
            DateComponents dateComponents = new DateComponents();
            dateComponents.Second = 30;
            //dateComponents.Minute = 10;

            /* Other examples. Considers the max date component for repeating when repeats is set
            // Triggers at : 15th hour of the day, 10th minute and 30th second. If repeats is set to true, repeats on 15th hour of the day, 10th minute and 30th second of every day
            DateComponents dateComponents = new DateComponents();
            dateComponents.Second   = 30;
            dateComponents.Minute   = 10;
            dateComponents.Hour     = 15;


            // Triggers at : 20th day of the month, 15th hour of the day, 10th minute and 30th second. If repeats is set to true, repeats on 20th day of month, 10th hour of the day, 10th minute and 30th second of every month
            DateComponents dateComponents = new DateComponents();
            dateComponents.Second = 30;
            dateComponents.Minute = 10;
            dateComponents.Hour = 15;
            dateComponents.Day = 20;
            */

            return dateComponents;
        }

        private NotificationPriority GetNotificationPriority()
        {
            var priorityStr = m_notificationPriorityDropdown.options[m_notificationPriorityDropdown.value];
            return (NotificationPriority)Enum.Parse(typeof(NotificationPriority), priorityStr.text);
        }

        #endregion
    }
}
