//
//  NPGameKitObserver.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <GameKit/GameKit.h>
#import "NPGameServicesDataTypes.h"
#import "NPGameServicesError.h"
#import "UIViewController+Presentation.h"

@interface NPGameKitManager : NSObject<GKGameCenterControllerDelegate, NPUIPopoverPresentationControllerDelegate>

+ (NPGameKitManager*_Nonnull)sharedManager;
+ (void)setGameCenterViewClosedCallback:(GameServicesViewClosedNativeCallback _Nullable )callback;
+ (void)setAuthChangeCallback:(GameServicesAuthStateChangeNativeCallback _Nullable )callback;
+ (void)setGameCenterLoadServerCredentialsCompleteCallback:(GameServicesLoadServerCredentialsNativeCallback _Nullable )callback;

- (void)authenticate :(BOOL) interactive;
- (bool)isShowingGameCenterView;
- (void)showGameCenterViewController:(GKGameCenterViewController*_Nonnull)viewController withTag:(void*_Nullable)tagPtr;
- (void)loadServerCredentials: (void*_Nonnull) tagPtr;

- (void)loadLeaderboards:(NSArray<NSString*>*_Nullable) leaderboardIdArray withCallback:(void(^_Nullable)(NSArray<GKLeaderboard*>* _Nullable leaderboards, NPGameServicesError* _Nullable error)) callback;
- (GKLeaderboard*_Nullable) getLeaderboard: (NSString*_Nullable) baseIdentifier;

//TODO: Move remaining methods from leaderboard and achievement binding classes to this.

@end

