//
//  NPSLComposeViewControllerWrapper.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <Social/Social.h>
#import "NPSocialShareComposerProtocol.h"
#import "UIViewController+Presentation.h"

@interface NPSLComposeViewControllerWrapper : NSObject<NPSocialShareComposerProtocol, NPUIPopoverPresentationControllerDelegate>

// static methods
+ (bool)isServiceTypeAvailable:(NSString*)serviceType;

// init methods
- (id)initWithServiceType:(NSString*)serviceType;

@end
