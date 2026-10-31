#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <objc/message.h>

static Class EOVActivityClass(void) {
    if (@available(iOS 16.2, *)) return NSClassFromString(@"EOVLiveActivityBridge");
    return Nil;
}

// Unity's iOS IL2CPP runtime may expose only argv[0] to managed Environment.
// Preserve the prototype's explicit demo launch opt-in using the native arguments.
extern "C" int EOVLiveActivity_IsDemoLaunch(void) {
    return [[NSProcessInfo processInfo].arguments containsObject:@"-eov-background-demo"] ? 1 : 0;
}

extern "C" int EOVLiveActivity_IsForeground(void) {
    __block int active = 0;
    void (^read)(void) = ^{
        static NSInteger lastState = -1;
        NSInteger state = UIApplication.sharedApplication.applicationState;
        if (state != lastState) {
            NSLog(@"[EOVLiveActivity] resume UIKit state=%ld", (long)state);
            lastState = state;
        }
        active = state == UIApplicationStateActive ? 1 : 0;
    };
    if ([NSThread isMainThread]) read(); else dispatch_sync(dispatch_get_main_queue(), read);
    return active;
}

extern "C" void EOVLiveActivity_End(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        Class bridge = EOVActivityClass();
        SEL selector = NSSelectorFromString(@"end");
        if (bridge && [bridge respondsToSelector:selector])
            ((void (*)(id, SEL))objc_msgSend)(bridge, selector);
    });
}

extern "C" void EOVWatchCompanion_Publish(const char *json) {
    if (!json) return;
    NSString *payload = [[NSString alloc] initWithUTF8String:json];
    if (!payload) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        Class bridge = NSClassFromString(@"EOVWatchPhoneSession");
        SEL selector = NSSelectorFromString(@"publish:");
        if (bridge && [bridge respondsToSelector:selector])
            ((void (*)(id, SEL, NSString *))objc_msgSend)(bridge, selector, payload);
        bridge = EOVActivityClass();
        selector = NSSelectorFromString(@"publishSnapshot:");
        if (bridge && [bridge respondsToSelector:selector])
            ((void (*)(id, SEL, NSString *))objc_msgSend)(bridge, selector, payload);
    });
}

extern "C" int EOVWatchCompanion_ConsumeRefreshRequest(void) {
    __block int requested = 0;
    void (^read)(void) = ^{
        Class bridge = NSClassFromString(@"EOVWatchPhoneSession");
        SEL selector = NSSelectorFromString(@"consumeRefreshRequest");
        if (bridge && [bridge respondsToSelector:selector])
            requested = ((int (*)(id, SEL))objc_msgSend)(bridge, selector);
    };
    if ([NSThread isMainThread]) read(); else dispatch_sync(dispatch_get_main_queue(), read);
    return requested;
}
