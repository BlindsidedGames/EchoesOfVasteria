using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains options for selecting media content.
    /// </summary>
    /// @ingroup MediaServices
    public class MediaContentSelectOptions
    {
        #region Properties

        /// <summary>
        /// Gets the title which is shown to user.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the allowed mime type.
        /// </summary>
        /// <value>The allowed mime type.</value>
        public string AllowedMimeType { get; private set; }

        /// <summary>
        /// Gets the max allowed.
        /// </summary>
        /// <value>The max allowed.</value>
        public int MaxAllowed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MediaContentSelectOptions"/> show prepermission dialog.
        /// </summary>
        /// <value><c>true</c> if show prepermission dialog; otherwise, <c>false</c>.</value>
        private bool ShowPrepermissionDialog { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaContentSelectOptions"/> class.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="allowedMimeType">Allowed mime type.</param>
        /// <param name="maxAllowed">Max allowed contents to select.</param>
        public MediaContentSelectOptions(string title, string allowedMimeType, int maxAllowed)
        {
            // set properties
            Title                   = title;
            AllowedMimeType         = allowedMimeType;
            MaxAllowed              = maxAllowed;
            ShowPrepermissionDialog = false;
        }

        #endregion

        #region Utility methods

        /// @name Helpers
        /// @{
        
        /// <summary>
        /// Creates options for selecting image.
        /// </summary>
        /// <returns>The options.</returns>
        /// <param name="maxAllowed">Max allowed.</param>
        public static MediaContentSelectOptions CreateForImage(int maxAllowed = 1)
        {
            return new MediaContentSelectOptions(title: maxAllowed == 1 ? "Select image" : "Select images", allowedMimeType: MimeType.kAllImages, maxAllowed: maxAllowed);
        }

        /// <summary>
        /// Creates options for selecting video.
        /// </summary>
        /// <returns>The options.</returns>
        /// <param name="maxAllowed">Max allowed contents.</param>
        public static MediaContentSelectOptions CreateForVideo(int maxAllowed = 1)
        {
            return new MediaContentSelectOptions(title: maxAllowed == 1 ? "Select video" : "Select videos", allowedMimeType: MimeType.kAllVideos, maxAllowed: maxAllowed);
        }

        /// <summary>
        /// Creates options for selecting audio.
        /// </summary>
        /// <returns>The options.</returns>
        /// <param name="maxAllowed">Max allowed.</param>
        public static MediaContentSelectOptions CreateForAudio(int maxAllowed = 1)
        {
            return new MediaContentSelectOptions(title: maxAllowed == 1 ? "Select audio" : "Select audio", allowedMimeType: MimeType.kAllAudio, maxAllowed: maxAllowed);
        }

        /// <summary>
        /// Creates options for selecting any file type.
        /// </summary>
        /// <returns>The options.</returns>
        /// <param name="maxAllowed">Max allowed.</param>
        public static MediaContentSelectOptions CreateForAny(int maxAllowed = 1)
        {
            return new MediaContentSelectOptions(title: maxAllowed == 1 ? "Select file" : "Select files", allowedMimeType: MimeType.kAny, maxAllowed: maxAllowed);
        }
        /// @}
        #endregion
    }
}