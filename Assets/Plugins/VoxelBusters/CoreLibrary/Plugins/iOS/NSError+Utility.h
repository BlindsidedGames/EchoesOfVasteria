//
//  UIViewController+Presentation.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>


#define ErrorWithDomain(domain, code, description) [NSError createWithDomain: domain withCode: code withDescription: description]
#define CreateFromError(error, domain, code) [NSError createFromError: error withDomain: domain withCode: code]

@interface NSError (Utility)

+ (NSError*)createWithDomain:(NSString*) domain
                withCode:(int) code
         withDescription:(NSString*) description;

+ (NSError*)createFromError:(NSError*) error
                 withDomain:(NSString*) domain
                   withCode:(int) code;

@end
