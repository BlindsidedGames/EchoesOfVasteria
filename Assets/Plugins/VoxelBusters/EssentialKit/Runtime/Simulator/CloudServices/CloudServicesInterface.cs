using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.CloudServicesCore.Simulator
{
    public sealed class CloudServicesInterface : NativeCloudServicesInterfaceBase, INativeCloudServicesInterface
    {
        #region Properties

        private string m_userId = null;

        #endregion

        #region Constructors

        public CloudServicesInterface()
            : base(isAvailable: true)
        { }

        #endregion

        #region Base class methods

        public override bool GetBool(string key)
        {
            string  value   = CloudServicesSimulator.Instance.GetData(key);

            // convert to source type
            bool    result;
            bool.TryParse(value, out result);

            return result;
        }

        public override long GetLong(string key)
        { 
            string  value   = CloudServicesSimulator.Instance.GetData(key);

            // convert to source type
            long    result;
            long.TryParse(value, out result);

            return result;
        }

        public override double GetDouble(string key)
        { 
            string  value   = CloudServicesSimulator.Instance.GetData(key);

            // convert to source type
            double  result;
            double.TryParse(value, out result);

            return result;
        }

        public override string GetString(string key)
        { 
            return CloudServicesSimulator.Instance.GetData(key);
        }

        public override byte[] GetByteArray(string key)
        {
            string  value   = CloudServicesSimulator.Instance.GetData(key);
            return (value == null) ? null : Convert.FromBase64String(value);
        }

        public override bool HasKey(string key)
        {
            return CloudServicesSimulator.Instance.HasKey(key);
        }

        public override void SetBool(string key, bool value)
        { 
            CloudServicesSimulator.Instance.AddData(key, value.ToString());
        }

        public override void SetLong(string key, long value)
        { 
            CloudServicesSimulator.Instance.AddData(key, value.ToString());
        }

        public override void SetDouble(string key, double value)
        { 
            CloudServicesSimulator.Instance.AddData(key, value.ToString());
        }

        public override void SetString(string key, string value)
        { 
            CloudServicesSimulator.Instance.AddData(key, value);
        }

        public override void SetByteArray(string key, byte[] value)
        {
            string  valueStr    = Convert.ToBase64String(value);
            CloudServicesSimulator.Instance.AddData(key, valueStr);
        }

        public override void RemoveKey(string key)
        { 
            CloudServicesSimulator.Instance.RemoveData(key);
        }

        public override void Synchronize(SynchronizeInternalCallback callback)
        {
            if(m_userId == null)
            {
                m_userId = "DummyUser";
                SendUserChangeEvent(new CloudUser(m_userId, CloudUserAccountStatus.Available), null);
            }

            CloudServicesSimulator.Instance.Synchronize();
            callback(true, null);
        }

        public override IDictionary GetSnapshot()
        {
            string  jsonStr     = CloudServicesSimulator.Instance.GetSnapshot();
            return (IDictionary)ExternalServiceProvider.JsonServiceProvider.FromJson(jsonStr);
        }

        #endregion
    }
}