//
//  NPAppShortcuts.h
//  Essential Kit
//
//  Created by Ayyappa on 09/01/25.
//  Copyright (c) 2024 Voxel Busters Interactive LLP. All rights reserved.

#define Domain @"App Shortcuts"

typedef enum : NSInteger
{
    AppShortcutsStatusTypeUnknown,
} AppShortcutsStatusType;

typedef enum : NSInteger
{
    AppShortcutsErrorCodeUnknown
} AppShortcutsErrorCode;

typedef void (^ShortcutClickedCallback)(NSString* shortcutItemId);



@interface AppShortcutItem : NSObject

@property (strong, nonatomic) NSString *identifier;
@property (strong, nonatomic) NSString *title;
@property (strong, nonatomic) NSString *subtitle;
@property (strong, nonatomic) NSString *iconFileName;

@end


@interface NPAppShortcuts : NSObject

+(void) setShortcutClickedCallback:(ShortcutClickedCallback) clickCallback;
-(void) add:(AppShortcutItem*) item;
-(void) remove:(NSString*) shortcutItemId;


@end
