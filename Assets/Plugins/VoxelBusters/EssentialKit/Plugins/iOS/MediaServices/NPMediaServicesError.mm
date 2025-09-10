//
//  NPMediaContent.mm
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//
#import "NPMediaServicesError.h"

@interface NPMediaServicesError ()

+(NPMediaServicesError*) createFrom:(MediaServicesErrorCode) code withDescription:(NSString*) errorDescription;

@end

@implementation NPMediaServicesError

+(NPMediaServicesError*) createFrom:(NSError*) error
{
    if(error == nil)
        return nil;
    
    MediaServicesErrorCode errorCode;
    
    switch (error.code) {

        default:
            NSLog(@"[Media Services] %ld error code not handled - Inform developer to handle this error code: ", error.code);
            errorCode = MediaServicesErrorCodeUnknown;
            break;
    }
    
    return [NPMediaServicesError createFrom:errorCode
                           withDescription:error.description];
}

+(NPMediaServicesError*) unknown
{
    return [NPMediaServicesError createFrom: MediaServicesErrorCodeUnknown withDescription: @"Unknown error"];
}

+(NPMediaServicesError*) permissionNotAvailable
{
    return [NPMediaServicesError createFrom: MediaServicesErrorCodePermissionNotAvailable withDescription: @"Permission not available"];
}

+(NPMediaServicesError*) userCancelled
{
    return [NPMediaServicesError createFrom: MediaServicesErrorCodeUserCancelled withDescription: @"User cancelled operation"];
}

+ (NPMediaServicesError *)dataNotAvailable {
    return [NPMediaServicesError createFrom: MediaServicesErrorCodeUserCancelled withDescription: @"No data available"];
}



+(NPMediaServicesError*) createFrom:(MediaServicesErrorCode) code withDescription:(NSString*) errorDescription
{
    return [NPMediaServicesError errorWithDomain:Domain
                                           code:(int)code
                                       userInfo:@{NSLocalizedDescriptionKey: errorDescription}];
}

@end
