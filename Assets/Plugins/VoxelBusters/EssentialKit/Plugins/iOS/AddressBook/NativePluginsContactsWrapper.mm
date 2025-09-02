//
//  NativePluginsContactsWrapper.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#if !TARGET_OS_TV
#import "NativePluginsContactsWrapper.h"
#import "NSError+Utility.h"

// static fields
static CNContactStore*                      _contactStore                   = nil;


@implementation ReadContactsOptions
@end

@implementation NativePluginsContactsWrapper

+ (CNContactStore*)getContactStore
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        _contactStore = [[CNContactStore alloc] init];
    });
    return _contactStore;
}

+ (CNAuthorizationStatus)getAuthorizationStatus
{
    return [CNContactStore authorizationStatusForEntityType:CNEntityTypeContacts];
}

+ (void)requestContactsAccess:(void (^) (CNAuthorizationStatus status, NSError* error)) callback
{
    [[NativePluginsContactsWrapper getContactStore] requestAccessForEntityType:CNEntityTypeContacts
                                                             completionHandler:^(BOOL granted, NSError* __nullable error) {
                                                                 // send callback
                                                                 CNAuthorizationStatus  newStatus   = [NativePluginsContactsWrapper getAuthorizationStatus];
        
        
                                                                    NSError* resolvedError = error == nil ? nil : [NSError createWithDomain:Domain
                                                                                                                                   withCode:AddressBookErrorCodeUnknown
                                                                                                                            withDescription: error.localizedDescription];
        
                                                                 callback(newStatus, resolvedError);
                                                             }];
}

+ (BOOL)isAuthorized:(CNAuthorizationStatus) status
{
    if (@available(iOS 18.0, *)) {
        return (status == CNAuthorizationStatusAuthorized || status == CNAuthorizationStatusLimited);
    } else {
        return status == CNAuthorizationStatusAuthorized;
    }
}

+ (void)readContacts:(ReadContactsOptions*) options withCallback:(void (^)(NSArray<CNContact*> *contacts, NSUInteger nextOffset, NSError* error)) callback
{
    [NativePluginsContactsWrapper requestContactsAccess:^(CNAuthorizationStatus status, NSError *error) {
        if([NativePluginsContactsWrapper isAuthorized: status]) {
            
            dispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_DEFAULT, 0), ^{
                [NativePluginsContactsWrapper fetchContactsInternal: options withCallback:^(NSArray<CNContact *> *contacts, NSUInteger nextOffset, NSError *error) {
                    [NativePluginsContactsWrapper hasMoreContacts:nextOffset withConstraints: options.constraints withCallback:^(BOOL hasMore) {
                        callback(contacts, hasMore ? nextOffset : -1, error);
                    }];
                }];
            });
        }
        else {
            NSLog(@"Read contacts failed due to permission status %ld", status);
            
            NSError *resolvedError = error;
            
            if(resolvedError == nil) {
                
                resolvedError = [NSError createWithDomain: Domain
                                                 withCode: status == CNAuthorizationStatusDenied ? AddressBookErrorCodePermissionDenied : AddressBookErrorCodeUnknown
                                          withDescription: @"Failed due to permission"];
            }

            callback(nil, -1, resolvedError);
        }
    }];
    
}

+ (void)fetchContactsInternal:(ReadContactsOptions*) options withCallback:(void (^)(NSArray<CNContact*> *contacts, NSUInteger nextOffset, NSError* error)) callback
{
    CNContactStore*         contactStore        = [NativePluginsContactsWrapper getContactStore];
    NSMutableArray*         contacts            = [NSMutableArray array];
    bool                    finished            = false;
    NSError*                error               = nil;
    CNContactFetchRequest*  fetchRequest        = [[CNContactFetchRequest alloc] initWithKeysToFetch:@[CNContactIdentifierKey]];
    [fetchRequest setUnifyResults:YES];
    [fetchRequest setSortOrder:CNContactSortOrderGivenName];
    
    __block NSUInteger resultCount      = 0;
    __block NSUInteger nextOffset       = 0;
    
    do
    {
        finished    = [contactStore enumerateContactsWithFetchRequest:fetchRequest
                                                                error:&error
                                                           usingBlock:^(CNContact* _Nonnull contact, BOOL * _Nonnull stop) {
            
            nextOffset++;

            //Skip until we are at next offset
            if(nextOffset <= options.offset)
            {
                return;
            }
            
            NSArray *keysToFetch = @[CNContactGivenNameKey,
                                     CNContactMiddleNameKey,
                                     CNContactFamilyNameKey,
                                     CNContactImageDataKey,
                                     CNContactPhoneNumbersKey,
                                     CNContactEmailAddressesKey,
                                     CNContactOrganizationNameKey];
            
            NSError *detailedFetchError = nil;
            CNContact *detailedContact = [contactStore unifiedContactWithIdentifier:contact.identifier keysToFetch:keysToFetch error:&detailedFetchError];
            
            if(detailedContact && [self hasRequiredKeys:detailedContact forConstraints:options.constraints])
            {
                [contacts addObject:detailedContact];
                resultCount++;
            }
            
            if(resultCount >= options.limit)
            {
                *stop = TRUE;
            }
        }];
        
    } while (!finished);
        
    if(resultCount < options.limit)
        nextOffset = -1;
    
    NSError* resolvedError = error == nil ? nil : [NSError createWithDomain:Domain
                                                                   withCode:AddressBookErrorCodeUnknown
                                                            withDescription: error.localizedDescription];
    
    callback(contacts, nextOffset, resolvedError);
}

+(BOOL) hasRequiredKeys:(CNContact*) contact forConstraints:(ReadContactsConstraint) constraints
{
    if(constraints == ReadContactsConstraintNone)
        return TRUE;
    
    if((constraints & ReadContactsConstraintMustIncludeName) != 0)
    {
        NSString *givenName     = [contact givenName];
        NSString *middleName    = [contact middleName];
        NSString *familyName    = [contact familyName];
        
        if([givenName length] == 0 && [middleName length] == 0 && [familyName length] == 0)
            return FALSE;
    }
    
    if((constraints & ReadContactsConstraintMustIncludePhoneNumber) != 0)
    {
        if([contact.phoneNumbers count] == 0)
            return FALSE;
    }
    
    if((constraints & ReadContactsConstraintMustIncludeEmail) != 0)
    {
        if([contact.emailAddresses count] == 0)
            return FALSE;
    }
    
    return TRUE;
}

+(void) hasMoreContacts:(NSUInteger) offset withConstraints:(ReadContactsConstraint) constraints withCallback:(void (^)(BOOL hasMore)) callback
{
    if(offset == -1)
    {
        callback(FALSE);
        return;
    }
    
    ReadContactsOptions *options = [[ReadContactsOptions alloc] init];
    options.limit = 1;
    options.constraints = constraints;
    options.offset = (int)offset;
    
    [NativePluginsContactsWrapper fetchContactsInternal: options  withCallback:^(NSArray<CNContact *> *contacts, NSUInteger nextOffset, NSError *error) {
        callback(nextOffset == -1 ? FALSE : TRUE);
    }];
}

@end
#endif
