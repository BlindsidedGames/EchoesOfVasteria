#if UNITY_IOS || UNITY_TVOS
using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;

namespace VoxelBusters.EssentialKit.MediaServicesCore.iOS
{
    internal class NativeMediaContent : NativeObjectBase, IMediaContent
    {
        internal NativeMediaContent(IntPtr nativePtr) : base(nativeObjectRef: new IosNativeObjectRef(nativePtr))
        {
        }

        public void AsFilePath(string destinationDirectory, string fileName, EventCallback<string> onComplete)
        {
            IntPtr tagPtr = MarshalUtility.GetIntPtr(onComplete);
            MediaContentBinding.NPMediaContentAsFilePath(AddrOfNativeObject(), destinationDirectory, fileName, tagPtr, HandleMediaContentAsFilePathNativeCallback);
        }

        public void AsRawMediaData(EventCallback<RawMediaData> onComplete)
        {
            IntPtr tagPtr = MarshalUtility.GetIntPtr(onComplete);
            MediaContentBinding.NPMediaContentAsRawData(AddrOfNativeObject(), tagPtr, HandleMediaContentGetRawDataNativeCallback);
        }

        public void AsTexture2D(EventCallback<Texture2D> onComplete)
        {
            AsRawMediaData((rawData, error) => {
                if (error == null)
                {
                    Texture2D texture = rawData.Mime.StartsWith("image") ? new TextureData(rawData.Bytes).GetTexture() : null;
                    CallbackDispatcher.InvokeOnMainThread(onComplete, texture, null);
                }
                else
                {
                    CallbackDispatcher.InvokeOnMainThread(onComplete, null, error);
                }
            });
        }

        [MonoPInvokeCallback(typeof(MediaContentGetRawDataNativeCallback))]
        private static void HandleMediaContentGetRawDataNativeCallback(IntPtr byteArrayPtr, int byteLength, string mime, NativeError nativeError, IntPtr tagPtr)
        {
            var tagHandle = GCHandle.FromIntPtr(tagPtr);
            var error = nativeError.Convert();

            try
            {
                EventCallback<RawMediaData> callback = (EventCallback<RawMediaData>)tagHandle.Target;

                if (error == null)
                {
                    var byteArray = new byte[byteLength];
                    Marshal.Copy(byteArrayPtr, byteArray, 0, byteLength);

                    // send result
                    CallbackDispatcher.InvokeOnMainThread(callback, new RawMediaData(byteArray, mime), null);
                }
                else
                {
                    // send result
                    CallbackDispatcher.InvokeOnMainThread(callback, null, error);
                }
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

        [MonoPInvokeCallback(typeof(MediaContentAsFilePathNativeCallback))]
        private static void HandleMediaContentAsFilePathNativeCallback(string path, NativeError nativeError, IntPtr tagPtr)
        {
            var tagHandle = GCHandle.FromIntPtr(tagPtr);
            var error = nativeError.Convert();

            try
            {
                EventCallback<string> callback = (EventCallback<string>)tagHandle.Target;
                CallbackDispatcher.InvokeOnMainThread(callback, path, error);
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
    }
}
#endif