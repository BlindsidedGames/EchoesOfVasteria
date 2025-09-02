//
//  NPNotificationBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <UserNotifications/UserNotifications.h>
#import <CoreLocation/CoreLocation.h>
#import "NPNotificationCenterDataTypes.h"
#import "UNNotificationRequest+TriggerType.h"
#import "NPKit.h"

const NSString* kUserInfoKey = @"user_info";

NPBINDING DONTSTRIP NPIntPtr NPNotificationRequestCreate(const char* id, NPIntPtr contentPtr, NPIntPtr triggerPtr)
{
    // create notification request
    UNNotificationContent*     content                  = (__bridge UNNotificationContent*)contentPtr;
    UNNotificationTrigger*     trigger                  = (triggerPtr == nil) ? nil : (__bridge UNNotificationTrigger*)triggerPtr;
    UNNotificationRequest*     notificationRequest      = [UNNotificationRequest requestWithIdentifier:NPCreateNSStringFromCString(id)
                                                                                               content:content
                                                                                               trigger:trigger];
    return NPRetainWithOwnershipTransfer(notificationRequest);
}

NPBINDING DONTSTRIP const char* NPNotificationRequestGetId(NPIntPtr requestPtr)
{
    UNNotificationRequest*              request     = (__bridge UNNotificationRequest*)requestPtr;
    return NPCreateCStringCopyFromNSString(request.identifier);
}

NPBINDING DONTSTRIP UNNotificationTriggerType NPNotificationRequestGetTriggerType(NPIntPtr requestPtr)
{
    UNNotificationRequest*              request     = (__bridge UNNotificationRequest*)requestPtr;
    return [request triggerType];
}

NPBINDING DONTSTRIP NPIntPtr NPNotificationRequestGetContent(NPIntPtr requestPtr)
{
    UNNotificationRequest*              request     = (__bridge UNNotificationRequest*)requestPtr;
    return NPRetainWithOwnershipTransfer(request.content);
}

NPBINDING DONTSTRIP NPIntPtr NPNotificationRequestGetTrigger(NPIntPtr requestPtr)
{
    UNNotificationRequest*              request     = (__bridge UNNotificationRequest*)requestPtr;
    UNNotificationTrigger*              trigger     = request.trigger;
    return trigger ? NPRetainWithOwnershipTransfer(trigger) : nil;
}

NPBINDING DONTSTRIP NPIntPtr NPNotificationContentCreate()
{
    UNMutableNotificationContent*       content     = [[UNMutableNotificationContent alloc] init];
    return NPRetainWithOwnershipTransfer(content);
}

NPBINDING DONTSTRIP const char* NPNotificationContentGetTitle(NPIntPtr contentPtr)
{
#if !TARGET_OS_TV
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    return NPCreateCStringCopyFromNSString(content.title);
#else
    return nil;
#endif
}

NPBINDING DONTSTRIP void NPNotificationContentSetTitle(NPIntPtr contentPtr, const char* value)
{
#if !TARGET_OS_TV
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setTitle:NPCreateNSStringFromCString(value)];
#endif
}

NPBINDING DONTSTRIP const char* NPNotificationContentGetSubtitle(NPIntPtr contentPtr)
{
#if !TARGET_OS_TV
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    return NPCreateCStringCopyFromNSString(content.subtitle);
#else
    return nil;
#endif
}

NPBINDING DONTSTRIP void NPNotificationContentSetSubtitle(NPIntPtr contentPtr, const char* value)
{
#if !TARGET_OS_TV
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setSubtitle:NPCreateNSStringFromCString(value)];
#endif
}

NPBINDING DONTSTRIP const char* NPNotificationContentGetBody(NPIntPtr contentPtr)
{
#if !TARGET_OS_TV
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    return NPCreateCStringCopyFromNSString(content.body);
#else
    return nil;
#endif
}

NPBINDING DONTSTRIP void NPNotificationContentSetBody(NPIntPtr contentPtr, const char* value)
{
#if !TARGET_OS_TV
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setBody:NPCreateNSStringFromCString(value)];
#endif
}

