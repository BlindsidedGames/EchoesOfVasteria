//
//  NPAppUpdater.h
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#define Domain @"App Updater"

typedef enum : NSInteger
{
    TaskServicesStatusTypeUnknown,
} TaskServicesStatusType;

typedef enum : NSInteger
{
    TaskServicesErrorCodeUnknown
} TaskServicesErrorCode;

typedef void (^BackgroundProcessingUpdateCallback)();


@interface NPTaskServices : NSObject

+(void) setBackgroundProcessingUpdateLoopCallback:(BackgroundProcessingUpdateCallback) updateCallback;

-(void) startBackgroundTask:(NSString*) taskId withExpiryCallback:(void (^) ()) callback;

-(void) cancelTask:(NSString*) taskId;

@end
