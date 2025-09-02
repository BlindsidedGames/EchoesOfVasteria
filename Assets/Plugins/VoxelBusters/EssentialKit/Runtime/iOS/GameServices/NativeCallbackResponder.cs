#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
using VoxelBusters.EssentialKit;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.GameServicesCore.iOS
{
    internal static class NativeCallbackResponder
    {
        #region Native callback methods

        [MonoPInvokeCallback(typeof(GameServicesLoadImageNativeCallback))]
        public static void HandleLoadImageNativeCallback(IntPtr dataArrayPtr, int dataLength, NativeError error, IntPtr tagPtr)
        {
            // get handle from pointer
            var     handle      = GCHandle.FromIntPtr(tagPtr);
            var     callback    = (LoadImageInternalCallback)handle.Target;

            try
            {
                var     errorObj        = error.Convert(GameServicesError.kDomain);
                if (errorObj == null)
                {
                    // create texture from raw data
                    var     byteArray   = new byte[dataLength];
                    Marshal.Copy(dataArrayPtr, byteArray, 0, dataLength);

                    // send result
                    callback(byteArray, null);
                }
                else
                {
                    // send result
                    callback(null, errorObj);
                }
            }
            finally
            {
                // release handle
                handle.Free();
            }
        }
        
        [MonoPInvokeCallback(typeof(GameServicesViewClosedNativeCallback))]
        public static void HandleViewClosedNativeCallback(NativeError error, IntPtr tagPtr)
        {
            GCHandle    tagHandle   = GCHandle.FromIntPtr(tagPtr);

            try
            {
                // send result
                var     callback    = (ViewClosedInternalCallback)tagHandle.Target;
                var     errorObj    = error.Convert(GameServicesError.kDomain);
                callback.Invoke(errorObj);
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

        [MonoPInvokeCallback(typeof(GameServicesLoadServerCredentialsNativeCallback))]
        public static void HandleLoadServerCredentialsNativeCallback(string publicKeyUrl, IntPtr signaturePtr, int signatureDataLength, IntPtr saltPtr, int saltDataLength, long timestamp, NativeError error, IntPtr tagPtr)
        {
            GCHandle tagHandle = GCHandle.FromIntPtr(tagPtr);
            var callback = (LoadServerCredentialsInternalCallback)tagHandle.Target;

            try
            {
                var     errorObj        = error.Convert(GameServicesError.kDomain);
                if (errorObj == null)
                {
                    // create signature data from signaturePtr
                    var signature = new byte[signatureDataLength];
                    Marshal.Copy(signaturePtr, signature, 0, signatureDataLength);


                    // create salt data from saltPtr
                    var salt = new byte[saltDataLength];
                    Marshal.Copy(saltPtr, salt, 0, saltDataLength);


                    ServerCredentials.IosPlatformProperties iosPlatformProperties = new ServerCredentials.IosPlatformProperties(publicKeyUrl, signature, salt, timestamp);
                    ServerCredentials serverCredentials = new ServerCredentials(iosProperties: iosPlatformProperties);

                    // send result
                    callback(serverCredentials, new List<ServerCredentialAdditionalScope>(), null);
                }
                else
                {
                    // send result
                    callback(null, null, errorObj);
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

        #endregion
    }
}
#endif