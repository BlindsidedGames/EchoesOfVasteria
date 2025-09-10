//
//  NPUnityAppController.mm
//  Unity-iPhone
//
//  Created by Ashwin Kumar on 09/04/20.
//

#import "NPUnityAppController.h"
#import "NPKit.h"

#if NATIVE_PLUGINS_USES_DEEP_LINK
static HandleUrlSchemeCallback _handleUrlSchemeCallback = nil;
static HandleUniversalLinkCallback _handleUniversalLinkCallback = nil;
#endif

#if NATIVE_PLUGINS_USES_APP_SHORTCUTS
static HandleShortcutItemClickCallback  _handleShortcutClickedCallback = nil;
static UIApplicationShortcutItem*       _cachedClickedShortcut = nil;
#endif

@interface NPUnityAppController ()

#if NATIVE_PLUGINS_USES_DEEP_LINK
@property(nonatomic) bool               isDeepLinkServiceInitialised;
@property(nonatomic, copy) NSURL*       cachedUrlScheme;
@property(nonatomic, copy) NSURL*       cachedUniversalLink;
#endif

@end

@implementation NPUnityAppController

#if NATIVE_PLUGINS_USES_DEEP_LINK
@synthesize isDeepLinkServiceInitialised    = _isDeepLinkServiceInitialised;
@synthesize cachedUrlScheme                 = _cachedUrlScheme;
@synthesize cachedUniversalLink             = _cachedUniversalLink;
#endif

#pragma mark - Static methods

#if NATIVE_PLUGINS_USES_DEEP_LINK
+ (void)registerUrlSchemeHandler:(HandleUrlSchemeCallback)callback;
{
    _handleUrlSchemeCallback        = callback;
}

+ (void)registerUniversalLinkHandler:(HandleUniversalLinkCallback)callback
{
    _handleUniversalLinkCallback    = callback;
}

- (void)initDeepLinkServices
{
    // update status
    self.isDeepLinkServiceInitialised   = true;
    
    // dispatch events
    if (self.cachedUrlScheme)
    {
        [self handleUrlScheme:self.cachedUrlScheme];
        self.cachedUrlScheme        = nil;
    }
    if (self.cachedUniversalLink)
    {
        [self handleUniversalLink:self.cachedUniversalLink];
        self.cachedUniversalLink    = nil;
    }
}

- (bool)handleUrlScheme:(NSURL*)url
{
    if (_handleUrlSchemeCallback != nil)
    {
        return _handleUrlSchemeCallback(NPCreateCStringFromNSString([url absoluteString]));
    }
    
    return false;
}

- (bool)handleUniversalLink:(NSURL*)url
{
    if (_handleUniversalLinkCallback != nil)
    {
        return _handleUniversalLinkCallback(NPCreateCStringFromNSString([url absoluteString]));
    }
    
    return false;
}

- (BOOL)application:(UIApplication*)application openURL:(NSURL*)url options:(NSDictionary<UIApplicationOpenURLOptionsKey, id> *)options
{
    // forward callback to parent class
    bool    originalResult  = false;
    if ([super respondsToSelector:@selector(application:openURL:options:)])
    {
        originalResult      = [super application:application openURL:url options:options];
    }

    
    // check the availability of the service and process the incoming request
    // store the event information for later usage incase if the service is not active
    if (self.isDeepLinkServiceInitialised)
    {
        return [self handleUrlScheme:url];
    }
    self.cachedUrlScheme    = url;
    
    return originalResult;
}

- (BOOL)application:(UIApplication *)application continueUserActivity:(NSUserActivity *)userActivity restorationHandler:(void(^)(NSArray<id<UIUserActivityRestoring>> * __nullable restorableObjects))restorationHandler
{
    // forward callback to parent class
    bool    originalResult  = false;
    if ([super respondsToSelector:@selector(application:continueUserActivity:restorationHandler:)])
    {
        originalResult      = [super application:application continueUserActivity:userActivity restorationHandler:restorationHandler];
    }

    // check whether this event is concerned with web activity
    if ([userActivity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb])
    {
        NSURL* url  = userActivity.webpageURL;
        if (url)
        {
            // check whether the embedded link can be handled by our application
            // store the event information for later usage incase if the service is not active
            if (self.isDeepLinkServiceInitialised)
            {
                return [self handleUniversalLink:url];
            }
            
            self.cachedUniversalLink    = url;
        }
    }
        
    return originalResult;
}
#endif

#if NATIVE_PLUGINS_USES_APP_SHORTCUTS

- (void)application:(UIApplication *)application performActionForShortcutItem:(UIApplicationShortcutItem *)shortcutItem completionHandler:(void(^)(BOOL succeeded))completionHandler API_AVAILABLE(ios(9.0)) API_UNAVAILABLE(tvos);
{
    [self handleShortcutClicked: shortcutItem];
    completionHandler(TRUE);
}

+(void) registerShortcutActionHandler:(HandleShortcutItemClickCallback) callback
{
    _handleShortcutClickedCallback = callback;
    
    if(_cachedClickedShortcut != nil)
    {
        //Trigger with as async as by this time event receivers may not be setup yet.
        dispatch_async(dispatch_get_main_queue(), ^{
                callback(_cachedClickedShortcut);
                _cachedClickedShortcut = nil;
            });
    }
}

-(void) handleShortcutClicked:(UIApplicationShortcutItem*) shortcutItem
{
    if(_handleShortcutClickedCallback == nil)
    {
        _cachedClickedShortcut = shortcutItem;
    }
    else
    {
        _handleShortcutClickedCallback(shortcutItem);
    }
}
#endif

@end

#if NATIVE_PLUGINS_USES_DEEP_LINK || NATIVE_PLUGINS_USES_APP_SHORTCUTS
// inform Unity to use UnityDeeplinksAppController as the main app controller:
IMPL_APP_CONTROLLER_SUBCLASS(NPUnityAppController)
#endif
