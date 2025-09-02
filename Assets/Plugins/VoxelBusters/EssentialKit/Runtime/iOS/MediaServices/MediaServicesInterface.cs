#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
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
            var     status      = MediaServicesBinding.NPMediaServicesGetPhotoLibraryAccessStatus();
            return MediaServicesUtility.ConvertToGalleryAccessStatus(status);
        }

        public override CameraAccessStatus GetCameraAccessStatus()
        {
            var     status      = MediaServicesBinding.NPMediaServicesGetCameraAccessStatus();
            return MediaServicesUtility.ConvertToCameraAccessStatus(status);
        }

        public override void SelectMediaContent(MediaContentSelectOptions options, SelectMediaContentInternalCallback callback)
        {
            var tagPtr = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesSelectMediaContent(MediaServicesUtility.Convert(options), tagPtr, HandleSelectMediaContentNativeCallback);
        }

        public override void CaptureMediaContent(MediaContentCaptureOptions options, CaptureMediaContentInternalCallback callback)
        {
            var tagPtr = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesCaptureMediaContent(MediaServicesUtility.Convert(options), tagPtr, HandleCaptureMediaContentNativeCallback);
        }

        public override void SaveMediaContent(byte[] data, string mimeType, MediaContentSaveOptions options, SaveMediaContentInternalCallback callback)
        {
            var tagPtr = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesSaveMediaContent(data, data.Length, mimeType, MediaServicesUtility.Convert(options), tagPtr, HandleSaveMediaContentNativeCallback);
        }

        /*
        public override void RequestGalleryAccess(GalleryAccessMode mode, RequestGalleryAccessInternalCallback callback)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesRequestPhotoLibraryAccess(tagPtr);
        }
        public override void RequestCameraAccess(RequestCameraAccessInternalCallback callback)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesRequestCameraAccess(tagPtr);
        }
        
    

        public override bool CanSelectImageFromGallery()
        {
            return MediaServicesBinding.NPMediaServicesCanPickImageFromGallery();
        }

        public override void SelectImageFromGallery(bool canEdit, SelectImageInternalCallback callback)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            IosNativePluginsUtility.SetLastTouchPosition();
            MediaServicesBinding.NPMediaServicesPickImageFromGallery(canEdit, tagPtr);
        }

        public override bool CanCaptureImageFromCamera()
        {
            return MediaServicesBinding.NPMediaServicesCanPickImageFromCamera();
        }

        public override void CaptureImageFromCamera(bool canEdit, SelectImageInternalCallback callback)
        {
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesPickImageFromCamera(canEdit, tagPtr);
        }

        public override bool CanSaveImageToGallery()
        {
            return MediaServicesBinding.NPMediaServicesCanSaveImageToAlbum();
        }

        public override void SaveImageToGallery(string albumName, Texture2D image, SaveImageToGalleryInternalCallback callback)
        {
            var     imageData   = image.Encode();
            var     handle      = GCHandle.Alloc(imageData, GCHandleType.Pinned);

            // make request
            var     tagPtr      = MarshalUtility.GetIntPtr(callback);
            MediaServicesBinding.NPMediaServicesSaveImageToAlbum(albumName, handle.AddrOfPinnedObject(), imageData.Length, tagPtr);

            // release pinned data object
            handle.Free();
        }
        */

        #endregion

        #region Native callback methods


        [MonoPInvokeCallback(typeof(SelectMediaContentInternalNativeCallback))]
        private static void HandleSelectMediaContentNativeCallback(ref NativeArray mediaContentsPtr, NativeError error, IntPtr tagPtr)
        {
            var tagHandle = GCHandle.FromIntPtr(tagPtr);

            try
            {
                SelectMediaContentInternalCallback callback = (SelectMediaContentInternalCallback)tagHandle.Target;

                // send result
                var managedArray = MarshalUtility.CreateManagedArray(mediaContentsPtr.Pointer, mediaContentsPtr.Length);
                var mediaContents = (managedArray == null) ? null : Array.ConvertAll(managedArray, (nativePtr) => new NativeMediaContent(nativePtr));
                var errorObj = error.Convert(MediaServicesError.kDomain);

                callback(mediaContents, errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        [MonoPInvokeCallback(typeof(CaptureMediaContentInternalNativeCallback))]
        private static void HandleCaptureMediaContentNativeCallback(IntPtr capturedMediaContentPtr, NativeError error, IntPtr tagPtr)
        {
            var tagHandle = GCHandle.FromIntPtr(tagPtr);

            try
            {
                CaptureMediaContentInternalCallback callback = (CaptureMediaContentInternalCallback)tagHandle.Target;

                var errorObj = error.Convert(MediaServicesError.kDomain);
                NativeMediaContent mediaContent = null;

                // send result
                if(errorObj == null)
                {
                    mediaContent = new NativeMediaContent(capturedMediaContentPtr);
                }
                
                callback(mediaContent, errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        [MonoPInvokeCallback(typeof(SaveMediaContentInternalNativeCallback))]
        private static void HandleSaveMediaContentNativeCallback(bool success, NativeError error, IntPtr tagPtr)
        {
            var tagHandle = GCHandle.FromIntPtr(tagPtr);

            try
            {
                // send result
                SaveMediaContentInternalCallback callback = (SaveMediaContentInternalCallback)tagHandle.Target;
                var errorObj = error.Convert(MediaServicesError.kDomain);

                callback(success, errorObj);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release handle
                tagHandle.Free();
            }
        }

        #endregion
    }
}
#endif