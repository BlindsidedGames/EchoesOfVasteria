//
//  NativePluginsContactsWrapper.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#define Domain @"Address Book"

typedef enum : NSInteger
{
    ReadContactsConstraintNone = 0,
    ReadContactsConstraintMustIncludeName = 1,
    ReadContactsConstraintMustIncludePhoneNumber = 2,
    ReadContactsConstraintMustIncludeEmail = 4
} ReadContactsConstraint;


typedef enum : NSInteger
{
    AddressBookErrorCodeUnknown = 0,
    AddressBookErrorCodePermissionDenied = 1
} AddressBookErrorCode;


#if !TARGET_OS_TV
#import <Contacts/Contacts.h>

@interface ReadContactsOptions : NSObject

@property (nonatomic) int limit;
@property (nonatomic) int offset;
@property (nonatomic) ReadContactsConstraint constraints;

@end


@interface NativePluginsContactsWrapper : NSObject

+ (CNAuthorizationStatus)getAuthorizationStatus;
+ (void)readContacts:(ReadContactsOptions*) options withCallback:(void (^)(NSArray<CNContact*> *contacts, NSUInteger nextOffset, NSError* error)) callback;

@end
#endif
