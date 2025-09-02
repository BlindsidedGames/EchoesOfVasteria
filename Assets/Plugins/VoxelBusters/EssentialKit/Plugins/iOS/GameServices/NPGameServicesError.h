//
//  NPGameServicesError.h
//  Native Plugins
//
//  Created by Ayyappa Reddy
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//

#import <GameKit/GameKit.h>
#import "NPGameServicesDataTypes.h"

#define Domain @"Game Services"

typedef enum : NSInteger
{
    GameServicesErrorCodeUnknown,
    GameServicesErrorCodeSystemIssue,
    GameServicesErrorCodeNetworkIssue,
    GameServicesErrorCodeNotAllowed,
    GameServicesErrorCodeDataNotAvailable,
    GameServicesErrorCodeNotSupported,
    GameServicesErrorCodeConfigurationIssue,
    GameServicesErrorCodeInvalidInput,
    GameServicesErrorCodeNotAuthenticated
} GameServicesErrorCode;


@interface NPGameServicesError : NSError

+(NPGameServicesError*) createFrom:(NSError*) error;
+(NPGameServicesError*) unknown;
+(NPGameServicesError*) notAuthenticated;
+(NPGameServicesError*) dataNotAvailable;
+(NPGameServicesError*) notSupported;


@end
