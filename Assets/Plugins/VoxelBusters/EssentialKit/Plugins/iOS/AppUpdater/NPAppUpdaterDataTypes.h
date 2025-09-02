//
//  NPAppUpdaterDataTypes.h
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.


#import "NPKit.h"

typedef enum : NSInteger
{
    NPAppUpdaterUpdateStatusUnknown = 0,
    NPAppUpdaterUpdateStatusAvailable = 1,
    NPAppUpdaterUpdateStatusNotAvailable = 2,
    NPAppUpdaterUpdateStatusInProgress = 3

} NPAppUpdaterUpdateStatus;


struct NativeAppUpdaterUpdateInfoData
{
    NPAppUpdaterUpdateStatus status;
};
typedef NativeAppUpdaterUpdateInfoData NativeAppUpdaterUpdateInfoData;

struct NativeAppUpdaterPromptUpdateOptionsData
{
    BOOL isForceUpdate;
    const char* title;
    const char* message;
    BOOL allowInstallationIfDownloaded;
};
typedef NativeAppUpdaterPromptUpdateOptionsData NativeAppUpdaterPromptUpdateOptionsData;


// callback signatures
typedef void (*RequestUpdateInfoNativeCallback)(NativeAppUpdaterUpdateInfoData info, NPError error, void* tagPtr);
typedef void (*PromptUpdateNativeCallback)(float progress, NPError error, void* tagPtr);


