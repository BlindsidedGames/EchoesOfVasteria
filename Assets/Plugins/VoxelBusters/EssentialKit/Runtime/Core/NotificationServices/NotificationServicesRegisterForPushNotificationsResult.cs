using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="NotificationServices.RegisterForPushNotifications(EventCallback{NotificationServicesRegisterForPushNotificationsResult})"/> operation is completed.
    /// </summary>
    /// @ingroup NotificationServices
    public class NotificationServicesRegisterForPushNotificationsResult
    {
        #region Properties

        /// <summary>
        /// The device token.
        /// </summary>
        public string DeviceToken { get; private set; }

        #endregion

        #region Constructors

        internal NotificationServicesRegisterForPushNotificationsResult(string deviceToken)
        {
            // Set properties
            DeviceToken     = deviceToken;
        }

        #endregion
    }
}