//
//  NPAppUpdaterBinding.mm
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPKit.h"
#import "NPTaskServices.h"

// callback signatures
typedef void (*BackgroundProcessingUpdateNativeCallback)();
typedef void (*BackgroundQuotaWillExpireNativeCallback)(void* tagPtr);

static NPTaskServices* cachedTaskServices;

NPTaskServices* getTaskServices()
{
    if(cachedTaskServices == nil)
    {
        cachedTaskServices = [[NPTaskServices alloc] init];
    }
    
    return cachedTaskServices;
}

NPBINDING DONTSTRIP void NPTaskServicesSetBackgrounProcessingUpdateLoopCallback(BackgroundProcessingUpdateNativeCallback callback)
{
    [NPTaskServices setBackgroundProcessingUpdateLoopCallback:^{
        if(callback != nil)
        {
            callback();
        }
    }];
}


NPBINDING DONTSTRIP void NPTaskServicesStartTaskWithoutInterruption(const char* taskId, BackgroundQuotaWillExpireNativeCallback callback, void* tagPtr)
{
    NPTaskServices *taskServices = getTaskServices();
    [taskServices startBackgroundTask:NPCreateNSStringFromCString(taskId) withExpiryCallback:^{
        if(callback != nil)
        {
            callback(tagPtr);
        }
    }];
}

NPBINDING DONTSTRIP void NPTaskServicesCancelTask(const char* taskId)
{
    NPTaskServices *taskServices = getTaskServices();
    [taskServices cancelTask:NPCreateNSStringFromCString(taskId)];
}
