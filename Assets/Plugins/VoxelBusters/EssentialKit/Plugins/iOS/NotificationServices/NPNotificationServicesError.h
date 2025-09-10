//
//  NPNotificationServicesError.h
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//
#define Domain @"Notification Services"

typedef enum : NSInteger
{
    NotificationServicesErrorCodeUnknown,
    NotificationServicesErrorCodePermissionNotAvailable,
    NotificationServicesErrorCodeTriggerNotValid,
    NotificationServicesErrorCodeConfigurationError,
    NotificationServicesErrorCodeScheduledTimeNotValid
} NotificationServicesErrorCode;


@interface NPNotificationServicesError : NSError

+(NPNotificationServicesError*) createFrom:(NSError*) error;
+(NPNotificationServicesError*) unknown;
+(NPNotificationServicesError*) permissionNotAvailable;
+(NPNotificationServicesError*) triggerNotValid;
+(NPNotificationServicesError*) configurationError;
+(NPNotificationServicesError*) scheduledTimeNotValid;

@end

