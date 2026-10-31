#import <Foundation/Foundation.h>
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

extern "C" int EOVLiveActivity_IsSupported(void) {
    __block int supported = 0;
    void (^read)(void) = ^{
        Class bridge = EOVActivityClass();
        SEL selector = NSSelectorFromString(@"isSupported");
        if (bridge && [bridge respondsToSelector:selector])
            supported = ((BOOL (*)(id, SEL))objc_msgSend)(bridge, selector) ? 1 : 0;
    };
    if ([NSThread isMainThread]) read();
    else dispatch_sync(dispatch_get_main_queue(), read);
    return supported;
}

extern "C" void EOVLiveActivity_StartOrUpdate(const char *json) {
    if (!json) return;
    NSString *payload = [[NSString alloc] initWithUTF8String:json];
    if (!payload) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        Class bridge = EOVActivityClass();
        SEL selector = NSSelectorFromString(@"startOrUpdate:");
        if (bridge && [bridge respondsToSelector:selector])
            ((void (*)(id, SEL, NSString *))objc_msgSend)(bridge, selector, payload);
    });
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
