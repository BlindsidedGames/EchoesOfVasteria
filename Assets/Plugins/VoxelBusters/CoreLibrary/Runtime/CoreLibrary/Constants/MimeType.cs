using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace VoxelBusters.CoreLibrary
{
    /// <summary>
    /// The MimeType class is a collection of most commonly used MIME types. 
    /// </summary>
    /// <description>
    /// MIME types enable apps to recognize the filetype of a file. 
    /// </description>
    public static class MimeType
    {
        #region Constants

        /// <summary> The MIME value used to determine any file. (Readonly)</summary>
        public  const   string      kAny          = "*/*";

        /// <summary> The MIME value used to determine plain text file. (Readonly)</summary>
        public  const   string      kPlainText          = "text/plain";

        /// <summary> The MIME value used to determine normal web pages. (Readonly)</summary>
        public  const   string      kHtmlText           = "text/html";

        /// <summary> The MIME value used to determine javascript content. (Readonly)</summary>
        public  const   string      kJavaScriptText     = "text/javascript";

        /// <summary> The MIME value used to determine all image files. (Readonly)</summary>
        public  const   string      kAllImages          = "image/*";

        /// <summary> The MIME value used to determine jpg image file. (Readonly)</summary>
        public  const   string      kJPGImage           = "image/jpeg";

        /// <summary> The MIME value used to determine png image file. (Readonly)</summary>
        public  const   string      kPNGImage           = "image/png";

        /// <summary> The MIME value used to determine gif image file. (Readonly)</summary>
        public  const   string      kGIFImage           = "image/gif";

        /// <summary> The MIME value used to determine Adobe® PDF documents. (Readonly)</summary>
        public  const   string      kPDF                = "application/pdf";


        /// <summary> The MIME value used to determine all video files. (Readonly)</summary>
        public  const   string      kAllVideos          = "video/*";

        /// <summary> The MIME value used to determine mp4 video. (Readonly)</summary>
        public  const   string      kMP4Video           = "video/mp4";

        /// <summary> The MIME value used to determine all audio files. (Readonly)</summary>
        public  const   string      kAllAudio          = "audio/*";

        #endregion

        #region Static methods

        public static string GetTypeForExtension(string extension)
        {
            extension = extension.TrimStart('.');
            if (string.Equals(extension, "txt", StringComparison.InvariantCultureIgnoreCase))
            {
                return kPlainText;
            }
            else if (string.Equals(extension, "html", StringComparison.InvariantCultureIgnoreCase))
            {
                return kHtmlText;
            }
            else if (string.Equals(extension, "js", StringComparison.InvariantCultureIgnoreCase))
            {
                return kJavaScriptText;
            }
            else if (string.Equals(extension, "jpg", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(extension, "jpeg", StringComparison.InvariantCultureIgnoreCase))
            {
                return kJPGImage;
            }
            else if (string.Equals(extension, "png", StringComparison.InvariantCultureIgnoreCase))
            {
                return kPNGImage;
            }
            else if (string.Equals(extension, "gif", StringComparison.InvariantCultureIgnoreCase))
            {
                return kGIFImage;
            }
            else if (string.Equals(extension, "pdf", StringComparison.InvariantCultureIgnoreCase))
            {
                return kPDF;
            }
            else if (string.Equals(extension, "mp4", StringComparison.InvariantCultureIgnoreCase))
            {
                return kMP4Video;
            }

            DebugLogger.LogWarning(CoreLibraryDomain.Default, $"Unknown MIME type for extension: {extension}");
            return kPlainText;
        }

        public static string GetExtensionForType(string mimeType)
        {
            if (string.Equals(mimeType, kPlainText, StringComparison.InvariantCultureIgnoreCase))
            {
                return "txt";
            }
            else if (string.Equals(mimeType, kHtmlText, StringComparison.InvariantCultureIgnoreCase))
            {
                return "html";
            }
            else if (string.Equals(mimeType, kJavaScriptText, StringComparison.InvariantCultureIgnoreCase))
            {
                return "js";
            }
            else if (string.Equals(mimeType, kJPGImage, StringComparison.InvariantCultureIgnoreCase))
            {
                return "jpg";
            }
            else if (string.Equals(mimeType, kPNGImage, StringComparison.InvariantCultureIgnoreCase))
            {
                return "png";
            }
            else if (string.Equals(mimeType, kGIFImage, StringComparison.InvariantCultureIgnoreCase))
            {
                return "gif";
            }
            else if (string.Equals(mimeType, kPDF, StringComparison.InvariantCultureIgnoreCase))
            {
                return "pdf";
            }
            else if (string.Equals(mimeType, kMP4Video, StringComparison.InvariantCultureIgnoreCase))
            {
                return "mp4";
            }
            else if (string.Equals(mimeType, kAllImages, StringComparison.InvariantCultureIgnoreCase)) //Defaults to png
            {
                return "png";
            }
            else if (string.Equals(mimeType, kAllVideos, StringComparison.InvariantCultureIgnoreCase)) //Defaults to mp4
            {
                return "mp4";
            }
            else if (string.Equals(mimeType, kAllAudio, StringComparison.InvariantCultureIgnoreCase)) //Defaults to wav
            {
                return "wav";
            }

            DebugLogger.LogWarning(CoreLibraryDomain.Default, $"Unknown extension for MIME type : {mimeType}");
            return kPlainText;
        }

        #endregion
    }
}