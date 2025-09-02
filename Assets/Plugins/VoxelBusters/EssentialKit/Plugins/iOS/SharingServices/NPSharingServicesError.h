//
//  NPSharingServicesError.h
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//
#define Domain @"Notification Services"

typedef enum : NSInteger
{
    SharingServicesErrorCodeUnknown,
    SharingServicesErrorCodeAttachmentNotValid
} SharingServicesErrorCode;


@interface NPSharingServicesError : NSError

+(NPSharingServicesError*) createFrom:(NSError*) error;
+(NPSharingServicesError*) unknown;
+(NPSharingServicesError*) attachmentNotValid;

@end

