//
//  NPSharingServicesDataTypes.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "NPSocialShareComposerProtocol.h"
#import "NPUnityDataTypes.h"

#if !TARGET_OS_TV
#import <MessageUI/MessageUI.h>
#import <Social/Social.h>
#endif


// enum for determining recipient type
typedef enum : NSInteger
{
    NPMailRecipientTypeTo,
    NPMailRecipientTypeCc,
    NPMailRecipientTypeBcc,
} NPMailRecipientType;

typedef enum : int
{
    NPSocialShareComposerTypeFacebook,
    NPSocialShareComposerTypeTwitter,
    NPSocialShareComposerTypeWhatsApp,
} NPSocialShareComposerType;



// callback signatures
#if !TARGET_OS_TV
typedef void (*MailComposerClosedNativeCallback)(void* nativePtr, MFMailComposeResult result, NPError error);
#else
typedef void (*MailComposerClosedNativeCallback)(void* nativePtr, long result, NPError error);
#endif

#if !TARGET_OS_TV
typedef void (*MessageComposerClosedNativeCallback)(void* nativePtr, MessageComposeResult result);
#else
typedef void (*MessageComposerClosedNativeCallback)(void* nativePtr, long result);
#endif

typedef void (*ShareSheetClosedNativeCallback)(void* nativePtr, bool completed, NPError error);

#if !TARGET_OS_TV
typedef void (*SocialShareComposerClosedNativeCallback)(void* nativePtr, SLComposeViewControllerResult result);
#else
typedef void (*SocialShareComposerClosedNativeCallback)(void* nativePtr, long result);
#endif
