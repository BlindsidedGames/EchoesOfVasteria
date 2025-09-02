//
//  NPGameServicesDataTypes.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <GameKit/GameKit.h>
#import "NPDefines.h"
#import "NPUnityDataTypes.h"

#pragma mark - Enums

typedef enum : NSInteger
{
    GKLocalPlayerAuthStateNotFound,
    GKLocalPlayerAuthStateAuthenticating,
    GKLocalPlayerAuthStateAvailable,
} GKLocalPlayerAuthState;

#pragma mark - Callback definitions

// callback signatures
typedef void (*GameServicesLoadArrayNativeCallback)(NPArray* nativeArray, NPError error, void* tagPtr);

typedef void (*GameServicesLoadScoresNativeCallback)(NPArray* nativeArray, void* scorePtr,NPError error, void* tagPtr);

typedef void (*GameServicesReportNativeCallback)(NPError error, void* tagPtr);

typedef void (*GameServicesAuthStateChangeNativeCallback)(GKLocalPlayerAuthState state, NPError error);

typedef void (*GameServicesLoadImageNativeCallback)(void* dataPtr, int dataLength, NPError error, void* tagPtr);

typedef void (*GameServicesViewClosedNativeCallback)(NPError error, void* tagPtr);

typedef void (*GameServicesLoadServerCredentialsNativeCallback)(const char* publicKeyUrl, void* signaturePtr, int signatureDataLength, void* saltPtr, int saltDataLength, long timestamp, NPError error, void* tagPtr);
