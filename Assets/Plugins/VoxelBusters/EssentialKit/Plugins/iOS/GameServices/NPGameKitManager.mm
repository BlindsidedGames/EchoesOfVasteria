//
//  NPGameKitObserver.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPGameKitManager.h"
#import "NPKit.h"
#import "NPManagedPointerCache.h"
#import "UnityInterface.h"
#import "UIViewController+Presentation.h"

// static fields
static NPGameKitManager*                                _sharedObserver                             = nil;

static GameServicesViewClosedNativeCallback             _gameCenterViewClosedCallback               = nil;
static GameServicesAuthStateChangeNativeCallback        _authChangeCallback                         = nil;
static GameServicesLoadServerCredentialsNativeCallback  _gameCenterLoadServerCredentialsCallback    = nil;

NPBINDING DONTSTRIP void NPLeaderboardLoadLeaderboards(const char** leaderboardIds, int length, void* tagPtr);

@interface NPGameKitManager ()

@property(nonatomic, strong)    GKGameCenterViewController* gameCenterViewController;
@property(nonatomic)            BOOL    isInitialised;
@property(nonatomic, strong)    NSArray<GKLeaderboard*>* cachedLeaderboards;

@end

@implementation NPGameKitManager

@synthesize gameCenterViewController = _gameCenterViewController;
@synthesize isInitialised = _isInitialised;
@synthesize cachedLeaderboards = _cachedLeaderboards;

#pragma mark - Static methods

+ (NPGameKitManager*)sharedManager
{
    if (nil == _sharedObserver)
    {
        _sharedObserver = [[NPGameKitManager alloc] init];
    }
    
    return _sharedObserver;
}

+ (void)setGameCenterViewClosedCallback:(GameServicesViewClosedNativeCallback)callback
{
    _gameCenterViewClosedCallback  = callback;
}

+ (void)setAuthChangeCallback:(GameServicesAuthStateChangeNativeCallback)callback
{
    _authChangeCallback             = callback;
}

+ (void)setGameCenterLoadServerCredentialsCompleteCallback:(GameServicesLoadServerCredentialsNativeCallback)callback
{
    _gameCenterLoadServerCredentialsCallback    = callback;
}



+ (GKLocalPlayerAuthState)convertAuthFlagToEnumValue:(bool)isAuthenticated
{
    return isAuthenticated ? GKLocalPlayerAuthStateAvailable : GKLocalPlayerAuthStateNotFound;
}

#pragma mark - Instance methods

- (id)init
{
    self    = [super init];
    if (self)
    {
        // register for system notification
        [[NSNotificationCenter defaultCenter] addObserver:self
                                                 selector:@selector(authenticationDidChangeNotification:) name:GKPlayerAuthenticationDidChangeNotificationName
                                                   object:nil];
    }
    
    return self;
}

- (void)dealloc
{
    // unregister from notification
    [[NSNotificationCenter defaultCenter] removeObserver:self
                                                    name:GKPlayerAuthenticationDidChangeNotificationName
                                                  object:nil];
}

- (void)authenticate:(BOOL) interactive
{
    __weak GKLocalPlayer*  localPlayer = [GKLocalPlayer localPlayer];
    
    if (!localPlayer.isAuthenticated)
    {
        // set auth handler
        localPlayer.authenticateHandler = ^(UIViewController* viewController, NSError* error) {
            
            if(localPlayer.isAuthenticated)
            {
                NSLog(@"Player already authenticated...");
            }
            else if (error != nil)
            {
                GKLocalPlayerAuthState  state   = [NPGameKitManager convertAuthFlagToEnumValue: localPlayer.isAuthenticated];
                NSError* resolvedError = (state == GKLocalPlayerAuthStateAvailable) ? nil : [NPGameServicesError createFrom:error];
                _authChangeCallback(state, NPCreateError(resolvedError));
            }
            else if (viewController != nil)
            {
                if (!interactive)
                {
                    _authChangeCallback(GKLocalPlayerAuthStateNotFound, NPCreateError(nil));
                }
                else
                {
                    // notify observer that we are authenticating the user
                    _authChangeCallback(GKLocalPlayerAuthStateAuthenticating, NPCreateError(nil));
                    
                    // show corresponding view
                    [UnityGetGLViewController() presentViewController:viewController animated:YES completion:^{
                        NSLog(@"Showing auth view.");
                    }];
                }
            }
            else
            {
                NSLog(@"Reached Unexpected case. Please report to the developer.");
            }
        };
    }
    else
    {
        _authChangeCallback(GKLocalPlayerAuthStateAvailable, NPCreateError(nil));
    }
}

- (bool)isShowingGameCenterView
{
    return (self.gameCenterViewController != nil);
}

- (void)showGameCenterViewController:(GKGameCenterViewController*)viewController withTag:(void*)tagPtr
{
    // cache reference
    self.gameCenterViewController   = viewController;
    
    // configure contoller
    _gameCenterViewController.gameCenterDelegate    = self;

    // add instance to tracker
    [[NPManagedPointerCache sharedInstance] addPointer:tagPtr forKey:_gameCenterViewController];
    
    [UnityGetGLViewController() presentViewControllerInFullScreen: _gameCenterViewController
                                               animated: YES
                                             completion: nil];
}

