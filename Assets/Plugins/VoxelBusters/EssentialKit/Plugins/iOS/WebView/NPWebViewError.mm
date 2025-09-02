//
//  NPWebViewError.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPWebViewError.h"


@interface NPWebViewError ()

+(NPWebViewError*) createFrom:(WebViewErrorCode) code withDescription:(NSString*) errorDescription;

@end

@implementation NPWebViewError

+(NPWebViewError*) createFrom:(NSError*) error
{
    if(error == nil)
        return nil;
    
    WebViewErrorCode errorCode;
    
    switch (error.code) {

        default:
            NSLog(@"[Web View] %ld error code not handled - Inform developer to handle this error code: ", error.code);
            errorCode = WebViewErrorCodeUnknown;
            break;
    }
    
    return [NPWebViewError createFrom:errorCode
                           withDescription:error.description];
}

+(NPWebViewError*) unknown
{
    return [NPWebViewError createFrom: WebViewErrorCodeUnknown withDescription: @"Unknown error"];
}


+(NPWebViewError*) createFrom:(WebViewErrorCode) code withDescription:(NSString*) errorDescription
{
    return [NPWebViewError errorWithDomain:Domain
                                      code:(int)code
                                  userInfo:@{NSLocalizedDescriptionKey: errorDescription}];
}

@end
