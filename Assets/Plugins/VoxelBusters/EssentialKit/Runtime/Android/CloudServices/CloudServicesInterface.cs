#if UNITY_ANDROID
using System.Collections;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using System;
using VoxelBusters.EssentialKit.Common.Android;
using System.Diagnostics;

namespace VoxelBusters.EssentialKit.CloudServicesCore.Android
{
    public sealed class CloudServicesInterface : NativeCloudServicesInterfaceBase, IApplicationLifecycleListener
    {
#region Static fields

        private NativeCloudServices m_instance;

#endregion

#region Constructors

        public CloudServicesInterface()
            : base(isAvailable: true)
        {
            m_instance = new NativeCloudServices(NativeUnityPluginUtility.GetContext());
            m_instance.SetExternalDataChangedListener(new NativeExternalDataChangedListener()
            {
                onChangeCallback = (NativeExternalChangeReason reason, string[] keys) =>
                {
                    SendSavedDataChangeEvent(Converter.from(reason), keys);
                }
            });

            m_instance.SetUserChangedListener(new NativeOnUserChangedListener()
            {
                onChangeCallback = (string userId) =>
                {
                    SendUserChangeEvent(new CloudUser(userId, userId == null ? CloudUserAccountStatus.NoAccount : CloudUserAccountStatus.Available), null);
                }
            });

            //TODO: Need to move this to native as native code should be handling all functionality on its own.
            ApplicationLifecycleObserver.Instance.AddListener(this);
        }

        ~CloudServicesInterface()
        {
            Dispose(false);
        }

#endregion

#region Base class methods

        public override bool GetBool(string key)
        {
            return m_instance.GetBool(key);
        }

        public override long GetLong(string key)
        {
            return m_instance.GetLong(key);
        }

        public override double GetDouble(string key)
        {
            return m_instance.GetDouble(key);
        }

        public override string GetString(string key)
        {
            return m_instance.GetString(key);
        }

        public override byte[] GetByteArray(string key)
        {
            NativeBytesWrapper nativeByteBuffer = m_instance.GetByteArray(key);
            return nativeByteBuffer.GetBytes();
        }

        public override bool HasKey(string key)
        {
            return m_instance.HasKey(key);
        }

        public override void SetBool(string key, bool value)
        {
            m_instance.SetBool(key, value);
        }

        public override void SetLong(string key, long value)
        {
            m_instance.SetLong(key, value);
        }

        public override void SetDouble(string key, double value)
        {
            m_instance.SetDouble(key, value);
        }

        public override void SetString(string key, string value)
        {
            m_instance.SetString(key, value);
        }

        public override void SetByteArray(string key, byte[] value)
        {
            m_instance.SetByteArray(key, new NativeBytesWrapper(value));
        }

        public override void RemoveKey(string key)
        {
            m_instance.RemoveKey(key);
        }

        public override void Synchronize(SynchronizeInternalCallback callback)
        {
            m_instance.Synchronize(new NativeSyncronizeListener()
            {
                onSuccessCallback = () =>
                {
                    callback?.Invoke(true, null);
                },
                onFailureCallback = (String error) =>
                {
                    callback?.Invoke(false, new Error(error));
                }

            });
        }

        public override IDictionary GetSnapshot()
        {
            string jsonStr = m_instance.GetSnapshot();
            if (jsonStr != null)
            {
                return (IDictionary)ExternalServiceProvider.JsonServiceProvider.FromJson(jsonStr);
            }

            return null;
        }

        #endregion

        #region Helpers

        private void Synchronize(NativeSyncronizeListener listener, bool forceReloginIfRequired)
        {
            UnityEngine.Debug.Log("Synchronize: " + forceReloginIfRequired);
            m_instance.Synchronize(listener, forceReloginIfRequired);
        }

        #endregion

        #region IApplicationLifecycleListener implementation

        public void OnApplicationFocus(bool hasFocus)
        {}

        public void OnApplicationPause(bool pauseStatus)
        {
            Synchronize(null, forceReloginIfRequired: false);
        }

        public void OnApplicationQuit()
        {
            Synchronize(null, forceReloginIfRequired: false);
        }

        #endregion
    }
}
#endif