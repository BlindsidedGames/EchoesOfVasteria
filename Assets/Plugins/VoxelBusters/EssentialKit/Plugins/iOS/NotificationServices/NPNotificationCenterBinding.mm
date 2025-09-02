//
//  NPNotificationCenterBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <UserNotifications/UserNotifications.h>
#import "UNNotificationRequest+TriggerType.h"
#import "UNNotificationSettings+ManagedData.h"
#import "NPAppDelegateListener.h"
#import "NPKit.h"
#import "NPNotificationCenterDataTypes.h"
#import "NPNotificationServicesError.h"

#pragma mark - Native binding methods

static RequestAuthorizationNativeCallback           _requestAuthorizationCallback;
static GetSettingsNativeCallback                    _getSettingsCallback;
static ScheduleLocalNotificationNativeCallback      _scheduleLocalNotificationCallback;
static GetScheduledNotificationsNativeCallback      _getScheduledNotificationsCallback;
static GetDeliveredNotificationsNativeCallback      _getDeliveredNotificationsCallback;
static RegisterForRemoteNotificationsNativeCallback _registerForRemoteNotificationsCallback;
static NotificationReceivedNativeCallback           _notificationReceivedCallback;

NPBINDING DONTSTRIP void NPNotificationCenterRegisterCallbacks(RequestAuthorizationNativeCallback requestAuthorizationCallback,
                                                               GetSettingsNativeCallback getSettingsCallback,
                                                               ScheduleLocalNotificationNativeCallback scheduleLocalNotificationCallback,
                                                               GetScheduledNotificationsNativeCallback getScheduledNotificationsCallback,
                                                               GetDeliveredNotificationsNativeCallback getDeliveredNotificationsCallback,
                                                               RegisterForRemoteNotificationsNativeCallback registerForRemoteNotificationCallback,
                                                               NotificationReceivedNativeCallback notificationReceivedCallback)
{
    // cache callback
    _requestAuthorizationCallback           = requestAuthorizationCallback;
    _getSettingsCallback                    = getSettingsCallback;
    _scheduleLocalNotificationCallback      = scheduleLocalNotificationCallback;
    _getScheduledNotificationsCallback      = getScheduledNotificationsCallback;
    _getDeliveredNotificationsCallback      = getDeliveredNotificationsCallback;
    _registerForRemoteNotificationsCallback = registerForRemoteNotificationCallback;
    _notificationReceivedCallback           = notificationReceivedCallback;
    
    // set remote notifiation callback responder
    __weak NPAppDelegateListener*   delegateListener    = [NPAppDelegateListener sharedListener];
    [delegateListener setNotificationRecievedCompletionHandler:^(UNNotification *notification, BOOL isLaunchNotification) {
            // send callback
            void*   notificationRequestPtr  = (__bridge NPIntPtr)notification.request;
            _notificationReceivedCallback(notificationRequestPtr, isLaunchNotification);
    }];
}

NPBINDING DONTSTRIP void NPNotificationCenterInit(UNNotificationPresentationOptions presentationOptions)
{
    [[NPAppDelegateListener sharedListener] setPresentationOptions:presentationOptions];
}

NPBINDING DONTSTRIP void NPNotificationCenterRequestAuthorization(UNAuthorizationOptions options, NPIntPtr tagPtr)
{
    [[UNUserNotificationCenter currentNotificationCenter] requestAuthorizationWithOptions:options
                                                                        completionHandler:^(BOOL granted, NSError * _Nullable error) {
        // get settings
        [[UNUserNotificationCenter currentNotificationCenter] getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings* _Nonnull settings) {
            // send callback
            _requestAuthorizationCallback(settings.authorizationStatus, NPCreateError([NPNotificationServicesError createFrom:error]), tagPtr);
        }];
    }];
}

NPBINDING DONTSTRIP void NPNotificationCenterGetSettings(NPIntPtr tagPtr)
{
    [[UNUserNotificationCenter currentNotificationCenter] getNotificationSettingsWithCompletionHandler:^(UNNotificationSettings* _Nonnull settings) {
        // send callback
        NPUnityNotificationSettings settingsData    = [settings toManagedData];
        _getSettingsCallback(&settingsData, tagPtr);
    }];
}

