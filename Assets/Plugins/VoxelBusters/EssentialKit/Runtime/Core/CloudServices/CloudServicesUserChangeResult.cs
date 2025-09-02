using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information related to <see cref="CloudServices.OnUserChange"/> event.
    /// </summary>
    /// @ingroup CloudServices
    public class CloudServicesUserChangeResult
    {
        #region Properties

        /// <summary>
        /// The cloud user.
        /// </summary>
        public ICloudUser User { get; private set; }

        #endregion

        #region Constructors

        internal CloudServicesUserChangeResult(ICloudUser user)
        {
            // set properties
            User        = user;
        }

        #endregion
    }
}