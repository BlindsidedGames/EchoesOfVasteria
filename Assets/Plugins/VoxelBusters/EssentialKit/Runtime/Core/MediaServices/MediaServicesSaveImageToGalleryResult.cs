using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="MediaServices.SaveImageToGallery(Texture2D, EventCallback{MediaServicesSaveImageToGalleryResult})"/> operation is completed.
    /// </summary>
    /// @deprecated Use <see cref="MediaServices.SaveMediaContent"/> instead
    [Obsolete("This class is obsolete. Use SaveMediaContent instead.", true)]  //Obsolete:2024
    public class MediaServicesSaveImageToGalleryResult
    {
        #region Properties

        /// <summary>
        /// The status of requested operation.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; private set; }

        #endregion

        #region Constructors

        internal MediaServicesSaveImageToGalleryResult(bool success)
        {
            // Set properties
            Success     = success;
        }

        #endregion
    }
}
