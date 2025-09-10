//
//  NPAddressBookDataTypes.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Contacts/Contacts.h>
#import "NPKit.h"

// callback signatures
typedef void (*ReadContactsNativeCallback)(void* contactsPtr, int count, int nextOffset, NPError error, void* tagPtr);

// custom datatypes
struct NPUnityAddressBookContact
{
    // data members
    void*           nativeObjectPtr;
    void*           firstNamePtr;
    void*           middleNamePtr;
    void*           lastNamePtr;
    void*           imageDataPtr;
    int             phoneNumberCount;
    void*           phoneNumbersPtr;
    int             emailAddressCount;
    void*           emailAddressesPtr;
    void*           companyNamePtr;
    
    // methods
    void CopyProperties(CNContact* contact);
    
    // destructors
    ~NPUnityAddressBookContact();
};
typedef NPUnityAddressBookContact NativeAddressBookContactData;



typedef enum : NSInteger
{
    NPReadContactsConstraintNone = 0,
    NPReadContactsConstraintMustIncludeName = 1,
    NPReadContactsConstraintMustIncludePhoneNumber = 2,
    NPReadContactsConstraintMustIncludeEmail = 4
} NPReadContactsConstraint;


struct NPUnityReadContactsOptionsData
{
    int limit;
    int offset;
    NPReadContactsConstraint constraints;
};
typedef NPUnityReadContactsOptionsData NPUnityReadContactsOptionsData;

