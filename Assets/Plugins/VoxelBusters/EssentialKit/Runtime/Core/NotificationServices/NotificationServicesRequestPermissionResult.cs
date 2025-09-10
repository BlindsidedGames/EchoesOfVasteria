using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="NotificationServices.RequestPermission(NotificationPermissionOptions, bool, EventCallback{NotificationServicesRequestPermissionResult})"/> operation is completed.
    /// </summary>
    /// @ingroup NotificationServices
    public class NotificationServicesRequestPermissionResult
    {
        #region Properties

        /// <summary>
        /// The permission granted by the user.
        /// </summary>
        public NotificationPermissionStatus PermissionStatus { get; private set; }

        #endregion

        #region Constructors

        internal NotificationServicesRequestPermissionResult(NotificationPermissionStatus permissionStatus)
        {
            // Set properties
            PermissionStatus    = permissionStatus;
        }

        #endregion
    }
}