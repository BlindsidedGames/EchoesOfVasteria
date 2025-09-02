//
//  NPAppUpdater.mm
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPKit.h"
#import "NPTaskServices.h"
#import "UnityInterface.h"
#import "NSError+Utility.h"


static BackgroundProcessingUpdateCallback backgroundProcessingUpdateCallback;


@interface NPTaskServices ()

//@property(nonatomic, strong) AppUpdateStatusInfo* cachedStatusInfo;
@property(nonatomic, strong) NSMutableDictionary *cachedTaskIdentifiers;
@property(nonatomic, strong) NSTimer *updateTimer;

@end

@implementation NPTaskServices

@synthesize cachedTaskIdentifiers;
@synthesize updateTimer;


-(NPTaskServices*) init
{
    
    self  =  [super init];
    self.cachedTaskIdentifiers = [[NSMutableDictionary alloc] init];
    
    return self;;
}

+(void) setBackgroundProcessingUpdateLoopCallback:(BackgroundProcessingUpdateCallback) updateCallback
{
    backgroundProcessingUpdateCallback = updateCallback;
}

    
-(void) startBackgroundTask:(NSString*) taskId withExpiryCallback:(void (^) ()) callback
{
    UIBackgroundTaskIdentifier nativeTaskId = [[UIApplication sharedApplication] beginBackgroundTaskWithName:taskId expirationHandler:^{
        if(callback != nil)
            callback(); //ending this task will be done from the callback.
    }];
    
    
    if(nativeTaskId == UIBackgroundTaskInvalid)
    {
        NSLog(@"Unable to consider for background processing. Add error case to the api.");
    }
    else
    {
        [self.cachedTaskIdentifiers setObject:@(nativeTaskId) forKey: taskId];
        
        if(self.cachedTaskIdentifiers.count == 1) {
            [self startBackgroundProcessingUpdateLoop];
        }
    }
}

-(void) cancelTask:(NSString*) taskId
{
    if([self.cachedTaskIdentifiers objectForKey:taskId])
    {
        UIBackgroundTaskIdentifier identifier = [[self.cachedTaskIdentifiers objectForKey:taskId] integerValue];
        [[UIApplication sharedApplication] endBackgroundTask:identifier];
        [self.cachedTaskIdentifiers removeObjectForKey:taskId];
        
        if(self.cachedTaskIdentifiers.count == 0) {
            [self stopBackgroundProcessingUpdateLoop];
        }
    }
}

-(void) startBackgroundProcessingUpdateLoop
{
    self.updateTimer = [NSTimer scheduledTimerWithTimeInterval:0.1
                                                        target:self
                                                      selector:@selector(callUpdateCallback)
                                                      userInfo:nil
                                                       repeats:YES];
    
    
    
}

-(void) stopBackgroundProcessingUpdateLoop
{
    [self.updateTimer invalidate];
    self.updateTimer = nil;
}

-(void) callUpdateCallback
{
    dispatch_async(dispatch_get_main_queue(), ^{
            if(!UnityIsPaused())
                return;
        
            backgroundProcessingUpdateCallback();
        });
}

@end
