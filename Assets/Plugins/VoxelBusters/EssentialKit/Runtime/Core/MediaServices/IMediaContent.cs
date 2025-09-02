using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Interface to be implemented by media content classes.
    /// </summary>
    /// <description>
    /// This interface provides methods to convert the media content to various formats.
    /// </description>
    /// @ingroup MediaServices
    public interface IMediaContent
    {
        /// <summary>
        /// Converts the media content to a <see cref="Texture2D"/>
        /// </summary>
        /// <description>
        /// The callback will be invoked with the converted texture.
        /// </description>
        /// <param name="onComplete">Callback to be invoked when the conversion is completed.</param>
        void AsTexture2D(EventCallback<Texture2D> onComplete);

        /// <summary>
        /// Converts the media content to a <see cref="RawMediaData"/>.
        /// </summary>
        /// <description>
        /// The callback will be invoked with the converted raw media data.
        /// </description>
        /// <param name="onComplete">Callback to be invoked when the conversion is completed.</param>
        void AsRawMediaData(EventCallback<RawMediaData> onComplete);

        /// <summary>
        /// Converts the media content to a file path.
        /// </summary>
        /// <description>
        /// The callback will be invoked with the converted file path.
        /// </description>
        /// <param name="destinationDirectory">The directory to which the file should be saved.</param>
        /// <param name="fileName">The name of the file to be saved.</param>
        /// <param name="onComplete">Callback to be invoked when the conversion is completed.</param>
        void AsFilePath(string destinationDirectory, string fileName, EventCallback<string> onComplete);
    }
}