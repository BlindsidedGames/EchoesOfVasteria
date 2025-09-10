//
//  NPWebViewTabBar.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#define Domain @"Web View"

typedef enum : NSInteger
{
    WebViewErrorCodeUnknown,
} WebViewErrorCode;


@interface NPWebViewError : NSError

+(NPWebViewError*) createFrom:(NSError*) error;
+(NPWebViewError*) unknown;

@end
