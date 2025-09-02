//
//  NPWhatsAppShareComposer.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import "NPSocialShareComposerProtocol.h"

#if !TARGET_OS_TV
@protocol NPUIDocumentInteractionControllerDelegate <UIDocumentInteractionControllerDelegate>
@end
#else
@protocol NPUIDocumentInteractionControllerDelegate
@end
#endif

@interface NPWhatsAppShareComposer : NSObject<NPUIDocumentInteractionControllerDelegate, NPSocialShareComposerProtocol>

// static methods
+ (bool)IsServiceAvailable;

@end
