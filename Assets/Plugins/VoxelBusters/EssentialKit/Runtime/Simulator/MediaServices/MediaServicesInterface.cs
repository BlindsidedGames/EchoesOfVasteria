using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit.MediaServicesCore.Simulator
{
    public sealed class MediaServicesInterface : NativeMediaServicesInterfaceBase 
    {
        #region Constructors

        public MediaServicesInterface()
            : base(isAvailable: true)
        { }

        #endregion
        
        #region Base class methods

        public override GalleryAccessStatus GetGalleryAccessStatus(GalleryAccessMode mode)
        {
            return MediaServicesSimulator.Instance.GetGalleryAccessStatus();
        }
        
        public override CameraAccessStatus GetCameraAccessStatus()
        {
            return MediaServicesSimulator.Instance.GetCameraAccessStatus();
        }

        public override void SelectMediaContent(MediaContentSelectOptions options, SelectMediaContentInternalCallback callback)
        {
            MediaServicesSimulator.Instance.SelectMediaContent(options, (contents, error) => callback(contents, error));
        }

        public override void CaptureMediaContent(MediaContentCaptureOptions options, CaptureMediaContentInternalCallback callback)
        {
            MediaServicesSimulator.Instance.CaptureMediaContent(options, (content, error) => callback(content, error));
        }

        public override void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, SaveMediaContentInternalCallback callback)
        {
            MediaServicesSimulator.Instance.SaveMediaContent(data, mimeType, options, (status, error) => callback(status, error));
        }

        #endregion
    }
}