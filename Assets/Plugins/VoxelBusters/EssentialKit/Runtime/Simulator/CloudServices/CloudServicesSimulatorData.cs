using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.CloudServicesCore.Simulator
{
    [Serializable]
    internal sealed class CloudServicesSimulatorData
    {
        #region Fields

        [SerializeField]
        private     List<StringKeyValuePair>    m_keyValues     = new List<StringKeyValuePair>();

        #endregion

        #region Public methods

        public void AddData(string key, string value)
        {
            // create item
            StringKeyValuePair  item    = new StringKeyValuePair() { Key = key, Value = value };

            // insert item
            int                 index   = FindIndexWithKey(key);
            if (index == -1)
            {
                m_keyValues.Add(item);
            }
            else
            {
                m_keyValues[index]      = item;
            }
        }

        public string GetData(string key)
        {
            int     index   = FindIndexWithKey(key);
            if (index != -1)
            {
                return m_keyValues[index].Value;
            }

            return null;
        }

        public bool HasKey(string key)
        {
            return FindIndexWithKey(key) != -1;
        }

        public bool RemoveData(string key)
        {
            int     index   = FindIndexWithKey(key);
            if (index != -1)
            {
                m_keyValues.RemoveAt(index);
                return true;
            }

            return false;
        }

        public string GetSnapshot()
        {
            IDictionary dictionary = new Dictionary<string, object>();
            foreach (StringKeyValuePair keyValue in m_keyValues)
            {
                dictionary.Add(key: keyValue.Key, value: keyValue.Value);
            }

            return ExternalServiceProvider.JsonServiceProvider.ToJson(dictionary);
        }

        #endregion

        #region Private methods

        private int FindIndexWithKey(string key)
        {
            return m_keyValues.FindIndex((item) => string.Equals(key, item.Key));
        }

        #endregion
    }
}