#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.CloudServicesCore.iOS
{
    public sealed class CloudServicesInterface : NativeCloudServicesInterfaceBase, INativeCloudServicesInterface
    {
#region Static fields

        private     static      CloudServicesInterface     s_sharedInstance      = null;

#endregion

#region Constructors

        static CloudServicesInterface()
        {
            // initialise component
            CloudServicesBinding.NPCloudServicesRegisterCallbacks(userChangeCallback: HandleUserChangeNativeCallback, savedDataChangeCallback: HandleSavedDataChangeNativeCallback);
            CloudServicesBinding.NPCloudServicesInit();
        }

        public CloudServicesInterface()
            : base(isAvailable: true)
        {
            // cache instance
            s_sharedInstance    = this;
        }

        ~CloudServicesInterface()
        {
            Dispose(false);
        }

#endregion

#region Base class methods

        public override bool GetBool(string key)
        {
            return CloudServicesBinding.NPCloudServicesGetBool(key);
        }

        public override long GetLong(string key)
        { 
            return CloudServicesBinding.NPCloudServicesGetLong(key);
        }

        public override double GetDouble(string key)
        { 
            return CloudServicesBinding.NPCloudServicesGetDouble(key);
        }

        public override string GetString(string key)
        { 
            return CloudServicesBinding.NPCloudServicesGetString(key);
        }

        public override byte[] GetByteArray(string key)
        {
            int length;
            IntPtr byteArrayPtr = CloudServicesBinding.NPCloudServicesGetByteArray(key, out length);

            // Copy bytes to managed memory
            var bytes = new byte[length];
            Marshal.Copy(byteArrayPtr, bytes, 0, length);
            return bytes;
        }

        public override bool HasKey(string key)
        {
            return CloudServicesBinding.NPCloudServicesHasKey(key);
        }

        public override void SetBool(string key, bool value)
        {
            CloudServicesBinding.NPCloudServicesSetBool(key, value);
        }

        public override void SetLong(string key, long value)
        { 
            CloudServicesBinding.NPCloudServicesSetLong(key, value);
        }

        public override void SetDouble(string key, double value)
        { 
            CloudServicesBinding.NPCloudServicesSetDouble(key, value);
        }

        public override void SetString(string key, string value)
        { 
            CloudServicesBinding.NPCloudServicesSetString(key, value);
        }

        public override void SetByteArray(string key, byte[] value)
        {
            int     length  = (null == value) ? -1 : value.Length;
            CloudServicesBinding.NPCloudServicesSetByteArray(key, value, length);
        }

        public override void RemoveKey(string key)
        {
            CloudServicesBinding.NPCloudServicesRemoveKey(key);
        }

        public override void Synchronize(SynchronizeInternalCallback callback)
        {
            bool    status  = CloudServicesBinding.NPCloudServicesSynchronize();

            // send result
            callback(status, null);
        }

        public override IDictionary GetSnapshot()
        {
            string  jsonStr = CloudServicesBinding.NPCloudServicesSnapshot();
            if (jsonStr != null)
            {
                return (IDictionary)ExternalServiceProvider.JsonServiceProvider.FromJson(jsonStr);
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            // check whether object is released
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                // reset shared instance
                if (this == s_sharedInstance)
                {
                    s_sharedInstance = null;
                }
            }

            base.Dispose(disposing);
        }

#endregion

#region Native callback methods

        [MonoPInvokeCallback(typeof(UserChangeNativeCallback))]
        private static void HandleUserChangeNativeCallback(ref CKAccountData accountData, NativeError error)
        {
            string  userId      = MarshalUtility.ToString(accountData.AccountIdentifierPtr);
            var     status      = (userId == null) ? CloudUserAccountStatus.NoAccount : CloudUserAccountStatus.Available;
            var     errorObj    = error.Convert(CloudServicesError.kDomain);
            s_sharedInstance.SendUserChangeEvent(new CloudUser(userId, status), errorObj);
        }

        [MonoPInvokeCallback(typeof(SavedDataChangeNativeCallback))]
        private static void HandleSavedDataChangeNativeCallback(int changeReason, ref NativeArray changedKeys)
        {
            var     reasonCode              = CloudServicesUtility.ConvertToCloudSavedDataChangeReasonCode((NSUbiquitousKeyValueStoreChange)changeReason);
            var     changedKeysManagedArray = MarshalUtility.CreateStringArray(changedKeys.Pointer, changedKeys.Length);
            s_sharedInstance.SendSavedDataChangeEvent(reasonCode, changedKeysManagedArray);
        }

#endregion
    }
}
#endif