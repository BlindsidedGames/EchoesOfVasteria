#include <TargetConditionals.h>
#if TARGET_OS_IOS
#import <Foundation/Foundation.h>
#include <mach/mach.h>
#include <os/proc.h>
#include <stdint.h>
#include <string.h>

// IL2CPP does not expose the Simulator launch arguments through Environment on
// every Unity iOS runtime. Copy into caller-owned storage without transferring
// an NSString or native allocation across the managed boundary.
extern "C" int EovReadStartupDiagnosticMode(uint8_t* buffer, int capacity)
{
    if (buffer == nullptr || capacity <= 0) return 0;
    buffer[0] = 0;
    NSString* prefix = @"-eovDiagnosticMode=";
    for (NSString* argument in [[NSProcessInfo processInfo] arguments])
    {
        if (![argument hasPrefix:prefix]) continue;
        NSData* value = [[argument substringFromIndex:[prefix length]] dataUsingEncoding:NSUTF8StringEncoding];
        if ([value length] == 0 || [value length] >= static_cast<NSUInteger>(capacity)) return 0;
        memcpy(buffer, [value bytes], [value length]);
        buffer[[value length]] = 0;
        return static_cast<int>([value length]);
    }
    return 0;
}

// Read-only diagnostics. No entitlements, allocation-limit changes or exception handlers.
extern "C" int EovReadStartupMemory(uint64_t* footprint, uint64_t* available)
{
    task_vm_info_data_t info = {};
    mach_msg_type_number_t count = TASK_VM_INFO_COUNT;
    kern_return_t result = task_info(mach_task_self(), TASK_VM_INFO,
                                    reinterpret_cast<task_info_t>(&info), &count);
    *footprint = result == KERN_SUCCESS ? info.phys_footprint : 0;
    *available = 0;
    if (@available(iOS 13.0, *)) *available = os_proc_available_memory();
    return result == KERN_SUCCESS ? 1 : 0;
}
#endif
