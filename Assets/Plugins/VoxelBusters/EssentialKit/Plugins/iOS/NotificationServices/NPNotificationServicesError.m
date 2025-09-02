//
//  NPNotificationServicesError.mm
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//


#import "NPNotificationServicesError.h"
#import <UserNotifications/UNError.h>

@interface NPNotificationServicesError ()

+(NPNotificationServicesError*) createFrom:(NotificationServicesErrorCode) code withDescription:(NSString*) errorDescription;

@end

@implementation NPNotificationServicesError

+(NPNotificationServicesError*) createFrom:(NSError*) error
{
    if(error == nil)
        return nil;
    
    NotificationServicesErrorCode errorCode;
    
    switch (error.code) {

        case UNErrorCodeNotificationsNotAllowed:
            errorCode = NotificationServicesErrorCodePermissionNotAvailable;
            break;
        case UNErrorCodeNotificationInvalidNoDate:
            errorCode = NotificationServicesErrorCodeScheduledTimeNotValid;
            break;
            
        default:
            NSLog(@"[Notification Services] %ld error code not handled - Inform developer to handle this error code: ", error.code);
            errorCode = NotificationServicesErrorCodeUnknown;
            break;
    }
    
    return [NPNotificationServicesError createFrom:errorCode
                           withDescription:error.description];
}

+(NPNotificationServicesError*) unknown
{
    return [NPNotificationServicesError createFrom: NotificationServicesErrorCodeUnknown withDescription: @"Unknown error"];
}

+(NPNotificationServicesError*) permissionNotAvailable
{
    return [NPNotificationServicesError createFrom: NotificationServicesErrorCodePermissionNotAvailable withDescription: @"Permission not available"];
}

+(NPNotificationServicesError*) triggerNotValid
{
    return [NPNotificationServicesError createFrom: NotificationServicesErrorCodeTriggerNotValid withDescription: @"Invalid trigger specified"];
}

+(NPNotificationServicesError*) configurationError
{
    return [NPNotificationServicesError createFrom: NotificationServicesErrorCodeConfigurationError withDescription: @"Check your configuration once"];
}

+(NPNotificationServicesError*) scheduledTimeNotValid
{
    return [NPNotificationServicesError createFrom: NotificationServicesErrorCodeScheduledTimeNotValid withDescription: @"Scheduled at invalid time"];
}

+(NPNotificationServicesError*) createFrom:(NotificationServicesErrorCode) code withDescription:(NSString*) errorDescription
{
    return [NPNotificationServicesError errorWithDomain:Domain
                                           code:(int)code
                                       userInfo:@{NSLocalizedDescriptionKey: errorDescription}];
}

@end
