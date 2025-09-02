//
//  NPShareSheetWrapper.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPSharingServicesDataTypes.h"
#import <UIKit/UIKit.h>
#import "UIViewController+Presentation.h"

@interface NPShareSheetWrapper : NSObject<NPUIPopoverPresentationControllerDelegate>

// callbacks
+ (void)setShareSheetClosedCallback:(ShareSheetClosedNativeCallback)callback;

- (void)addItem:(NSObject*)item;
- (void)showAtPosition:(CGPoint)position withAnimation:(BOOL)animated;

@end
