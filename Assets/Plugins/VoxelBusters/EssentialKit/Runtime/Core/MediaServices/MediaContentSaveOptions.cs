namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains options which customize the behavior of the <see cref="MediaServices.SaveMediaContent"/> method.
    /// </summary>
    /// @ingroup MediaServices
    public class MediaContentSaveOptions
    {
        #region Properties

        /// <summary>
        /// Gets the directory name where the media content will be saved.
        /// </summary>
        /// <value>The directory name (album name).</value>
        public string DirectoryName { get; private set; }

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <value>The file name without extension.</value>
        public string FileName { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaContentSaveOptions"/> class.
        /// </summary>
        /// <param name="directoryName">The directory name(album) where the media content will be saved.</param>
        /// <param name="fileName">The file name without extension.</param>
        public MediaContentSaveOptions(string directoryName, string fileName)
        {
            // set properties
            DirectoryName   = directoryName;
            FileName        = fileName;
        }
    }
}