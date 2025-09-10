//
//  NPMediaContent.h
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//

#define Domain @"Media Services"

typedef enum : NSInteger
{
    MediaServicesErrorCodeUnknown,
    MediaServicesErrorCodePermissionNotAvailable,
    MediaServicesErrorCodeUserCancelled,
    MediaServicesErrorCodeDataNotAvailable
} MediaServicesErrorCode;


@interface NPMediaServicesError : NSError

+(NPMediaServicesError*) createFrom:(NSError*) error;
+(NPMediaServicesError*) unknown;
+(NPMediaServicesError*) permissionNotAvailable;
+(NPMediaServicesError*) userCancelled;
+(NPMediaServicesError*) dataNotAvailable;

@end
