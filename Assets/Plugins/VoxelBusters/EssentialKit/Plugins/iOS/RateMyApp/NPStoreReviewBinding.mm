//
//  NPStoreReviewBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Updated 12/08/24
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.


#import <StoreKit/StoreKit.h>
#import "NPDefines.h"
#import "NPUnityAppController.h"
#import "NPKit.h"


NPBINDING DONTSTRIP void NPStoreReviewRequestReview(const char* appStoreIdStr)
{
#if !TARGET_OS_TV
    NPUnityAppController    *appController      = (NPUnityAppController*)GetAppController();
    UIWindowScene           *windowScene        = [[appController window] windowScene];
    
    [SKStoreReviewController requestReviewInScene: windowScene];
#else
    NSString* appStoreId = NPCreateNSStringFromCString(appStoreIdStr);
    NSString *url = [NSString stringWithFormat:@"https://apps.apple.com/app/id%@?action=write-review",appStoreId];
    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:url]
                                       options:@{}
                             completionHandler:nil];
#endif
}
