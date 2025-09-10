using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor.NativePlugins;
using System.IO;

namespace VoxelBusters.EssentialKit.MediaServicesCore.Simulator
{
    public sealed class MediaServicesSimulator : SingletonObject<MediaServicesSimulator>
    {
        #region Constants

        // messages
        private     const       string              kGalleryUnauthorizedAccessError     = "Unauthorized access! Check permission before accessing gallery.";

        private     const       string              kGalleryAuthorizedError             = "Permission for accessing gallery is already provided. Please check access status before requesting access.";
        
        private     const       string              kCameraAuthorizedError              = "Permission for accessing gallery is already provided. Please check access status before requesting access.";

        #endregion

        #region Fields

        private     MediaServicesSimulatorData       m_simulatorData;

        #endregion

        #region Constructors

        private MediaServicesSimulator()
        {
            // initialise
            m_simulatorData  = LoadFromDisk() ?? new MediaServicesSimulatorData();
        }

        #endregion

        #region Database methods

        private MediaServicesSimulatorData LoadFromDisk()
        {
            return SimulatorServices.GetObject<MediaServicesSimulatorData>(NativeFeatureType.kMediaServices);
        }

        private void SaveData()
        {
            SimulatorServices.SetObject(NativeFeatureType.kMediaServices, m_simulatorData);
        }

        public static void Reset() 
        {
            SimulatorServices.RemoveObject(NativeFeatureType.kMediaServices);
        }

        #endregion

        #region Public methods

        public void RequestGalleryAccess(Action<GalleryAccessStatus, Error> callback)
        {
            // check whether required permission is already granted
            var     accessStatus    = GetGalleryAccessStatus();
            if (GalleryAccessStatus.Authorized == accessStatus)
            {
                callback(GalleryAccessStatus.Authorized, new Error(description: kGalleryAuthorizedError));
            }
            else
            {
                // show prompt to user asking for required permission
                var     applicationSettings = EssentialKitSettings.Instance.ApplicationSettings;
                var     usagePermission     = applicationSettings.UsagePermissionSettings.GalleryUsagePermission;

                var     newAlertDialog      = new AlertDialogBuilder()
                    .SetTitle("Gallery Simulator")
                    .SetMessage(usagePermission.GetDescriptionForActivePlatform())
                    .AddButton("Authorise", () => 
                    { 
                        // save selection
                        UpdateGalleryAccessStatus(GalleryAccessStatus.Authorized);

                        // send result
                        callback(GalleryAccessStatus.Authorized, null);
                    })
                    .AddCancelButton("Cancel", () => 
                    { 
                        // save selection
                        UpdateGalleryAccessStatus(GalleryAccessStatus.Denied);

                        // send result
                        callback(GalleryAccessStatus.Denied, null);
                    })
                    .Build();
                newAlertDialog.Show();
            }
        }

        public GalleryAccessStatus GetGalleryAccessStatus()
        {
            return m_simulatorData.GalleryAccessStatus;
        }

        public void RequestCameraAccess(Action<CameraAccessStatus, Error> callback)
        {
            // check whether required permission is already granted
            var     accessStatus    = GetCameraAccessStatus();
            if (CameraAccessStatus.Authorized == accessStatus)
            {
                callback(CameraAccessStatus.Authorized, null);
            }
            else
            {
                // show prompt to user asking for required permission
                var     applicationSettings = EssentialKitSettings.Instance.ApplicationSettings;
                var     usagePermission     = applicationSettings.UsagePermissionSettings.CameraUsagePermission;

                var     newAlertDialog      = new AlertDialogBuilder()
                    .SetTitle("Gallery Simulator")
                    .SetMessage(usagePermission.GetDescriptionForActivePlatform())
                    .AddButton("Authorise", () => 
                    { 
                        // save selection
                        UpdateCameraAccessStatus(CameraAccessStatus.Authorized);

                        // send result
                        callback(CameraAccessStatus.Authorized, null);
                    })
                    .AddCancelButton("Cancel", () => 
                    { 
                        // save selection
                        UpdateCameraAccessStatus(CameraAccessStatus.Denied);

                        // send result
                        callback(CameraAccessStatus.Denied, null);
                    })
                    .Build();
                newAlertDialog.Show();
            }
        }
        
