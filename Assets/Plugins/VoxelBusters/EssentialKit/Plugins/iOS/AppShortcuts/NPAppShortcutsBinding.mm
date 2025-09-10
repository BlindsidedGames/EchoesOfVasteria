//
//  NPAppShortcutsBinding.mm
//  Essential Kit
//
//  Created by Ayyappa on 09/01/25.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPKit.h"
#import "NPAppShortcuts.h"

// callback signatures
typedef void (*ShortcutClickedNativeCallback)(const char* shortcutId);

struct AppShortcutItemData
{
    const char*     identifier;
    const char*     title;
    const char*     subtitle;
    const char*     iconFileName;
};
typedef struct AppShortcutItemData AppShortcutItemData;

static NPAppShortcuts* cachedAppShortcuts;

NPAppShortcuts* getAppShortcuts()
{
    if(cachedAppShortcuts == nil)
    {
        cachedAppShortcuts = [[NPAppShortcuts alloc] init];
    }
    
    return cachedAppShortcuts;
}

NPBINDING DONTSTRIP void NPAppShortcutsSetShortcutClickedCallback(ShortcutClickedNativeCallback callback)
{
    [NPAppShortcuts setShortcutClickedCallback:^(NSString *shortcutItemId) {
        if(callback != nil)
        {
            callback(NPCreateCStringFromNSString(shortcutItemId));
        }
    }];
}


NPBINDING DONTSTRIP void NPAppShortcutsAddShortcut(AppShortcutItemData itemData)
{
    NPAppShortcuts *appShortcuts = getAppShortcuts();
    AppShortcutItem *item = [[AppShortcutItem alloc] init];
    item.identifier     = NPCreateNSStringFromCString(itemData.identifier);
    item.title          = NPCreateNSStringFromCString(itemData.title);
    item.subtitle       = NPCreateNSStringFromCString(itemData.subtitle);
    item.iconFileName   = NPCreateNSStringFromCString(itemData.iconFileName);
    

    [appShortcuts add:item];
}

NPBINDING DONTSTRIP void NPAppShortcutsRemoveShortcut(const char* taskId)
{
    NPAppShortcuts *AppShortcuts = getAppShortcuts();
    [AppShortcuts remove:NPCreateNSStringFromCString(taskId)];
}
