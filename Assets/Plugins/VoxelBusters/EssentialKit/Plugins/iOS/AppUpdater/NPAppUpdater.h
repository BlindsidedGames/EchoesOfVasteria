//
//  NPAppUpdater.h
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#define Domain @"App Updater"

typedef enum : NSInteger
{
    AppUpdateStatusTypeUnknown,
    AppUpdateStatusTypeAvailable,
    AppUpdateStatusTypeNotAvailable,
    AppUpdateStatusTypeInProgress,
    AppUpdateStatusTypeDownloaded,
} AppUpdateStatusType;

typedef enum : NSInteger
{
    AppUpdaterErrorCodeUnknown,
    AppUpdaterErrorCodeNetworkIssue,
    AppUpdaterErrorCodeUpdateNotCompatible,
    AppUpdaterErrorCodeUpdateInfoNotAvaialble,
    AppUpdaterErrorCodeUpdateNotAvailable,
    AppUpdaterErrorCodeUpdateInProgress,
    AppUpdaterErrorCodeUpdateCancelled
} AppUpdaterErrorCode;


@interface AppUpdateStatusInfo : NSObject
@property (nonatomic) AppUpdateStatusType status;
@end


@interface PromptUpdateOptions : NSObject
@property (nonatomic) BOOL isForceUpdate;
@property (strong, nonatomic) NSString* title;
@property (strong, nonatomic) NSString* message;
@property (nonatomic) BOOL allowInstallationIfDownloaded;
@end

@interface NPAppUpdater : NSObject

@property (strong, nonatomic) NSString* appId;


-(NPAppUpdater*) initWithAppId:(NSString*) appId;

-(void) requestUpdateInfo:(void (^) (AppUpdateStatusInfo* info, NSError* error)) callback;

-(void) promptUpdate:(PromptUpdateOptions*) options withCallback:(void (^) (float progress, NSError* error)) callback;

@end
