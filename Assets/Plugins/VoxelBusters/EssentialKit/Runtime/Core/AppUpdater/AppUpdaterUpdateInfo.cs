using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// The <see cref="AppUpdaterUpdateInfo"/> class provides information related to the status of the update.
    /// </summary>
    /// @ingroup AppUpdater  
    public class AppUpdaterUpdateInfo
    {
        #region Properties

        /// <summary>
        /// The status that indicates if an update is available or not available or in progress.
        /// </summary>
        public AppUpdaterUpdateStatus Status { get; private set; }

        private int BuildTag {get; set;}

        #endregion

        #region Constructors

        internal AppUpdaterUpdateInfo(AppUpdaterUpdateStatus status, int buildTag)
        {
            // Set properties
            Status = status;
            BuildTag = buildTag;
        }

        #endregion

        public override string ToString()
        {
            return $"[Status={Status}]";
        }
    }
}