#include <TargetConditionals.h>
#if TARGET_OS_IOS
#include <mach/mach.h>
#include <os/proc.h>
#include <stdint.h>

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
