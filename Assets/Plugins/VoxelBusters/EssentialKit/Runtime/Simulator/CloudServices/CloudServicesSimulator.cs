using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor.NativePlugins;

namespace VoxelBusters.EssentialKit.CloudServicesCore.Simulator
{
    public sealed class CloudServicesSimulator : SingletonObject<CloudServicesSimulator>
    {
        #region Fields

        private         CloudServicesSimulatorData      m_simulatorData     = null;

        #endregion

        #region Constructors

        private CloudServicesSimulator()
        { }

        #endregion

        #region Private methods

        private void EnsureInitialised()
        {
            if (m_simulatorData != null) return;

            // Set properties
            m_simulatorData     = LoadFromDisk() ?? new CloudServicesSimulatorData();
        }

        #endregion

        #region Database methods

        private CloudServicesSimulatorData LoadFromDisk()
        {
            return SimulatorServices.GetObject<CloudServicesSimulatorData>(NativeFeatureType.kCloudServices);
        }

        private void SaveData()
        {
            SimulatorServices.SetObject(NativeFeatureType.kCloudServices, m_simulatorData);
        }

        public static void Reset() 
        {
            SimulatorServices.RemoveObject(NativeFeatureType.kCloudServices);
        }

        #endregion

        #region Public methods

        public void AddData(string key, string value)
        {
            EnsureInitialised();
            m_simulatorData.AddData(key, value);
            SaveData();
        }

        public string GetData(string key)
        {
            EnsureInitialised();
            return m_simulatorData.GetData(key);
        }

        public bool HasKey(string key)
        {
            EnsureInitialised();
            return m_simulatorData.HasKey(key);
        }

        public bool RemoveData(string key)
        {
            EnsureInitialised();
            return m_simulatorData.RemoveData(key);
        }

        public string GetSnapshot()
        {
            EnsureInitialised();
            return m_simulatorData?.GetSnapshot();
        }

        public void Synchronize()
        {
            EnsureInitialised();
            SaveData();
        }

        #endregion
    }
}