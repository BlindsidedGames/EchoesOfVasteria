//
//  NPUIManager.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPKit.h"
#import "UIViewController+Presentation.h"

typedef enum : NSInteger
{
    NPUIDatePickerModeTime,
    NPUIDatePickerModeDate,
    NPUIDatePickerModeDateAndTime,
    NPUIDatePickerModeCountDownTimer,
    NPUIDatePickerModeYearAndMonth
} NPUIDatePickerMode;


// signature
typedef void (*NativeAlertActionSelectCallback)(void* nativePtr, int selectedActionIndex, NPArray* inputTextValues);
typedef void (*NativeDatePickerControllerCallback)(long selectedValue, void* tagPtr);

@interface NPUIManager : NSObject<NPUIPopoverPresentationControllerDelegate>

+ (NPUIManager*)sharedManager;
+ (void)setAlertActionSelectCallback:(NativeAlertActionSelectCallback)callback;
+ (void)setDatePickerControllerCallback:(NativeDatePickerControllerCallback)callback;

// presentation methods
- (void)showAlertController:(UIAlertController*)alertController;
- (void)dismissAlertController:(UIAlertController*)alertController;
- (void) showDatePicker:(NPUIDatePickerMode) mode withInitialDate:(long) initialDateTime withMinimumDate:(long) min withMaximumDate:(long) max tagPtr:(void*)tagPtr;

// callback methods
- (void)alertController:(UIAlertController*)alertController didSelectAction:(UIAlertAction*)action withInputFields:(NSArray<UITextField*>*)textfields;

@end
