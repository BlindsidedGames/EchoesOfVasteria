//
//  NPAppUpdater.mm
//  Essential Kit
//
//  Created by Ayyappa on 09/09/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPKit.h"
#import "NPAppUpdater.h"
#import "UnityInterface.h"
#import "UIViewController+Presentation.h"
#import "NSError+Utility.h"

@implementation AppUpdateStatusInfo
@end

@implementation PromptUpdateOptions
@end

@interface NPAppUpdater ()

@property(nonatomic, strong) AppUpdateStatusInfo* cachedStatusInfo;

@end

@implementation NPAppUpdater

@synthesize appId;
@synthesize cachedStatusInfo;

-(NPAppUpdater*) initWithAppId:(NSString*) appId
{
    
    self  =  [super init];
    self.appId = appId;
    
    return self;
}

-(void) requestUpdateInfo:(void (^) (AppUpdateStatusInfo* info, NSError* error)) callback
{
    //Do a request to itunes connect and fire the callback with status accordingly.
    // Need to check the os version is also compatible with the update
    NSString* lookupUrlString = [NSString stringWithFormat:@"http://itunes.apple.com/lookup?id=%@", self.appId];
    NSURL* url = [NSURL URLWithString:lookupUrlString];
    NSString* currentAppVersion = [[NSBundle mainBundle] objectForInfoDictionaryKey: @"CFBundleShortVersionString"];
    NSString* currentSystemVersion = UIDevice.currentDevice.systemVersion;
    
    
    dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_BACKGROUND, 0), ^{
        NSError *error = nil;
        NSData* urlData = [NSData dataWithContentsOfURL:url options:NSDataReadingUncached error:&error];
        NSDictionary* json = (error != nil || urlData == nil) ? nil : [NSJSONSerialization JSONObjectWithData:urlData options:0 error:nil];
        NSArray* results = (json == nil) ? nil : json[@"results"];
        
        if(json != nil && [results count] > 0)
        {
            NSDictionary* result = [results objectAtIndex:0];
            NSString* newVersion = result[@"version"];
            NSString* minimumOsVersionRequired = result[@"minimumOsVersion"];
            
            AppUpdateStatusInfo *info = [[AppUpdateStatusInfo alloc] init];
            
            if([currentSystemVersion compare:minimumOsVersionRequired options:NSNumericSearch] == NSOrderedAscending)
            {
                NSError *resolvedError = [NSError createWithDomain:Domain withCode:AppUpdaterErrorCodeUpdateNotCompatible withDescription:@"Device needs an update to download latest version."];
                callback(nil, resolvedError);
            }
            else if([currentAppVersion compare:newVersion options:NSNumericSearch] == NSOrderedAscending)
            {
                info.status = AppUpdateStatusTypeAvailable;
                callback(info, nil);
            }
            else
            {
                info.status = AppUpdateStatusTypeNotAvailable;
                callback(info, nil);
            }
            
            self.cachedStatusInfo = info;
        }
        else
        {
            NSLog(@"Error when fetching url %@ : %@", url, error);
            
            NSError *resolvedError = [NSError createWithDomain:Domain
                                                      withCode:error != nil ? AppUpdaterErrorCodeNetworkIssue : AppUpdaterErrorCodeUnknown
                                               withDescription:error != nil ? [error localizedDescription] : @"Unknown error"];

            callback(nil, resolvedError);
        }
    });
}



-(void) promptUpdate:(PromptUpdateOptions*) options withCallback:(void (^) (float progress, NSError* error)) callback
{
    NSString* appId = self.appId;
    
    if(self.cachedStatusInfo == nil)
    {
        [self requestUpdateInfo:^(AppUpdateStatusInfo *info, NSError *error) {
            [self promptUpdateInternal:info withAppId: appId callback:callback options:options];
        }];
    }
    else
    {
        [self promptUpdateInternal:self.cachedStatusInfo withAppId:appId callback:callback options:options];
    }
}

- (void)promptUpdateInternal:(AppUpdateStatusInfo*) statusInfo withAppId:(NSString *)appId callback:(void (^)(float, NSError *))callback options:(PromptUpdateOptions *)options {
    
    if(statusInfo.status != AppUpdateStatusTypeAvailable)
    {
        NSError *resolvedError = [NSError createWithDomain:Domain withCode:AppUpdaterErrorCodeUpdateNotAvailable withDescription:@"No update available."];
        if(callback != nil)
        {
            callback(0.0f, resolvedError);
        }
        return;
    }
    
    dispatch_async(dispatch_get_main_queue(), ^{
        
        UIAlertController* alert = [UIAlertController alertControllerWithTitle:options.title
                                                                       message:options.message
                                                                preferredStyle:UIAlertControllerStyleAlert];
        
        UIAlertAction* updateAction = [UIAlertAction actionWithTitle:@"Update" style:UIAlertActionStyleDefault
                                                             handler:^(UIAlertAction * action) {
            
            
            NSURL* url = [NSURL URLWithString:[NSString stringWithFormat:@"itms-apps://itunes.apple.com/app/id%@", appId]];
            
            [UIApplication.sharedApplication openURL:url options:@{} completionHandler:nil];
            
            if(callback != nil)
            {
                callback(0.0f, nil);
            }
            
            if(options.isForceUpdate) //Enforce the alert again.
            {
                [self promptUpdate:options withCallback:callback];
            }
        }];
        
        [alert addAction:updateAction];
        
        if(!options.isForceUpdate)
        {
            UIAlertAction* cancelAction = [UIAlertAction actionWithTitle:@"Cancel" style:UIAlertActionStyleCancel
                                                                 handler:^(UIAlertAction * action) {
                if(callback != nil)
                {
                    NSError *resolvedError = [NSError createWithDomain:Domain withCode:AppUpdaterErrorCodeUpdateCancelled withDescription:@"Update cancelled."];
                    callback(0.0f, resolvedError);
                }
            }];
            [alert addAction:cancelAction];
        }
        
        [UnityGetGLViewController() presentViewController:alert animated:YES completion:nil];
    });
}

@end