NPBINDING DONTSTRIP int NPNotificationContentGetBadge(NPIntPtr contentPtr)
{
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    return (int)[content.badge integerValue];
}

NPBINDING DONTSTRIP void NPNotificationContentSetBadge(NPIntPtr contentPtr, int value)
{
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setBadge:[NSNumber numberWithInteger:value]];
}

NPBINDING DONTSTRIP const char* NPNotificationContentGetUserInfo(NPIntPtr contentPtr)
{
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    NSError*                            error;
#if !TARGET_OS_TV
    NSString*                           jsonStr     = NPToJson([content.userInfo objectForKey:kUserInfoKey], &error);
#else
    NSString *jsonStr = nil;
    NSLog(@"User info content is not supported on tvOS");
#endif
    return NPCreateCStringCopyFromNSString(jsonStr);
}

NPBINDING DONTSTRIP void NPNotificationContentSetUserInfo(NPIntPtr contentPtr, const char* jsonStr)
{
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    NSError*                            error;
    id data = NPFromJson(NPCreateNSStringFromCString(jsonStr), &error);
    NSMutableDictionary *userInfo = [NSMutableDictionary dictionary]; //@@ Need to add our data to another dictionary with key kUserInfoKey so that reading will be same for both remote and local notifications
    [userInfo setObject:data forKey:kUserInfoKey];
    
#if !TARGET_OS_TV
    [content setUserInfo:userInfo];
#else
    NSLog(@"User info content is not supported on tvOS");
#endif
}

NPBINDING DONTSTRIP void NPNotificationContentSetSoundName(NPIntPtr contentPtr, const char* soundName)
{
#if !TARGET_OS_TV
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setSound:[UNNotificationSound soundNamed:NPCreateNSStringFromCString(soundName)]];
#else
    NSLog(@"Setting sound name is not supported on tvOS");
#endif
}

NPBINDING DONTSTRIP const char* NPNotificationContentGetLaunchImageName(NPIntPtr contentPtr)
{
#if !TARGET_OS_OSX && !TARGET_OS_TV
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    return NPCreateCStringCopyFromNSString(content.launchImageName);
#else
    return nil;
#endif
}

NPBINDING DONTSTRIP void NPNotificationContentSetLaunchImageName(NPIntPtr contentPtr, const char* value)
{
#if !TARGET_OS_OSX && !TARGET_OS_TV
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setLaunchImageName:NPCreateNSStringFromCString(value)];
#endif
}

NPBINDING DONTSTRIP const char* NPNotificationContentGetCategoryId(NPIntPtr contentPtr)
{
#if !TARGET_OS_TV
    UNNotificationContent*              content     = (__bridge UNNotificationContent*)contentPtr;
    return NPCreateCStringCopyFromNSString(content.categoryIdentifier);
#else
    return nil;
#endif
}

NPBINDING DONTSTRIP bool NPNotificationTriggerGetRepeats(NPIntPtr triggerPtr)
{
    UNNotificationTrigger*              trigger     = (__bridge UNNotificationTrigger*)triggerPtr;
    return trigger.repeats;
}

NPBINDING DONTSTRIP NPIntPtr NPTimeIntervalNotificationTriggerCreate(double interval, bool repeats)
{
    UNTimeIntervalNotificationTrigger*  trigger     = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:interval repeats:repeats];
    return NPRetainWithOwnershipTransfer(trigger);
}

NPBINDING DONTSTRIP void NPTimeIntervalNotificationTriggerGetProperties(NPIntPtr triggerPtr, double* timeInterval, char* nextTriggerDate, bool* repeats)
{
    UNTimeIntervalNotificationTrigger*  trigger     = (__bridge UNTimeIntervalNotificationTrigger*)triggerPtr;
    
    // set values
    *timeInterval   = trigger.timeInterval;
    nextTriggerDate = NPCreateCStringFromNSString(NPCreateNSStringFromNSDate(trigger.nextTriggerDate));
    *repeats        = trigger.repeats;
}

