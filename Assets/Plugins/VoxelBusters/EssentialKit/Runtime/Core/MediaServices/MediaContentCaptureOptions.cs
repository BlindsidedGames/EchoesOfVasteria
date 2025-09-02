namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains options which customize the behavior of the <see cref="MediaServices.CaptureMediaContent"/> method.
    /// </summary>
    /// @ingroup MediaServices
    public class MediaContentCaptureOptions
    {
        #region Properties

        /// <summary>
        /// Gets the type of media content.
        /// </summary>
        /// <value>The type of media content.</value>
        public MediaContentCaptureType CaptureType { get; private set; }

        /// <summary>
        /// Gets the title of the capture.
        /// </summary>
        /// <value>The title of the capture.</value>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the file name.
        /// </summary>
        /// <value>The file name without extension.</value>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the source of media content.
        /// </summary>
        /// <value>The source of media content.</value>
        public MediaContentCaptureSource Source { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaContentCaptureOptions"/> class.
        /// </summary>
        /// <param name="captureType">Type of the capture.</param>
        /// <param name="title">Title of the capture.</param>
        /// <param name="fileName">File name without extension.</param>
        /// <param name="source">Source of media content.</param>
        public MediaContentCaptureOptions(MediaContentCaptureType captureType, string title, string fileName, MediaContentCaptureSource source = MediaContentCaptureSource.Camera)
        {
            CaptureType = captureType;
            Title = title;
            FileName = fileName;
            Source = source;
        }

        #region Utility methods

        /// <summary>
        /// Creates the options for image capture.
        /// </summary>
        /// <returns>The options for image capture.</returns>
        public static MediaContentCaptureOptions CreateForImage()
        {
            return new MediaContentCaptureOptions(MediaContentCaptureType.Image, title: "Capture image", "captured_image", MediaContentCaptureSource.Camera);
        }

        /// <summary>
        /// Creates the options for video capture.
        /// </summary>
        /// <returns>The options for video capture.</returns>
        public static MediaContentCaptureOptions CreateForVideo()
        {
            return new MediaContentCaptureOptions(MediaContentCaptureType.Video, title: "Capture video", "captured_video", MediaContentCaptureSource.Camera);
        }

        public override string ToString()
        {
            return $"Capture Type: {CaptureType}, Title: {Title}, File Name: {FileName}";
        }

        #endregion
    }
}