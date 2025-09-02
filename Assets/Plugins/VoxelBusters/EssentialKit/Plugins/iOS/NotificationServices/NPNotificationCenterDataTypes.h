//
//  NPNotificationCenterDataTypes.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#include "NPDefines.h"
#include "NPKit.h"

typedef enum : NSInteger
{
    UNNotificationTriggerTypeUnknown,
    UNNotificationTriggerTypePushNotification,
    UNNotificationTriggerTypeCalendar,
    UNNotificationTriggerTypeTimeInterval,
    UNNotificationTriggerTypeLocation,
} UNNotificationTriggerType;

typedef struct
{
    UNAuthorizationStatus   authorizationStatus;
    UNNotificationSetting   alertSetting;
    UNNotificationSetting   badgeSetting;
    UNNotificationSetting   carPlaySetting;
    UNNotificationSetting   lockScreenSetting;
    UNNotificationSetting   notificationCenterSetting;
    UNNotificationSetting   soundSetting;
    UNNotificationSetting   criticalAlertSetting;
    UNNotificationSetting   announcementSetting;
#if !TARGET_OS_TV
    UNAlertStyle            alertStyle;
#else
    int                     alertStyle;
#endif
    long                    showPreviewsSetting;
} NPUnityNotificationSettings;

typedef enum : NSInteger
{
    NotificationPriorityLow,
    NotificationPriorityMedium,
    NotificationPriorityHigh,
    NotificationPriorityMax
} NPNotificationPriority;


// callback signatures
typedef void (*RequestAuthorizationNativeCallback)(UNAuthorizationStatus status, const NPError error, NPIntPtr tagPtr);

typedef void (*GetSettingsNativeCallback)(NPUnityNotificationSettings* settingsData, NPIntPtr tagPtr);

typedef void (*ScheduleLocalNotificationNativeCallback)(const NPError error, NPIntPtr tagPtr);

typedef void (*GetScheduledNotificationsNativeCallback)(NPArray* arrayPtr, const NPError error, NPIntPtr tagPtr);

typedef void (*GetDeliveredNotificationsNativeCallback)(NPArray* arrayPtr, const NPError error, NPIntPtr tagPtr);

typedef void (*RegisterForRemoteNotificationsNativeCallback)(const NPString deviceToken, const NPError error, NPIntPtr tagPtr);

typedef void (*NotificationReceivedNativeCallback)(NPIntPtr nativePtr, bool isLaunchNotification);
