//
//  NPGameKitPlayerBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <GameKit/GameKit.h>
#import "NPGameKitManager.h"
#import "NPGameServicesDataTypes.h"
#import "NPKit.h"
#import "NPGameServicesError.h"

// static fields
static GameServicesLoadArrayNativeCallback          _loadPlayersCallback                    = nil;
static GameServicesLoadImageNativeCallback          _loadImageCallback                      = nil;
static GameServicesViewClosedNativeCallback         _viewClosedCallback                     = nil;

#pragma mark - Native binding calls

NPBINDING DONTSTRIP void NPPlayerRegisterCallbacks(GameServicesLoadArrayNativeCallback loadPlayersCallback, GameServicesLoadImageNativeCallback loadPlayerImageCallback, GameServicesViewClosedNativeCallback viewClosedCallback)
{
    // save reference to callback
    _loadPlayersCallback    = loadPlayersCallback;
    _loadImageCallback      = loadPlayerImageCallback;
    _viewClosedCallback     = viewClosedCallback;
}

NPBINDING DONTSTRIP void NPPlayerLoadPlayers(const char** playerIds, int count, void* tagPtr)
{
    [GKPlayer loadPlayersForIdentifiers:NPCreateArrayOfNSString(playerIds, count) withCompletionHandler:^(NSArray<GKPlayer*>* _Nullable players, NSError* _Nullable error) {
        // send data
        NPArray*    nativeArray     = NPCreateNativeArrayFromNSArray(players);
        _loadPlayersCallback(nativeArray, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        
        // release properties
        delete(nativeArray);
    }];
}

NPBINDING DONTSTRIP const char* NPPlayerGetLegacyId(void* playerPtr)
{
    GKPlayer*   player      = (__bridge GKPlayer*)playerPtr;
    return NPCreateCStringCopyFromNSString(player.playerID); //Using for legacy id for backward compatibility
}

NPBINDING DONTSTRIP const char* NPPlayerGetId(void* playerPtr)
{
    GKPlayer*   player      = (__bridge GKPlayer*)playerPtr;
    if (@available(iOS 12.4, *)) {
        return NPCreateCStringCopyFromNSString(player.gamePlayerID);
    } else {
        // Fallback on earlier versions
        return NPPlayerGetLegacyId(playerPtr);
    }
}

NPBINDING DONTSTRIP const char* NPPlayerGetDeveloperScopeId(void* playerPtr)
{
    GKPlayer*   player      = (__bridge GKPlayer*)playerPtr;
    if (@available(iOS 12.4, *)) {
        return NPCreateCStringCopyFromNSString(player.teamPlayerID);
    } else {
        // Fallback on earlier versions
        return NULL;
    }
}

NPBINDING DONTSTRIP const char* NPPlayerGetAlias(void* playerPtr)
{
    GKPlayer*   player      = (__bridge GKPlayer*)playerPtr;
    return NPCreateCStringCopyFromNSString(player.alias);
}

NPBINDING DONTSTRIP const char* NPPlayerGetDisplayName(void* playerPtr)
{
    GKPlayer*   player      = (__bridge GKPlayer*)playerPtr;
    return NPCreateCStringCopyFromNSString(player.displayName);
}

NPBINDING DONTSTRIP void NPPlayerLoadImage(void* playerPtr, void* tagPtr)
{
    GKPlayer*   player      = (__bridge GKPlayer*)playerPtr;
    [player loadPhotoForSize:GKPhotoSizeNormal withCompletionHandler:^(UIImage* _Nullable photo, NSError* _Nullable error) {
        // send data
        if (error)
        {
            _loadImageCallback(nil, -1, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        }
        else
        {
            NSData* imageData   = NPEncodeImageAsData(photo, UIImageEncodeTypePNG);
            _loadImageCallback((void*)[imageData bytes], (int)[imageData length], NPCreateError(nil), tagPtr);
        }
    }];
}

NPBINDING DONTSTRIP void NPLocalPlayerRegisterCallbacks(GameServicesAuthStateChangeNativeCallback authChangeCallback)
{
    // set value
    [NPGameKitManager setAuthChangeCallback:authChangeCallback];
}

NPBINDING DONTSTRIP void* NPLocalPlayerGetLocalPlayer()
{
    return (__bridge void*)[GKLocalPlayer localPlayer];
}

NPBINDING DONTSTRIP void NPLocalPlayerAuthenticate(bool interactive)
{
    [[NPGameKitManager sharedManager] authenticate: interactive];
}

NPBINDING DONTSTRIP bool NPLocalPlayerIsAuthenticated()
{
    return [[GKLocalPlayer localPlayer] isAuthenticated];
}

NPBINDING DONTSTRIP bool NPLocalPlayerIsUnderage()
{
    return [[GKLocalPlayer localPlayer] isUnderage];
}

NPBINDING DONTSTRIP void NPLocalPlayerLoadFriends(void* tagPtr)
{
    [[GKLocalPlayer localPlayer] loadFriendsAuthorizationStatus:^(GKFriendsAuthorizationStatus authorizationStatus, NSError * _Nullable error) {
        
            if(error != nil)
            {
                NSLog(@"Failed getting authorization status. Friends authorization status %ld", authorizationStatus);
                _loadPlayersCallback(NPCreateNativeArrayFromNSArray(nil), NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
                
                return;
            }
            
            [[GKLocalPlayer localPlayer] loadFriends:^(NSArray<GKPlayer *> * _Nullable friends, NSError * _Nullable error) {
                
                NPArray*    nativeArray     = NPCreateNativeArrayFromNSArray(friends);
                _loadPlayersCallback(nativeArray, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
                
                // release properties
                delete(nativeArray);
            }];
        }];
    
    
}

NPBINDING DONTSTRIP void NPLocalPlayerShowAddFriendRequestView(const char* playerId, void* tagPtr)
{
#if !TARGET_OS_TV
    NSError *error = nil;
    [[GKLocalPlayer localPlayer] presentFriendRequestCreatorFromViewController:UnityGetGLViewController() error: &error];
    
    if(_viewClosedCallback != nil)
    {
        if(error != nil )
        {
            _viewClosedCallback(NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        }
        else
        {
            _viewClosedCallback(NPCreateError(nil), tagPtr);
        }
    }
#else
    if(_viewClosedCallback != nil)
    {
        _viewClosedCallback(NPCreateError([NPGameServicesError notSupported]), tagPtr);
    }
#endif
}
