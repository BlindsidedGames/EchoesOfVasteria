using System.Text;
using UnityEngine;
using UnityEngine.UI;
// key namespaces
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit;
// internal namespace
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;
using VoxelBusters.CoreLibrary;
using System;
using System.Globalization;

namespace VoxelBusters.EssentialKit.Demo
{
    public class MediaServicesDemo : DemoActionPanelBase<MediaServicesDemoAction, MediaServicesDemoActionType>
    {
        #region Fields

        [SerializeField]
        private Dropdown m_selectMediaContentOptionsMimeTypeDropdown = null;

        [SerializeField]
        private Dropdown m_selectMediaContentOptionsMaxAllowedDropdown = null;

        [SerializeField]
        private Dropdown m_captureMediaContentOptionsCaptureTypeDropDown = null;

        [SerializeField]
        private RawImage m_texturePreview;

        #endregion

        #region Base class methods

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnActionSelectInternal(MediaServicesDemoAction selectedAction)
        {
            switch (selectedAction.ActionType)
            {
                case MediaServicesDemoActionType.GetGalleryAccessStatus:
                    var     accessStatus1   = MediaServices.GetGalleryAccessStatus(GalleryAccessMode.ReadWrite);
                    Log("Gallery access status: " + accessStatus1);
                    break;

                case MediaServicesDemoActionType.GetCameraAccessStatus:
                    var     accessStatus2   = MediaServices.GetCameraAccessStatus();
                    Log("Camera access status: " + accessStatus2);
                    break;

                case MediaServicesDemoActionType.SelectMediaContent:
                    MediaContentSelectOptions options = new MediaContentSelectOptions(title: "Select Images", allowedMimeType: GetSelectMediaContentMimeType(), maxAllowed: GetSelectMediaContentMaxAllowed() );
                    MediaServices.SelectMediaContent(options, (contents, error) =>
                    {
                        if (error == null)
                        {
                            Log($"Selected media content: {contents.Length}");

                            foreach (var each in contents)
                            {
                                each.AsTexture2D((texture, error) => {
                                    if (error == null) {
                                        Log($"Successfully loaded the texture.");
                                        m_texturePreview.texture = texture;
                                    }
                                    else {
                                        Log($"Error loading as texture:{error}");
                                    }
                                });
                                /*each.AsRawMediaData((rawMediaData, error) =>
                                {
                                    if (error == null)
                                    {
                                        Log($"Selected media content with {rawMediaData.Mime} mime type");
                                    }
                                    else
                                    {
                                        Log($"Selected media content failed to load with error: {error}");
                                    }
                                });

                                each.AsFilePath(Application.persistentDataPath + "/" + "Save Dir", "picked_file", (string path, Error error) =>
                                {
                                    if (error == null)
                                    {
                                        Log($"Selected media content at path " + path);
                                    }
                                    else
                                    {
                                        Log($"Selected media content failed to get as file path: " + error);
                                    }
                                });*/
                            }
                        }
                        else
                        {
                            Log("Select image from gallery failed with error. Error: " + error);
                        }
                    });
                    break;

                case MediaServicesDemoActionType.CaptureMediaContent:
                    MediaContentCaptureOptions captureOptions = new MediaContentCaptureOptions(title: "Capture Image", fileName: "image", captureType: GetCaptureType());
                    MediaServices.CaptureMediaContent(captureOptions, (content, error) =>
                    {
                        if (error == null)
                        {
                            content.AsTexture2D(onComplete: (Texture2D texture, Error error) => {

                                if(error == null)
                                {
                                    Log("Capture content complete." + ((texture != null) ? $" Received texture size : [{texture.width} {texture.height}]" : $"Video data can't be converted to a texture2d. Please use AsFilePath for accessing the video file."));
                                    m_texturePreview.texture = texture;
                                }
                                else
                                {
                                    Log($"Captured media content failed with error: {error}");
                                }
                                
                            });

                            content.AsFilePath(Application.persistentDataPath + "/" + "Save Dir", "captured_file", (string path, Error error) =>
                            {
                                if (error == null)
                                {
                                    Log($"Captured media content at path " + path);
                                }
                                else
                                {
                                    Log($"Captured media content failed to get as file path: " + error);
                                }
                            });
                        }
                        else
                        {
                            Log("Capture image using camera failed with error. Error: " + error);
                        }
                    });
                    break;

                case MediaServicesDemoActionType.SaveMediaContent:
                    var image = ScreenCapture.CaptureScreenshotAsTexture();
                    MediaContentSaveOptions saveOptions = new MediaContentSaveOptions(directoryName: null, fileName: "essential-kit-save-media-image"); //Passing null as directory name as it may need additional permissions on some platforms(iOS). Only pass the directory/album name only if required.
                    MediaServices.SaveMediaContent(image.EncodeToPNG(), MimeType.kPNGImage, saveOptions, (result, error) =>
                    {
                        if (error == null)
                        {
                            Log("Save media to gallery finished successfully.");
                        }
                        else
                        {
                            Log("Save media to gallery failed with error. Error: " + error);
                        }
                    });
                    break;

                case MediaServicesDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kMediaServices);
                    break;

                default:
                    break;
            }
        }
        #endregion

        #region Private methods

        private string GetSelectMediaContentMimeType()
        {
            int index = m_selectMediaContentOptionsMimeTypeDropdown.value;
            return m_selectMediaContentOptionsMimeTypeDropdown.options[index].text;
        }

        private int GetSelectMediaContentMaxAllowed()
        {
            int index = m_selectMediaContentOptionsMaxAllowedDropdown.value;
            var valueStr = m_selectMediaContentOptionsMaxAllowedDropdown.options[index].text;
            int value = int.Parse(valueStr, CultureInfo.InvariantCulture);
            return value;
        }

        private MediaContentCaptureType GetCaptureType()
        {
            switch(m_captureMediaContentOptionsCaptureTypeDropDown.value)
            {
                case 0: 
                    return MediaContentCaptureType.Image;

                case 1:
                    return MediaContentCaptureType.Video;
            }

            return MediaContentCaptureType.Image;
        }


        #endregion

    }
}
