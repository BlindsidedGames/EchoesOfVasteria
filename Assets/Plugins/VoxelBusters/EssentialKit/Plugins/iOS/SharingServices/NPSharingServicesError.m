//
//  NPSharingServicesError.mm
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//


#import "NPSharingServicesError.h"

@interface NPSharingServicesError ()

+(NPSharingServicesError*) createFrom:(SharingServicesErrorCode) code withDescription:(NSString*) errorDescription;

@end

@implementation NPSharingServicesError

+(NPSharingServicesError*) createFrom:(NSError*) error
{
    if(error == nil)
        return nil;
    
    SharingServicesErrorCode errorCode;
    
    switch (error.code) {

        default:
            NSLog(@"[Sharing Services] %ld error code not handled - Inform developer to handle this error code: ", error.code);
            errorCode = SharingServicesErrorCodeUnknown;
            break;
    }
    
    return [NPSharingServicesError createFrom:errorCode
                           withDescription:error.description];
}

+(NPSharingServicesError*) unknown
{
    return [NPSharingServicesError createFrom: SharingServicesErrorCodeUnknown withDescription: @"Unknown error"];
}

+(NPSharingServicesError*) attachmentNotValid
{
    return [NPSharingServicesError createFrom: SharingServicesErrorCodeAttachmentNotValid withDescription: @"Attachment not valid"];
}

+(NPSharingServicesError*) createFrom:(SharingServicesErrorCode) code withDescription:(NSString*) errorDescription
{
    return [NPSharingServicesError errorWithDomain:Domain
                                           code:(int)code
                                       userInfo:@{NSLocalizedDescriptionKey: errorDescription}];
}

@end
