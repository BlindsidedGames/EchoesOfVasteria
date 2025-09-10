using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Notification properties specific to iOS platform.
    /// </summary>
    /// @ingroup NotificationServices
    public class NotificationIosProperties
    {
        #region Properties

        /// <summary>
        /// The name of the launch image to display when your app is launched in response to the notification
        /// </summary>
        public string LaunchImageFileName { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationIosProperties"/> class.
        /// </summary>
        /// <param name="launchImageFileName">The name of the launch image to display when your app is launched in response to the notification.</param>
        public NotificationIosProperties(string launchImageFileName = null)
        {
            // set properties
            LaunchImageFileName     = launchImageFileName;
        }

        #endregion
    }
}