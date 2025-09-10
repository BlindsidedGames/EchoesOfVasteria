//
//  NPAppDelegateListener.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPAppDelegateListener.h"
#import "NPKit.h"
#import "UNNotificationRequest+TriggerType.h"

#define kDefaultPresentationOption  (UNNotificationPresentationOptionBadge | UNNotificationPresentationOptionSound | UNNotificationPresentationOptionAlert)

#define REGISTER_SELECTOR(obj, sel, notif_name)                  \
if([obj respondsToSelector:sel])                            \
    [[NSNotificationCenter defaultCenter]   addObserver:obj \
                                            selector:sel    \
                                            name:notif_name \
                                            object:nil      \
    ];                                                      \


static NPAppDelegateListener*   _sharedListener;
#if NATIVE_PLUGINS_USES_NOTIFICATION
static UNNotification*          _deliveredNotification;
static BOOL                 _deliveredNotificationLaunchStatus;
#endif

@implementation NPAppDelegateListener

@synthesize registerForRemoteNotificationsCompletionHandler = _registerForRemoteNotificationsCompletionHandler;
@synthesize notificationRecievedCompletionHandler           = _notificationRecievedCompletionHandler;
@synthesize presentationOptions                             = _presentationOptions;

+ (NPAppDelegateListener*)sharedListener
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        _sharedListener  = [[NPAppDelegateListener alloc] init];
    });
    
    return _sharedListener;
}

#if NATIVE_PLUGINS_USES_NOTIFICATION
+ (void)load
{
    UnityRegisterAppDelegateListener([NPAppDelegateListener sharedListener]);
}
#endif

#pragma mark - Init methods

- (id)init
{
    self    = [super init];
    if (self)
    {
#if NATIVE_PLUGINS_USES_NOTIFICATION
        self.presentationOptions    = kDefaultPresentationOption;
        [[UNUserNotificationCenter currentNotificationCenter] setDelegate:self];
        
#if defined(UNITY_VERSION) && defined(MAKE_UNITY_VERSION)
    #if (UNITY_VERSION >= MAKE_UNITY_VERSION(2022,1,0))
        REGISTER_SELECTOR(self, @selector(didRegisterForRemoteNotificationsWithDeviceToken:), kUnityDidRegisterForRemoteNotificationsWithDeviceToken);
        REGISTER_SELECTOR(self, @selector(didFailToRegisterForRemoteNotificationsWithError:), kUnityDidFailToRegisterForRemoteNotificationsWithError);
    #endif
#endif
        
#endif
    }
    return self;
}

- (void)dealloc
{
#if NATIVE_PLUGINS_USES_NOTIFICATION
    [[UNUserNotificationCenter currentNotificationCenter] setDelegate:nil];
#endif
}

- (void)setNotificationRecievedCompletionHandler:(NotificationRecievedCompletionHandler)notificationRecievedCompletionHandler
{
#if NATIVE_PLUGINS_USES_NOTIFICATION
    _notificationRecievedCompletionHandler       = notificationRecievedCompletionHandler;
    
    // send cached info
    if (_deliveredNotification)
    {
        _notificationRecievedCompletionHandler(_deliveredNotification, _deliveredNotificationLaunchStatus);
        _deliveredNotification          = nil;
    }
#endif
}

#pragma mark - AppDelegateListener methods

#if NATIVE_PLUGINS_USES_PUSH_NOTIFICATION
- (void)didRegisterForRemoteNotificationsWithDeviceToken:(NSNotification*)notification
{
    NSData*     deviceToken         = (NSData*)notification.userInfo;
    NSString*   deviceTokenStr      = NPExtractTokenFromNSData(deviceToken);
    
    // send event
    if(_registerForRemoteNotificationsCompletionHandler != nil)
    {
        _registerForRemoteNotificationsCompletionHandler(deviceTokenStr, nil);
    }
}

- (void)didFailToRegisterForRemoteNotificationsWithError:(NSNotification*)notification
{
    NSError*     error              = (NSError*)notification.userInfo;
    
    // send event
    if(_registerForRemoteNotificationsCompletionHandler != nil)
    {
        _registerForRemoteNotificationsCompletionHandler(nil, error);
    }
}
#endif

#pragma mark - UNUserNotificationCenterDelegate methods

#if NATIVE_PLUGINS_USES_NOTIFICATION
- (void)userNotificationCenter:(UNUserNotificationCenter*)center willPresentNotification:(UNNotification*)notification withCompletionHandler:(void (^)(UNNotificationPresentationOptions options))completionHandler
{
    [self handleReceivedNotification:notification];
    
    completionHandler(self.presentationOptions);
}
#if !TARGET_OS_TV
- (void)userNotificationCenter:(UNUserNotificationCenter *)center didReceiveNotificationResponse:(UNNotificationResponse *)response withCompletionHandler:(void(^)(void))completionHandler
{
    if(![response.actionIdentifier isEqualToString:UNNotificationDismissActionIdentifier]) {
        [self handleReceivedNotification:response.notification withLaunchStatus: TRUE];
    }
    
    completionHandler();
}
#endif

- (void)handleReceivedNotification:(UNNotification*)notification
{
    [self handleReceivedNotification: notification withLaunchStatus:FALSE];
}

- (void)handleReceivedNotification:(UNNotification*)notification withLaunchStatus: (BOOL)isLaunchNotification
{
#if !NATIVE_PLUGINS_USES_PUSH_NOTIFICATION
    UNNotificationTriggerType   triggerType = [[notification request] triggerType];
    if (triggerType == UNNotificationTriggerTypePushNotification)
    {
        return;
    }
#endif

    // save info if notification is received before initialisation
    if (_notificationRecievedCompletionHandler == nil)
    {
        _deliveredNotification  = notification;
        _deliveredNotificationLaunchStatus = isLaunchNotification;
    }
    else
    {
        NSLog(@"Launch notification? => %d", isLaunchNotification);
        _notificationRecievedCompletionHandler(notification, isLaunchNotification);
    }
}
#endif

@end
