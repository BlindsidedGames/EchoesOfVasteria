//
//  NPMediaServices.h
//  Native Plugins
//
//  Created by Ayyappa on 26/06/24.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPMediaContent.h"
#import <PhotosUI/PhotosUI.h>
#import <UIKit/UIKit.h>
#import "NPMediaServicesError.h"

typedef enum : NSInteger
{
    MediaContentCaptureTypeImage,
    MediaContentCaptureTypeVideo
} MediaContentCaptureType;

typedef void (^SelectMediaContentCallback) (NSArray<NPMediaContent*> *contents, NPMediaServicesError* error);
typedef void (^CaptureMediaContentCallback)(NPMediaContent *content, NPMediaServicesError* error);
typedef void (^SaveMediaContentCallback)(BOOL success, NPMediaServicesError* error);

@interface MediaContentSelectionOptions : NSObject

@property (strong, nonatomic) NSString *title;
@property (strong, nonatomic) NSString *allowedMimeType;
@property (nonatomic) int  maxAllowed;
@property (nonatomic) BOOL  showPermissionDialog;

@end



@interface MediaContentCaptureOptions : NSObject

@property (nonatomic) MediaContentCaptureType captureType;
@property (strong, nonatomic) NSString *title;
@property (nonatomic) NSString*  fileName;

@end


@interface MediaContentSaveOptions : NSObject

@property (strong, nonatomic) NSString *directoryName;
@property (strong, nonatomic) NSString *fileName;

@end


@interface NPMediaServices : NSObject<PHPickerViewControllerDelegate, UIDocumentPickerDelegate, UINavigationControllerDelegate, UIImagePickerControllerDelegate, UIAdaptivePresentationControllerDelegate>

+ (NPMediaServices*)sharedInstance;
-(void) selectMediaContent:(MediaContentSelectionOptions*) options withCallback:(SelectMediaContentCallback) callback;
-(void) captureMediaContent:(MediaContentCaptureOptions*) options withCallback:(CaptureMediaContentCallback) callback;
-(void) saveMediaContent:(NSData*) data withMimeType:(NSString*) mime withOptions:(MediaContentSaveOptions*) options withCallback:(SaveMediaContentCallback) callback;
@end


