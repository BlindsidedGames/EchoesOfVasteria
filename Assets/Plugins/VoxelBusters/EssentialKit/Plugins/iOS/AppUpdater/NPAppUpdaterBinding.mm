//
//  NPAppUpdaterBinding.mm
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPKit.h"
#import "NPAppUpdaterDataTypes.h"
#import "NPAppUpdater.h"

static NPAppUpdater* cachedAppUpdater;

NPBINDING DONTSTRIP void NPAppUpdaterCreate(const char* appId)
{
    cachedAppUpdater = [[NPAppUpdater alloc] initWithAppId:NPCreateNSStringFromCString(appId)];
}

NPBINDING DONTSTRIP void NPAppUpdaterRequestUpdateInfo(void* tagPtr, RequestUpdateInfoNativeCallback callback)
{
    [cachedAppUpdater requestUpdateInfo:^(AppUpdateStatusInfo *info, NSError *error) {
            
            NativeAppUpdaterUpdateInfoData infoData;
            infoData.status = (NPAppUpdaterUpdateStatus)info.status;
            
            callback(infoData, NPCreateError(error), tagPtr);
    }];
}

NPBINDING DONTSTRIP void NPAppUpdaterPromptUpdate(NativeAppUpdaterPromptUpdateOptionsData nativeOptions, void* tagPtr, PromptUpdateNativeCallback callback)
{
    PromptUpdateOptions *options = [[PromptUpdateOptions alloc] init];
    options.isForceUpdate   = nativeOptions.isForceUpdate;
    options.title           = NPCreateNSStringFromCString(nativeOptions.title);
    options.message         = NPCreateNSStringFromCString(nativeOptions.message);
    options.allowInstallationIfDownloaded = nativeOptions.allowInstallationIfDownloaded;
    
    [cachedAppUpdater promptUpdate:options
                                   withCallback:^(float progress, NSError *error) {
        callback(progress, NPCreateError(error), tagPtr);
    }];
}
