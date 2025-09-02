using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when notification message is received.
    /// </summary>
    /// @ingroup NotificationServices
    public class NotificationServicesNotificationReceivedResult
    {
        #region Properties

        /// <summary>
        /// The received notification.
        /// </summary>
        public INotification Notification { get; private set; }

        #endregion

        #region Constructors

        internal NotificationServicesNotificationReceivedResult(INotification notification)
        {
            // Set properties
            Notification    = notification;
        }

        #endregion
    }
}