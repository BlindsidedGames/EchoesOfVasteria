#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;

namespace VoxelBusters.EssentialKit.MediaServicesCore.Android
{

    public sealed class MediaServicesInterface : NativeMediaServicesInterfaceBase 
    {
#region Fields

        private NativeMediaServices m_instance;

#endregion
#region Constructors

        public MediaServicesInterface()
            : base(isAvailable: true)
        {
            m_instance = new NativeMediaServices(NativeUnityPluginUtility.GetContext());
        }

#endregion

#region Base class methods

        public override GalleryAccessStatus GetGalleryAccessStatus(GalleryAccessMode mode)
        {
            if(mode == GalleryAccessMode.Read)
            {
                return Converter.from(m_instance.GetGalleryReadAccessStatus());
            }
            else
            {
                return Converter.from(m_instance.GetGalleryReadWriteAccessStatus());
            }
        }
        
        public override CameraAccessStatus GetCameraAccessStatus()
        {
            return Converter.from(m_instance.GetCameraAccessStatus());
        }

        public override void SelectMediaContent(MediaContentSelectOptions options, SelectMediaContentInternalCallback callback)
        {
            m_instance.SelectMediaContent(options.Title, options.AllowedMimeType, options.MaxAllowed, new NativeMediaContentSelectionListener()
            {
                onSuccessCallback = (assets) =>
                {
                    List<IMediaContent> list = new List<IMediaContent>();
                    foreach(NativeMediaContentBase content in assets.Get())
                    {
                        list.Add(new AssetMediaContent(content));
                    }

                    callback(list.ToArray(), null);

                },
                onFailureCallback = (error) =>
                {
                    callback(null, new Error(null, error.GetCode(), error.GetDescription()));
                }
            });
        }

        public override void CaptureMediaContent(MediaContentCaptureOptions options, CaptureMediaContentInternalCallback callback)
        {
            m_instance.CaptureMediaContent(captureType: (NativeMediaCaptureType)options.CaptureType, title: options.Title, fileName: "captured_media" ,new NativeMediaContentCaptureListener()
            {
                onSuccessCallback = (asset) =>
                {
                    //FinishLoadingAsset(assets, callback);
                    callback(new AssetMediaContent(asset), null);
                },
                onFailureCallback = (error) =>
                {
                    callback(null, new Error(null, error.GetCode(), error.GetDescription()));
                }
            });
        }

        public override void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, SaveMediaContentInternalCallback callback)
        {
            m_instance.SaveMediaContent(new NativeBytesWrapper(data), mimeType, options.DirectoryName, options.FileName, new NativeMediaContentSaveListener()
            {
                onSuccessCallback = () => callback(true, null),
                onFailureCallback = (error) =>
                {
                    callback(false, new Error(null, error.GetCode(), error.GetDescription()));
                }
            });
        }

#endregion

#region Utility methods

        private void FinishLoadingAsset(NativeAsset asset, SelectImageInternalCallback callback)
        {
            Debug.Log("Loading asset...");
            asset.Load(new NativeLoadAssetListener()
            {
                onSuccessCallback = (data) =>
                {
                    Debug.Log("Loading asset successful... : " + data + "Bytes : " + data.GetBytes());
                    callback(data.GetBytes(), null);
                },
                onFailureCallback = (error) =>
                {
                    Debug.Log("Loading asset failed... : " + error);

                    callback(null, MediaServicesError.DataNotAvailable(error));                }
            });
        }

#endregion
    }
}
#endif