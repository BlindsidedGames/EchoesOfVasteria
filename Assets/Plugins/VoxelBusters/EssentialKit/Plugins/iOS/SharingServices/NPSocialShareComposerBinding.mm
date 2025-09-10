//
//  NPSocialShareComposerBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPKit.h"
#import "NPSharingServicesDataTypes.h"

#if !TARGET_OS_TV
#import <Foundation/Foundation.h>
#import <Social/Social.h>
#import "NPSharingServicesManager.h"
#import "NPWhatsAppShareComposer.h"
#import "NPSLComposeViewControllerWrapper.h"

#pragma mark - Callback definitions

// static properties
SocialShareComposerClosedNativeCallback _shareComposerClosedCallback;

#pragma mark - Native binding methods

NPBINDING DONTSTRIP bool NPSocialShareComposerIsComposerAvailable(NPSocialShareComposerType composerType)
{
    return [NPSharingServicesManager isSocialShareComposerAvailable:composerType];
}

NPBINDING DONTSTRIP void NPSocialShareComposerRegisterCallback(SocialShareComposerClosedNativeCallback closedCallback)
{
    // save references
    _shareComposerClosedCallback    = closedCallback;
}

NPBINDING DONTSTRIP void* NPSocialShareComposerCreate(NPSocialShareComposerType composerType)
{
    id<NPSocialShareComposerProtocol>   composer    = [NPSharingServicesManager createShareComposer:composerType];
    void*                       nativePtr   = NPRetainWithOwnershipTransfer(composer);
    [composer setCompletionHandler:^(SLComposeViewControllerResult result) {
        NSLog(@"[NativePlugins] Dismissing share composer. Result: %ld", (long)result);
        _shareComposerClosedCallback(nativePtr, result);
    }];
    return nativePtr;
}

NPBINDING DONTSTRIP void NPSocialShareComposerShow(void* nativePtr, float posX, float posY)
{
    NSLog(@"[NativePlugins] Showing share composer.");

    id<NPSocialShareComposerProtocol>   composer    = (__bridge id<NPSocialShareComposerProtocol>)nativePtr;
    [composer showAtPosition:CGPointMake(posX, posY)];
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddText(void* nativePtr, const char* value)
{
    id<NPSocialShareComposerProtocol>   composer    = (__bridge id<NPSocialShareComposerProtocol>)nativePtr;
    [composer addText:NPCreateNSStringFromCString(value)];
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddScreenshot(void* nativePtr)
{
    id<NPSocialShareComposerProtocol>   composer    = (__bridge id<NPSocialShareComposerProtocol>)nativePtr;
    [composer addImage:NPCaptureScreenshotAsImage()];
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddImage(void* nativePtr, void* dataArrayPtr, int dataArrayLength)
{
    id<NPSocialShareComposerProtocol>   composer    = (__bridge id<NPSocialShareComposerProtocol>)nativePtr;
    [composer addImage:NPCreateImage(dataArrayPtr, dataArrayLength)];
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddURL(void* nativePtr, const char* value)
{
    id<NPSocialShareComposerProtocol>   composer    = (__bridge id<NPSocialShareComposerProtocol>)nativePtr;
    [composer addURL:NPCreateNSURLFromCString(value)];
}
#else

#pragma mark - Native binding methods

NPBINDING DONTSTRIP bool NPSocialShareComposerIsComposerAvailable(NPSocialShareComposerType composerType)
{
    return false;
}

NPBINDING DONTSTRIP void NPSocialShareComposerRegisterCallback(SocialShareComposerClosedNativeCallback closedCallback)
{
    
}

NPBINDING DONTSTRIP void* NPSocialShareComposerCreate(NPSocialShareComposerType composerType)
{
    return NULL;
}

NPBINDING DONTSTRIP void NPSocialShareComposerShow(void* nativePtr, float posX, float posY)
{
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddText(void* nativePtr, const char* value)
{
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddScreenshot(void* nativePtr)
{
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddImage(void* nativePtr, void* dataArrayPtr, int dataArrayLength)
{
}

NPBINDING DONTSTRIP void NPSocialShareComposerAddURL(void* nativePtr, const char* value)
{
}

#endif
