using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Defines the type of push notification service which sends the notifications to the device.
    /// </summary>
    public enum PushNotificationServiceType
    {
        /// <summary> Disables receiving remote push notifications completely as no service is used to send push notifications.</summary>
        None = 0,

        /// <summary> Custom push notification backend service which sends the payload to the device in plugin's expected format.</summary>
        Custom
    }
}