//
//  NPMailComposerDelegate.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPSharingServicesDataTypes.h"

#if !TARGET_OS_TV
#import <MessageUI/MessageUI.h>
@interface NPSharingServicesManager : NSObject<MFMailComposeViewControllerDelegate, MFMessageComposeViewControllerDelegate>
#else
@interface NPSharingServicesManager : NSObject
#endif

+ (NPSharingServicesManager*)sharedManager;
+ (void)setMailComposerClosedCallback:(MailComposerClosedNativeCallback)callback;
+ (void)setMessageComposerClosedCallback:(MessageComposerClosedNativeCallback)callback;
+ (id<NPSocialShareComposerProtocol>)createShareComposer:(NPSocialShareComposerType)composerType;
+ (bool)isSocialShareComposerAvailable:(NPSocialShareComposerType)composerType;

@end