- (void)loadServerCredentials: (void*) tagPtr
{
    
    if(![[GKLocalPlayer localPlayer] isAuthenticated]) {
        
        _gameCenterLoadServerCredentialsCallback(nil, nil, NULL, nil, 0, 0, NPCreateError([NPGameServicesError notAuthenticated]), tagPtr);
        
        return;
    }
    
    [[GKLocalPlayer localPlayer] fetchItemsForIdentityVerificationSignature:^(NSURL *publicKeyUrl, NSData *signature, NSData *salt, uint64_t timestamp, NSError *error)
     {
        if(error == nil)
        {
            NSString *publicKeyUrlString       = [publicKeyUrl absoluteString];
                         
            _gameCenterLoadServerCredentialsCallback(NPCreateCStringCopyFromNSString(publicKeyUrlString), (void*)[signature bytes], (int)[signature length], (void*)[salt bytes], (int)[salt length], timestamp, NPCreateError(nil), tagPtr);
        }
        else
        {
            //TODO: Test different errors and pass the relevant error code
            _gameCenterLoadServerCredentialsCallback(nil, nil, NULL, nil, 0, 0, NPCreateError([NPGameServicesError createFrom:error]), tagPtr);
        }
        
    }];
}

#pragma mark - Notification callback

- (void)authenticationDidChangeNotification:(NSNotification *)notification
{
    // notify listener
    GKLocalPlayer*          localPlayer = [GKLocalPlayer localPlayer];
    GKLocalPlayerAuthState  state       = [NPGameKitManager convertAuthFlagToEnumValue: localPlayer.isAuthenticated];
    
    if(state == GKLocalPlayerAuthStateAvailable && !_isInitialised)
    {
        [self loadLeaderboards:nil withCallback:^(NSArray<GKLeaderboard *> * _Nullable leaderboards, NPGameServicesError * _Nullable error) {
            
            self.isInitialised = (error == nil);
            if(error != nil) {
                NSLog(@"Unexpected - Failed fetching leaderboards with error %@. This can be due to no network or connection issues. Failing authentication.", error);
                _authChangeCallback(GKLocalPlayerAuthStateNotFound, NPCreateError(nil));
            } else {
                _authChangeCallback(state, NPCreateError(nil));
            }
        }];
    }
    else
    {
        _authChangeCallback(state, NPCreateError(nil));
    }
}

#pragma mark - GKGameCenterControllerDelegate implementation

- (void)gameCenterViewControllerDidFinish:(nonnull GKGameCenterViewController*)gameCenterViewController
{
    NSLog(@"[NativePlugins] Game center view closed.");
    
    // dimiss view controller
    [UnityGetGLViewController() dismissViewControllerAnimated:YES completion:nil];
    
    // invoke associated event
    id      key     = gameCenterViewController;
    void*   pointer = [[NPManagedPointerCache sharedInstance] pointerForKey:key];
    if (pointer != nil)
    {
        _gameCenterViewClosedCallback(NPCreateError(nil), pointer);
    }

    // reset properties
    [[NPManagedPointerCache sharedInstance] removePointerForKey:key];
    self.gameCenterViewController   = nil;
}

#pragma mark - UIPopoverPresentationControllerDelegate implementation

- (void)presentationControllerDidDismiss:(UIPresentationController*)presentationController
{
    GKGameCenterViewController* gcViewController    = (GKGameCenterViewController*)presentationController.presentingViewController;
    [self gameCenterViewControllerDidFinish:gcViewController];
}

#pragma mark - Leaderboards implementation

- (void)loadLeaderboards:(NSArray<NSString*>*) leaderboardIdArray withCallback:(void(^_Nullable)(NSArray<GKLeaderboard*>* _Nullable leaderboards, NPGameServicesError* _Nullable error)) callback
{
    [GKLeaderboard loadLeaderboardsWithIDs:leaderboardIdArray completionHandler:^(NSArray<GKLeaderboard*>* _Nullable leaderboards, NSError* _Nullable error) {
        
        self.cachedLeaderboards = leaderboards;
        
        callback(leaderboards, [NPGameServicesError createFrom:error]);
    }];
}

- (GKLeaderboard*) getLeaderboard: (NSString*) baseIdentifier
{
    if(_cachedLeaderboards == nil) {
        NSLog(@"No cached leaderboards found. This can be because load leaderboards call fail to load. Contact developer.");
        return nil;
    }
    
    for (GKLeaderboard* each in _cachedLeaderboards) {
        if([each.baseLeaderboardID isEqualToString:baseIdentifier]) {
            return each;
        }
    }
    
    NSLog(@"No leaderboard found with platform identifier: %@", baseIdentifier);
    return nil;
}

@end

#pragma mark - Native binding calls

NPBINDING DONTSTRIP void NPGameServicesSetViewClosedCallback(GameServicesViewClosedNativeCallback callback)
{
    _gameCenterViewClosedCallback   = callback;
}

NPBINDING DONTSTRIP void NPGameServicesLoadServerCredentials(void* tagPtr)
{
    [[NPGameKitManager sharedManager] loadServerCredentials: tagPtr];
}

NPBINDING DONTSTRIP void NPGameServicesLoadServerCredentialsCompleteCallback(GameServicesLoadServerCredentialsNativeCallback callback)
{
    _gameCenterLoadServerCredentialsCallback = callback;
}



