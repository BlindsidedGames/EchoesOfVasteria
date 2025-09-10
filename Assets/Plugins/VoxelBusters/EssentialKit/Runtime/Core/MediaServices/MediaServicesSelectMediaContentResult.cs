using System.Collections.Generic;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains information about the media content selected by the user.
    /// </summary>
    /// @ingroup MediaServices
    public class MediaServicesSelectMediaContentResult
    {
        #region Properties


        /// <summary>
        /// The list of media contents selected by the user.
        /// </summary>
        /// <value>The list of media contents.</value>
        public IMediaContent[] Contents
        {
            get;
            private set;
        }

        #endregion

        #region Constructors

        internal MediaServicesSelectMediaContentResult(List<IMediaContent> contents)
        {
            // Set properties
            Contents    = contents.ToArray();
        }

        #endregion
    }
}
