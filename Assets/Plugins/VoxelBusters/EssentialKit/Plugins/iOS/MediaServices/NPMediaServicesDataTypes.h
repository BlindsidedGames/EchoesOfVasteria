//
//  NPMediaServicesDataTypes.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Photos/Photos.h>
#import "NPKit.h"

// enums
typedef enum : NSInteger
{
    PickImageFinishReasonCancelled,
    PickImageFinishReasonFailed,
    PickImageFinishReasonSuccess,
} PickImageFinishReason;

struct MediaContentSelectionOptionsData
{
    const char*     title;
    const char*     allowedMimeType;
    int             maxAllowed;
    bool            showPermissionDialog;
};
typedef struct MediaContentSelectionOptionsData MediaContentSelectionOptionsData;

struct MediaContentCaptureOptionsData
{
    int captureType;
    const char* title;
    const char* fileName;
};
typedef struct MediaContentCaptureOptionsData MediaContentCaptureOptionsData;

struct MediaContentSaveOptionsData
{
    const char* directoryName;
    const char* fileName;
};
typedef struct MediaContentSaveOptionsData MediaContentSaveOptionsData;



// callback signatures
typedef void (*RequestPhotoLibraryAccessNativeCallback)(PHAuthorizationStatus status, const char* error, void* tagPtr);
typedef void (*RequestCameraAccessNativeCallback)(AVAuthorizationStatus status, const char* error, void* tagPtr);
typedef void (*PickImageNativeCallback)(void* imageDataPtr, PickImageFinishReason reasonCode, void* tagPtr);
typedef void (*SaveImageToAlbumNativeCallback)(bool success, const char* error, void* tagPtr);

typedef void (*SelectMediaContentNativeCallback)(NPArray*  mediaContents, NPError error, void* tagPtr);
typedef void (*CaptureMediaContentNativeCallback)(void*  mediaContentPtr, NPError error, void* tagPtr);
typedef void (*SaveMediaContentNativeCallback)(bool  success, NPError error, void* tagPtr);


typedef void (*MediaContentAsRawDataNativeCallback)(void* mediaContentDataPtr, int dataLength, const char* mime, NPError error, void* tagPtr);
typedef void (*MediaContentAsFilePathNativeCallback)(const char* pathPtr, NPError error, void* tagPtr);



