//
//  UNNotificationSettings+ManagedData.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "UNNotificationSettings+ManagedData.h"

@implementation UNNotificationSettings (ManagedData)

- (NPUnityNotificationSettings)toManagedData
{
    NPUnityNotificationSettings settingsData;
    
    // set default values for optional members
    settingsData.showPreviewsSetting        = 0;
    settingsData.criticalAlertSetting       = UNNotificationSettingDisabled;
    settingsData.announcementSetting        = UNNotificationSettingDisabled;
    
    // copy values
    settingsData.authorizationStatus        = self.authorizationStatus;
    
#if TARGET_OS_IOS
    settingsData.alertSetting               = self.alertSetting;
    settingsData.carPlaySetting             = self.carPlaySetting;
    settingsData.lockScreenSetting          = self.lockScreenSetting;
    settingsData.notificationCenterSetting  = self.notificationCenterSetting;
    settingsData.soundSetting               = self.soundSetting;
    settingsData.alertStyle                 = self.alertStyle;

    // iOS 11 and above
    if (@available(iOS 11.0, *))
    {
        settingsData.showPreviewsSetting    = self.showPreviewsSetting;
    }

    // iOS 12 and above
    if (@available(iOS 12.0, *))
    {
        settingsData.criticalAlertSetting   = self.criticalAlertSetting;
    }
    
    // iOS 12 and above
    if (@available(iOS 13.0, *))
    {
        settingsData.announcementSetting    = self.announcementSetting;
    }
#endif
    
    settingsData.badgeSetting               = self.badgeSetting;
    
    return settingsData;
}

@end
