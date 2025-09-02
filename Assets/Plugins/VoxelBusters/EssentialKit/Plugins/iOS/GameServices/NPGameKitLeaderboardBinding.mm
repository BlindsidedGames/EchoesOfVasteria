//
//  NPGameKitLeaderboardBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <GameKit/GameKit.h>
#import "NPGameKitManager.h"
#import "NPGameServicesDataTypes.h"
#import "NPKit.h"
#import "NPManagedPointerCache.h"

// static fields
static GameServicesLoadArrayNativeCallback          _loadLeaderboardsCallback               = nil;
static GameServicesLoadScoresNativeCallback         _loadLeaderboardScoresCallback          = nil;
static GameServicesLoadImageNativeCallback          _loadImageCallback                      = nil;
static GameServicesReportNativeCallback             _reportScoreCallback                    = nil;

#pragma mark - Utilities

GKLeaderboard* getLeaderboard(const char* leaderboardId)
{
    NSString* baseIdentifier = NPCreateNSStringFromCString(leaderboardId);
    return [[NPGameKitManager sharedManager] getLeaderboard:baseIdentifier];
}


#pragma mark - Native binding calls

NPBINDING DONTSTRIP void NPLeaderboardRegisterCallbacks(GameServicesLoadArrayNativeCallback loadLeaderboardsCallback, GameServicesLoadScoresNativeCallback loadScoresCallback, GameServicesLoadImageNativeCallback loadImageCallback, GameServicesReportNativeCallback reportScoreCallback)
{
    // set properties
    _loadLeaderboardsCallback       = loadLeaderboardsCallback;
    _loadLeaderboardScoresCallback  = loadScoresCallback;
    _loadImageCallback              = loadImageCallback;
    _reportScoreCallback            = reportScoreCallback;
}

NPBINDING DONTSTRIP void NPLeaderboardLoadLeaderboards(const char** leaderboardIds, int length, void* tagPtr)
{
    NSArray<NSString*>* leaderboardIdArray  = NPCreateArrayOfNSString(leaderboardIds, length); 
    
    [[NPGameKitManager sharedManager] loadLeaderboards:leaderboardIdArray withCallback:^(NSArray<GKLeaderboard *> * _Nullable leaderboards, NSError * _Nullable error) {
        // send data
        NPArray*    nativeArray     = NPCreateNativeArrayFromNSArray(leaderboards);
        
        if(_loadLeaderboardsCallback != nil)
        {
            _loadLeaderboardsCallback(nativeArray, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        }
        
        // release properties
        delete(nativeArray);
    }];
}

NPBINDING DONTSTRIP void* NPLeaderboardCreate(const char* leaderboardId)
{
    GKLeaderboard*  leaderboard     = getLeaderboard(leaderboardId);
    return NPRetainWithOwnershipTransfer(leaderboard);
}

NPBINDING DONTSTRIP const char* NPLeaderboardGetId(void* leaderboardPtr)
{
    GKLeaderboard*  leaderboard     = (__bridge GKLeaderboard*)leaderboardPtr;
    return NPCreateCStringCopyFromNSString(leaderboard.baseLeaderboardID);
}

NPBINDING DONTSTRIP const char* NPLeaderboardGetTitle(void* leaderboardPtr)
{
    GKLeaderboard*  leaderboard     = (__bridge GKLeaderboard*)leaderboardPtr;
    return NPCreateCStringCopyFromNSString(leaderboard.title);
}

NPBINDING DONTSTRIP void NPLeaderboardLoadScores(void* leaderboardPtr, GKLeaderboardPlayerScope playerScope, GKLeaderboardTimeScope timeScope, long startIndex, int count, void* tagPtr)
{
    GKLeaderboard*  leaderboard     = (__bridge GKLeaderboard*)leaderboardPtr;
    leaderboard.range               = NSMakeRange(startIndex, count);
    
    [leaderboard loadEntriesForPlayerScope:playerScope timeScope:timeScope range:NSMakeRange(startIndex, count) completionHandler:^(GKLeaderboardEntry * _Nullable_result localPlayerEntry, NSArray<GKLeaderboardEntry *> * _Nullable entries, NSInteger totalPlayerCount, NSError * _Nullable error) {
        
        //Ideally, if no score exists, as per apple documentation, localPlayerEntry should be nil. But thats not the case. So checking for validity of the player.
        void* localPlayerEntryPtr = ((localPlayerEntry == nil) || localPlayerEntry.rank < 1) ? NULL : (__bridge void*)localPlayerEntry;
                
        // send data
        NPArray*    nativeArray     = NPCreateNativeArrayFromNSArray(entries);
        _loadLeaderboardScoresCallback(nativeArray, localPlayerEntryPtr, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        
        // release properties
        delete(nativeArray);
    }];
}

NPBINDING DONTSTRIP void NPLeaderboardLoadImage(void* leaderboardPtr, void* tagPtr)
{
#if !TARGET_OS_TV
    GKLeaderboard*  leaderboard     = (__bridge GKLeaderboard*)leaderboardPtr;
    [leaderboard loadImageWithCompletionHandler:^(UIImage* _Nullable image, NSError* _Nullable error) {
        // send data
        if (error)
        {
            _loadImageCallback(nil, -1, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        }
        else
        {
            NSData* imageData       = NPEncodeImageAsData(image, UIImageEncodeTypePNG);
            _loadImageCallback((void*)[imageData bytes], (int)[imageData length], NPCreateError(nil), tagPtr);
        }
    }];
#else
    if(_loadImageCallback != nil) {
        _loadImageCallback(nil, -1, NPCreateError([NPGameServicesError notSupported]), tagPtr);
    }
#endif
}

NPBINDING DONTSTRIP void NPLeaderboardShowView(const char* leaderboardID, int timeScope, void* tagPtr)
{
    // create view controller
    GKGameCenterViewController* gameCenterVC;
    gameCenterVC    = [[GKGameCenterViewController alloc] initWithLeaderboardID:NPCreateNSStringFromCString(leaderboardID) playerScope:GKLeaderboardPlayerScopeGlobal timeScope:(GKLeaderboardTimeScope)timeScope];
    [[NPGameKitManager sharedManager] showGameCenterViewController:gameCenterVC withTag:tagPtr];
}

NPBINDING DONTSTRIP void NPLeaderboardReportScore(void* leaderboardPtr, long value, const char* contextTag, void* tagPtr)
{
    GKLeaderboard*  leaderboard     = (__bridge GKLeaderboard*)leaderboardPtr;

    NSUInteger  context;
    NSString*   tag         = NPCreateNSStringFromCString(contextTag);
    
    if(tag != nil)
    {
        NSData*     data        = [tag dataUsingEncoding:NSASCIIStringEncoding];
        [data getBytes:&context length:sizeof(context)];
    }
    else
    {
        context = -1;
    }
    
    [leaderboard submitScore:value context:context player:[GKLocalPlayer localPlayer] completionHandler:^(NSError * _Nullable error) {
        _reportScoreCallback(NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
    }];
}
