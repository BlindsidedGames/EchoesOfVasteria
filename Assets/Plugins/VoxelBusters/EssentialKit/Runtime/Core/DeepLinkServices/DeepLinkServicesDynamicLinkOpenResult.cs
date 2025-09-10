using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when deep link is opened.
    /// </summary>
    /// @ingroup DeepLinkServices
    public class DeepLinkServicesDynamicLinkOpenResult
    {
        #region Properties

        /// <summary>
        /// The URL of the deep link.
        /// </summary>
        public Uri Url { get; private set; }

        /// <summary>
        /// The received notification as a raw string.
        /// </summary>
        public string RawUrlString { get; private set; }

        #endregion

        #region Constructors

        internal DeepLinkServicesDynamicLinkOpenResult(Uri url, string rawUrlString)
        {
            // Set properties
            Url             = url;
            RawUrlString    = rawUrlString;
        }

        #endregion
    }
}