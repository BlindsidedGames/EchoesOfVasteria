//
//  NPUnityAppController.h
//  Unity-iPhone
//
//  Created by Ashwin Kumar on 09/04/20.
//

#import <Foundation/Foundation.h>
#import "AppDelegateListener.h"
#import "UnityAppController.h"
#include "NPConfig.h"

#if NATIVE_PLUGINS_USES_DEEP_LINK
// callback signatures
typedef bool (*HandleUrlSchemeCallback)(const char* url);
typedef bool (*HandleUniversalLinkCallback)(const char* url);
#endif

#if NATIVE_PLUGINS_USES_APP_SHORTCUTS

typedef void (^HandleShortcutItemClickCallback)(UIApplicationShortcutItem* item);

#endif

@interface NPUnityAppController : UnityAppController

#if NATIVE_PLUGINS_USES_DEEP_LINK
// set handlers methods
+ (void)registerUrlSchemeHandler:(HandleUrlSchemeCallback)callback;
+ (void)registerUniversalLinkHandler:(HandleUniversalLinkCallback)callback;

- (void)initDeepLinkServices;
#endif

#if NATIVE_PLUGINS_USES_APP_SHORTCUTS
+ (void)registerShortcutActionHandler:(HandleShortcutItemClickCallback) callback;
#endif

@end
