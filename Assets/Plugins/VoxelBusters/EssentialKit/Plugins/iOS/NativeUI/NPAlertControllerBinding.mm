//
//  NPAlertControllerBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "NPKit.h"
#import "NPUIManager.h"

#pragma mark - Data Structs

typedef enum : NSInteger
{
    KeyboardInputTypeDefault,
    KeyboardInputTypeASCIICapable,
    KeyboardInputTypeNumbersAndPunctuation,
    KeyboardInputTypeURL,
    KeyboardInputTypeNumberPad,
    KeyboardInputTypePhonePad,
    KeyboardInputTypeEmailAddress
} KeyboardInputType;


struct TextInputFieldOptionsData
{
    const char*     text;
    const char*     placeholderText;
    const int       isSecured;
    const int       keyboardInputType;
};
typedef struct TextInputFieldOptionsData TextInputFieldOptionsData;


NPBINDING DONTSTRIP UIKeyboardType getNativeKeyboardType(KeyboardInputType inputType)
{
    switch (inputType) {
        case KeyboardInputTypeDefault:
            return UIKeyboardTypeDefault;
            
        case KeyboardInputTypeASCIICapable:
            return UIKeyboardTypeASCIICapable;
            
        case KeyboardInputTypeNumbersAndPunctuation:
            return UIKeyboardTypeNumbersAndPunctuation;
            
        case KeyboardInputTypeURL:
            return UIKeyboardTypeURL;
            
        case KeyboardInputTypeNumberPad:
            return UIKeyboardTypeNumberPad;
            
        case KeyboardInputTypePhonePad:
            return UIKeyboardTypePhonePad;
            
            
        case KeyboardInputTypeEmailAddress:
            return UIKeyboardTypeEmailAddress;
        
        default:
            NSLog(@"[Alert] Didn't implement this input type! Notify developer!");
            return UIKeyboardTypeDefault;
    }
}


#pragma mark - Native binding methods

NPBINDING DONTSTRIP void NPAlertControllerRegisterCallback(NativeAlertActionSelectCallback actionSelectCallback)
{
    [NPUIManager setAlertActionSelectCallback:actionSelectCallback];
}

NPBINDING DONTSTRIP void* NPAlertControllerCreate(const char* title, const char* message, UIAlertControllerStyle preferredStyle)
{
    UIAlertController*  alert       = [UIAlertController alertControllerWithTitle:NPCreateNSStringFromCString(title)
                                                                          message:NPCreateNSStringFromCString(message)
                                                                   preferredStyle:preferredStyle];

    return NPRetainWithOwnershipTransfer(alert);
}

NPBINDING DONTSTRIP void NPAlertControllerShow(void* nativePtr)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    [[NPUIManager sharedManager] showAlertController:alert];
}

NPBINDING DONTSTRIP void NPAlertControllerDismiss(void* nativePtr)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    [[NPUIManager sharedManager] dismissAlertController:alert];
}

NPBINDING DONTSTRIP void NPAlertControllerSetTitle(void* nativePtr, const char* value)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    [alert setTitle:NPCreateNSStringFromCString(value)];
}

NPBINDING DONTSTRIP const char* NPAlertControllerGetTitle(void* nativePtr)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    NSString*           title       = [alert title];
    return NPCreateCStringCopyFromNSString(title);
}

NPBINDING DONTSTRIP void NPAlertControllerSetMessage(void* nativePtr, const char* value)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    [alert setMessage:NPCreateNSStringFromCString(value)];
}

NPBINDING DONTSTRIP const char* NPAlertControllerGetMessage(void* nativePtr)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    NSString*           message     = [alert message];
    return NPCreateCStringCopyFromNSString(message);
}


NPBINDING DONTSTRIP void NPAlertControllerAddTextInputField(void* nativePtr, TextInputFieldOptionsData options)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
        
    [alert addTextFieldWithConfigurationHandler:^(UITextField * _Nonnull textField) {
        textField.text = NPCreateNSStringFromCString(options.text);
        textField.placeholder = NPCreateNSStringFromCString(options.placeholderText);
        textField.secureTextEntry = (options.isSecured == 1);
        textField.keyboardType = getNativeKeyboardType((KeyboardInputType)options.keyboardInputType);
    }];
}


NPBINDING DONTSTRIP void NPAlertControllerAddAction(void* nativePtr, const char* text, bool isCancelType)
{
    UIAlertController*  alert       = (__bridge UIAlertController*)nativePtr;
    
    // create action
    UIAlertAction*      action      = [UIAlertAction actionWithTitle:NPCreateNSStringFromCString(text)
                                                               style:isCancelType ? UIAlertActionStyleCancel : UIAlertActionStyleDefault
                                                             handler:^(UIAlertAction* _Nonnull action) {
                                                                    [[NPUIManager sharedManager] alertController:alert didSelectAction:action withInputFields:alert.textFields];
                                                             }];
    // add action to controller
    [alert addAction:action];
}


