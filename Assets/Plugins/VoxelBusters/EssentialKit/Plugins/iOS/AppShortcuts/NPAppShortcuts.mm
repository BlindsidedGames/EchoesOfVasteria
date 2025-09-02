//
//  NPAppShortcuts.mm
//  Essential Kit
//
//  Created by Ayyappa on 09/01/25.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#import "NPKit.h"
#import "NPAppShortcuts.h"
#import "UnityInterface.h"
#import "NSError+Utility.h"
#import "NPUnityAppController.h"



static ShortcutClickedCallback shortcutClickedCallback;


@implementation AppShortcutItem
@end

@interface NPAppShortcuts ()


@end

@implementation NPAppShortcuts

-(NPAppShortcuts*) init
{
    
    self  =  [super init];
    return self;;
}

+(void) setShortcutClickedCallback:(ShortcutClickedCallback) callback
{
    shortcutClickedCallback = callback;
    
    [NPUnityAppController registerShortcutActionHandler:^(UIApplicationShortcutItem *item) {
        shortcutClickedCallback(item.type);
    }];
}

-(void) add:(AppShortcutItem*) item
{
    UIApplicationShortcutIcon *icon = ([item.iconFileName length] == 0) ? nil : [UIApplicationShortcutIcon iconWithTemplateImageName:item.iconFileName];
    UIApplicationShortcutItem* newItem = [[UIApplicationShortcutItem alloc] initWithType:item.identifier localizedTitle:item.title localizedSubtitle:item.subtitle icon:icon userInfo:nil];
 
    //Remove if existing and add as new
    NSMutableArray* items = [self removeItemFromCurrentShortcutItems:item.identifier];
    [items addObject:newItem];
    
    [UIApplication sharedApplication].shortcutItems = items;
}

-(void) remove:(NSString*) shortcutItemId
{
    NSArray* items = [self removeItemFromCurrentShortcutItems: shortcutItemId];
    [UIApplication sharedApplication].shortcutItems = items;
}

-(NSMutableArray*) removeItemFromCurrentShortcutItems:(NSString*) shortcutItemId
{
    NSArray* currentItems = [[UIApplication sharedApplication] shortcutItems];
    NSMutableArray* updatedItems = [NSMutableArray arrayWithArray:currentItems];

    for (UIApplicationShortcutItem* eachItem in currentItems) {
        if([shortcutItemId isEqualToString:eachItem.type]) {
            [updatedItems removeObject:eachItem];
            break;
        }
    }
    
    return updatedItems;
}

@end
