//
//  NPSocialShareComposerProtocol.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#if !TARGET_OS_TV
#import <Foundation/Foundation.h>
#import <MessageUI/MessageUI.h>
#import <Social/Social.h>

@protocol NPSocialShareComposerProtocol <NSObject>

// adding items
- (BOOL)addText:(NSString*)text;
- (BOOL)addImage:(UIImage*)image;
- (BOOL)addURL:(NSURL*)url;

// setter methods
- (void)setCompletionHandler:(SLComposeViewControllerCompletionHandler)completionHandler;

// presentation methods
- (void)showAtPosition:(CGPoint)position;

@end
#else
@protocol NPSocialShareComposerProtocol
@end
#endif
