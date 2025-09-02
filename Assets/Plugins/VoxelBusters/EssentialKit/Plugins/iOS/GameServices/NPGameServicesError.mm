//
//  NPGameServicesError.mm
//  Native Plugins
//
//  Created by Ayyappa Reddy
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.
//


#import "NPGameServicesError.h"
#import <GameKit/GKError.h>

@interface NPGameServicesError ()

+(NPGameServicesError*) createFrom:(GameServicesErrorCode) code withDescription:(NSString*) errorDescription;

@end

@implementation NPGameServicesError

+(NPGameServicesError*) createFrom:(NSError*) error
{
    if(error == nil)
        return nil;
    
    GameServicesErrorCode errorCode;
    
    switch (error.code) {

        case GKErrorInvalidCredentials:
        case GKErrorInvalidParameter:
        case GKErrorInvalidPlayer:
            errorCode = GameServicesErrorCodeInvalidInput;
            break;
            
        case GKErrorNotAuthenticated:
            errorCode = GameServicesErrorCodeNotAuthenticated;
            break;
        
        case GKErrorGameUnrecognized:
        case GKErrorFriendListDescriptionMissing:
            errorCode = GameServicesErrorCodeConfigurationIssue;
            break;
        
        case GKErrorNotSupported:
            errorCode = GameServicesErrorCodeNotSupported;
            break;
            
        case GKErrorPlayerPhotoFailure:
            errorCode = GameServicesErrorCodeDataNotAvailable;
            break;
            
        case GKErrorNotAuthorized:
        case GKErrorFriendListRestricted:
        case GKErrorFriendListDenied:
            errorCode = GameServicesErrorCodeNotAllowed;
            break;
            
        case GKErrorConnectionTimeout:
        case GKErrorCommunicationsFailure:
            errorCode = GameServicesErrorCodeNetworkIssue;
            break;
            
            
        case GKErrorLockdownMode:
            errorCode = GameServicesErrorCodeSystemIssue;
            break;

        default:
            NSLog(@"[Game Services] %ld error code not handled - Inform developer to handle this error code: ", error.code);
            errorCode = GameServicesErrorCodeUnknown;
            break;
    }
    
    return [NPGameServicesError createFrom:errorCode
                           withDescription:error.description];
}

+(NPGameServicesError*) unknown
{
    return [NPGameServicesError createFrom: GameServicesErrorCodeUnknown withDescription: @"Unknown error"];
}

+(NPGameServicesError*) notAuthenticated
{
    return [NPGameServicesError createFrom: GameServicesErrorCodeNotAuthenticated withDescription: @"Player not authenticated"];
}

+(NPGameServicesError*) dataNotAvailable
{
    return [NPGameServicesError createFrom: GameServicesErrorCodeUnknown withDescription: @"Data not available"];
}



+(NPGameServicesError*) createFrom:(GameServicesErrorCode) code withDescription:(NSString*) errorDescription
{
    return [NPGameServicesError errorWithDomain:Domain
                                           code:(int)code
                                       userInfo:@{NSLocalizedDescriptionKey: errorDescription}];
}

+(NPGameServicesError*) notSupported
{
    return [NPGameServicesError createFrom: GameServicesErrorCodeNotSupported withDescription: @"Not supported"];
}

@end
