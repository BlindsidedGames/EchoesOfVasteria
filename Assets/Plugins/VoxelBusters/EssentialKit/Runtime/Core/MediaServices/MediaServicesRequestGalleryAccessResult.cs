using System.Collections;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="MediaServices.RequestGalleryAccess(GalleryAccessMode, bool, EventCallback{MediaServicesRequestGalleryAccessResult})"/> operation is completed.
    /// </summary>
    /// @ingroup MediaServices
    public class MediaServicesRequestGalleryAccessResult
    {
        #region Properties

        /// <summary>
        /// The access permission provided by the user.
        /// </summary>
        public GalleryAccessStatus AccessStatus { get; private set; }

        #endregion

        #region Constructors

        internal MediaServicesRequestGalleryAccessResult(GalleryAccessStatus accessStatus)
        {
            // Set properties
            AccessStatus    = accessStatus;
        }

        #endregion
    }
}