NPBINDING DONTSTRIP NPIntPtr NPCalendarNotificationTriggerCreate(NPUnityDateComponents dateComponents, bool repeats)
{
    // create date component
    NSDateComponents*                   components  = dateComponents.ToNSDateComponents();
    
    // create trigger
    UNCalendarNotificationTrigger*      trigger     = [UNCalendarNotificationTrigger triggerWithDateMatchingComponents:components repeats:repeats];

    NSLog(@"Next trigger date : %@", trigger.nextTriggerDate);

    return NPRetainWithOwnershipTransfer(trigger);
}

NPBINDING DONTSTRIP void NPCalendarNotificationTriggerGetProperties(NPIntPtr triggerPtr, NPUnityDateComponents* dateComponents, char* nextTriggerDate, bool* repeats)
{
    UNCalendarNotificationTrigger*      trigger     = (__bridge UNCalendarNotificationTrigger*)triggerPtr;
    
    // set values
    dateComponents->CopyProperties(trigger.dateComponents);
    nextTriggerDate = NPCreateCStringFromNSString(NPCreateNSStringFromNSDate(trigger.nextTriggerDate));
    *repeats        = trigger.repeats;
}

NPBINDING DONTSTRIP NPIntPtr NPLocationNotificationTriggerCreate(NPUnityCircularRegion regionData, bool notifyOnEntry, bool notifyOnExit, bool repeats)
{
#if NATIVE_PLUGINS_USES_CORE_LOCATION
    // set region info
    CLLocationCoordinate2D              center      = CLLocationCoordinate2DMake(regionData.latitude, regionData.longitude);
    CLCircularRegion*                   region      = [[CLCircularRegion alloc] initWithCenter:center
                                                                                        radius:regionData.radius
                                                                                    identifier:NPCreateNSStringFromCString((const char*)regionData.regionIdPtr)];
    region.notifyOnEntry                            = notifyOnEntry;
    region.notifyOnExit                             = notifyOnExit;
    
    // create trigger
    UNLocationNotificationTrigger*      trigger     = [UNLocationNotificationTrigger triggerWithRegion:region repeats:repeats];
    return NPRetainWithOwnershipTransfer(trigger);
#else
    return nil;
#endif
}

NPBINDING DONTSTRIP void NPLocationNotificationTriggerGetProperties(NPIntPtr triggerPtr, NPUnityCircularRegion* regionData, bool* notifyOnEntry, bool* notifyOnExit, bool* repeats)
{
#if NATIVE_PLUGINS_USES_CORE_LOCATION
    UNLocationNotificationTrigger*      trigger     = (__bridge UNLocationNotificationTrigger*)triggerPtr;
    __weak CLCircularRegion*            region      = (CLCircularRegion*)trigger.region;
    
    // copy properties
    regionData->latitude        = region.center.latitude;
    regionData->longitude       = region.center.longitude;
    regionData->radius          = region.radius;
    regionData->regionIdPtr     = NPCreateCStringFromNSString(region.identifier);
    *notifyOnEntry              = region.notifyOnEntry;
    *notifyOnExit               = region.notifyOnExit;
    *repeats                    = trigger.repeats;
#endif
}

UNNotificationInterruptionLevel getInterruptionLevel(NPNotificationPriority value)
{
    switch (value) {
        case NotificationPriorityLow:
            return UNNotificationInterruptionLevelPassive;
            break;
        case NotificationPriorityMedium:
            return UNNotificationInterruptionLevelActive;
            break;
        case NotificationPriorityHigh:
            return UNNotificationInterruptionLevelTimeSensitive;
            break;
        case NotificationPriorityMax:
            return UNNotificationInterruptionLevelCritical;
            break;
            
        default:
            return UNNotificationInterruptionLevelActive;
            break;
    }
}

NPBINDING DONTSTRIP void NPNotificationContentSetPriority(NPIntPtr contentPtr, NPNotificationPriority value)
{
#if !TARGET_OS_OSX || !TARGET_OS_TV
    UNMutableNotificationContent*       content     = (__bridge UNMutableNotificationContent*)contentPtr;
    [content setInterruptionLevel:getInterruptionLevel(value)];
#endif
}
