//
//  NPAddressBookBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "NPAddressBookDataTypes.h"
#import "NativePluginsContactsWrapper.h"
#import "NPKit.h"

#pragma mark - Native binding methods

NPBINDING DONTSTRIP int NPAddressBookGetAuthorizationStatus()
{
#if !TARGET_OS_TV
    return (int)[NativePluginsContactsWrapper getAuthorizationStatus];
#else
    return 0;
#endif
}

NPBINDING DONTSTRIP void NPAddressBookReadContacts(NPUnityReadContactsOptionsData optionsData, NPIntPtr tagPtr, ReadContactsNativeCallback callback)
{
#if !TARGET_OS_TV
    ReadContactsOptions *options = [[ReadContactsOptions alloc] init];
    options.limit       = optionsData.limit;
    options.offset      = optionsData.offset;
    options.constraints = (ReadContactsConstraint)optionsData.constraints;
    
    [NativePluginsContactsWrapper readContacts:options withCallback:^(NSArray<CNContact *> *contacts, NSUInteger nextOffset, NSError *error) {
        
        if (error)
        {
            // send error info
            callback(nil, 0, -1, NPCreateError(error), tagPtr);
            return;
        }
        else
        {
            // transform data to unity format
            int                             totalContacts   = (int)[contacts count];
            NativeAddressBookContactData    contactsArray[totalContacts];
            for (int iter = 0; iter < totalContacts; iter++)
            {
                CNContact*                  currentContact  = [contacts objectAtIndex:iter];
                
                // copy info
                NativeAddressBookContactData*   nativeData  = &contactsArray[iter];
                nativeData->CopyProperties(currentContact);
            }
            
            // send data using callback
            callback(contactsArray, totalContacts, (int)nextOffset, NPCreateError(nil), tagPtr);
        }
    }];
#else
    callback(nil, 0, -1, NPCreateError(AddressBookErrorCodeUnknown, @"Contacts are not supported on tvOS platform"), tagPtr);
#endif
}

NPBINDING DONTSTRIP void NPAddressBookReset()
{
    NSLog(@"Not implemented.");
}