NPBINDING DONTSTRIP void NPNotificationCenterScheduleLocalNotification(NPIntPtr notificationRequestPtr, NPIntPtr tagPtr)
{
    UNNotificationRequest*     notificationRequest      = (__bridge UNNotificationRequest*)notificationRequestPtr;
    [[UNUserNotificationCenter currentNotificationCenter] addNotificationRequest:notificationRequest withCompletionHandler:^(NSError* _Nullable error) {
        // send event
        _scheduleLocalNotificationCallback(NPCreateError([NPNotificationServicesError createFrom:error]), tagPtr);
    }];
}

NPBINDING DONTSTRIP void NPNotificationCenterGetScheduledNotifications(NPIntPtr tagPtr)
{
    [[UNUserNotificationCenter currentNotificationCenter] getPendingNotificationRequestsWithCompletionHandler:^(NSArray<UNNotificationRequest*>* _Nonnull requests) {
        // create c array
        NPArray*    cArray  = NPCreateNativeArrayFromNSArray(requests);
        
        // send data
        _getScheduledNotificationsCallback(cArray, NPCreateError(nil), tagPtr);
        
        // release c properties
        delete(cArray);
    }];
}

NPBINDING DONTSTRIP void NPNotificationCenterRemovePendingNotification(const char* notificationId)
{
    [[UNUserNotificationCenter currentNotificationCenter] removePendingNotificationRequestsWithIdentifiers:@[NPCreateNSStringFromCString(notificationId)]];
}

NPBINDING DONTSTRIP void NPNotificationCenterRemoveAllPendingNotifications()
{
    [[UNUserNotificationCenter currentNotificationCenter] removeAllPendingNotificationRequests];
}

NPBINDING DONTSTRIP void NPNotificationCenterRemoveAllDeliveredNotifications()
{
#if !TARGET_OS_TV
    [[UNUserNotificationCenter currentNotificationCenter] removeAllDeliveredNotifications];
#else
    NSLog(@"This functionality is not supported on tvOS");
#endif
}

NPBINDING DONTSTRIP void NPNotificationCenterGetDeliveredNotifications(NPIntPtr tagPtr)
{
#if !TARGET_OS_TV
    [[UNUserNotificationCenter currentNotificationCenter] getDeliveredNotificationsWithCompletionHandler:^(NSArray<UNNotification*>* _Nonnull notifications) {
        // create array of notification requests
        int count = (int)[notifications count];
        NSMutableArray*    notificationRequests    = [NSMutableArray arrayWithCapacity:count];
        for (int iter = 0; iter < count; iter++)
        {
            [notificationRequests addObject:[notifications[iter] request]];
        }
        
        // create c array
        NPArray*    cArray  = NPCreateNativeArrayFromNSArray(notificationRequests);
        
        // send data
        _getDeliveredNotificationsCallback(cArray, NPCreateError(nil), tagPtr);
        
        // release c properties
        delete(cArray);
    }];
#else
    _getDeliveredNotificationsCallback(NPCreateNativeArrayFromNSArray(nil), NPCreateError(0, @"Not supported on tvOS"), tagPtr);
#endif
    
}

NPBINDING DONTSTRIP void NPNotificationCenterRegisterForRemoteNotifications(NPIntPtr tagPtr)
{
    __weak NPAppDelegateListener*  delegateListener    = [NPAppDelegateListener sharedListener];
    [delegateListener setRegisterForRemoteNotificationsCompletionHandler:^(NSString* deviceToken, NSError* error) {
        // send callback
        _registerForRemoteNotificationsCallback(NPCreateCStringFromNSString(deviceToken), NPCreateError([NPNotificationServicesError createFrom:error]), tagPtr);
        
        // reset properties
        [delegateListener setRegisterForRemoteNotificationsCompletionHandler:nil];
    }];
    
    [[UIApplication sharedApplication] registerForRemoteNotifications];
}

NPBINDING DONTSTRIP bool NPNotificationCenterIsRegisteredForRemoteNotifications()
{
    return [[UIApplication sharedApplication] isRegisteredForRemoteNotifications];
}

NPBINDING DONTSTRIP void NPNotificationCenterUnregisterForRemoteNotifications()
{
    [[UIApplication sharedApplication] unregisterForRemoteNotifications];
}

NPBINDING DONTSTRIP void NPNotificationCenterSetApplicationIconBadgeNumber(int count)
{
    [UIApplication sharedApplication].applicationIconBadgeNumber = count;
}
