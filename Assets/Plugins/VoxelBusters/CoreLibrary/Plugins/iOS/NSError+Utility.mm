//
//  UIViewController+Presentation.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NSError+Utility.h"

@implementation NSError (Utility)

+ (NSError*)createWithDomain:(NSString*) domain
                withCode:(int) code
         withDescription:(NSString*) description
{
    NSError* error = [NSError errorWithDomain:domain
                                         code:code
                                     userInfo: @{NSLocalizedDescriptionKey: description}];
    return error;
}


+ (id)createFromError:(NSError*) error
                 withDomain:(NSString*) domain
                   withCode:(int) code
{
    if(error == nil)
        return nil;
    
    return [self errorWithDomain:domain
                               code:code
                           userInfo: @{NSLocalizedDescriptionKey: error.description}];
}

@end