        public CameraAccessStatus GetCameraAccessStatus()
        {
            return m_simulatorData.CameraAccessStatus;
        }

        public bool CanSelectImageFromGallery()
        {
            return (GalleryAccessStatus.Authorized == GetGalleryAccessStatus());
        }

        public void SelectMediaContent(MediaContentSelectOptions options, Action<IMediaContent[], Error> callback)
        {
            if(options.MaxAllowed > 1)
            {
                DebugLogger.LogWarning("Multiple media file selection is not supported in simulator. Defaulting to single selection.");
            }
            string extensions = GetExtensionsFromMimeType(options.AllowedMimeType);
            string  imagePath   = EditorUtility.OpenFilePanelWithFilters("Select media", "", extensions == null ? new string[] { "All files", "*"} : new string[] { "Media files", extensions});
                if (imagePath.Length != 0)
                {
                    List<IMediaContent> list = new List<IMediaContent>();
                    var mediaContent = MediaContent.From(IOServices.ReadFileData(imagePath), MimeType.GetTypeForExtension(Path.GetExtension(imagePath)));
                    list.Add(mediaContent);
                    callback(list.ToArray(), null);
                }
                else
                {
                    callback(null, new Error(description: "User cancelled operation."));
                }
        }

        public bool CanCaptureImageFromCamera()
        {
            return (CameraAccessStatus.Authorized == GetCameraAccessStatus());
        }

        public bool CanSaveImageToGallery()
        {
            return (GalleryAccessStatus.Authorized == GetGalleryAccessStatus());
        }

        public void CaptureMediaContent(MediaContentCaptureOptions options, Action<IMediaContent, Error> callback)
        {
            RequestCameraAccess((accessStatus, error) => {
                if (CameraAccessStatus.Authorized == accessStatus)
                {
                    var     image       = SimulatorServices.GetRandomImage();
                    callback(MediaContent.From(image), null);
                }
                else
                {
                    callback(null, MediaServicesError.PermissionNotAvailable());
                }
            });
        }

        public void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, SaveMediaContentInternalCallback callback)
        {
            RequestGalleryAccess((accessStatus, error) => {
                if (GalleryAccessStatus.Authorized == accessStatus)
                {
                    var     mediaContent = MediaContent.From(data, mimeType);
                    string extension = MimeType.GetExtensionForType(mimeType);
                    string  selectedPath    = EditorUtility.SaveFilePanel("Save media content", options.DirectoryName, $"{options.FileName}.{extension}", extension);
                    if (selectedPath.Length != 0)
                    {
                        mediaContent.AsFilePath(IOServices.GetDirectoryName(selectedPath), options.FileName, (savedPath, error) => {
                            callback(error == null, error);
                        });
                        return;
                    }

                    callback(false, MediaServicesError.UserCancelled());
                }
                else
                {
                    callback(false, MediaServicesError.PermissionNotAvailable());
                }
            });
        }

        #endregion

        #region Private methods

        private void UpdateGalleryAccessStatus(GalleryAccessStatus value)
        {
            m_simulatorData.GalleryAccessStatus = value;

            SaveData();
        }

        private void UpdateCameraAccessStatus(CameraAccessStatus value)
        {
            m_simulatorData.CameraAccessStatus  = value;

            SaveData();
        }

        private string GetExtensionsFromMimeType(string allowedMimeType)
        {
            if (allowedMimeType == null)
            {
                return null;
            }

            switch (allowedMimeType)
            {
                case "image/*":
                    return "jpg,jpeg,png";
                case "video/*":
                    return "mp4, avi, mkv";
                case "audio/*":
                    return "mp3, wav, ogg";
                default:
                    return MimeType.GetExtensionForType(allowedMimeType);
            }
        }

        #endregion
    }
}