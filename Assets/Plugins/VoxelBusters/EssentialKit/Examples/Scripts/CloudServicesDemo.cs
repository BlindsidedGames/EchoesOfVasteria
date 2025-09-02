using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
// key namespaces
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit;
// internal namespace
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
    public class CloudServicesDemo : DemoActionPanelBase<CloudServicesDemoAction, CloudServicesDemoActionType>
    {
        #region Fields

        [SerializeField]
        private     InputField      m_keyField          = null;

        [SerializeField]
        private     InputField      m_valueField        = null;

        #endregion

        #region Base class methods

        protected override void OnEnable()
        {
            base.OnEnable();

            // register for events
            CloudServices.OnUserChange              += OnUserChange;
            CloudServices.OnSavedDataChange         += OnSavedDataChange;
            CloudServices.OnSynchronizeComplete     += OnSynchronizeComplete;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // unregister from events
            CloudServices.OnUserChange              -= OnUserChange;
            CloudServices.OnSavedDataChange         -= OnSavedDataChange;
            CloudServices.OnSynchronizeComplete     -= OnSynchronizeComplete;
        }

        protected override void OnActionSelectInternal(CloudServicesDemoAction selectedAction)
        {
            string key  = GetKey();

            switch (selectedAction.ActionType)
            {
                case CloudServicesDemoActionType.GetBool:
                    bool    result1     = CloudServices.GetBool(key);
                    Log("Saved bool value: " + result1);
                    break;

                case CloudServicesDemoActionType.SetBool:
                    CloudServices.SetBool(key, GetInputValueAsBool());
                    Log("Bool value added.");
                    break;

                case CloudServicesDemoActionType.GetLong:
                    long    result2     = CloudServices.GetLong(key);
                    Log("Saved long value: " + result2);
                    break;

                case CloudServicesDemoActionType.SetLong:
                    CloudServices.SetLong(key, GetInputValueAsLong());
                    Log("Long value added.");
                    break;

                case CloudServicesDemoActionType.GetDouble:
                    double  result3     = CloudServices.GetDouble(key);
                    Log("Saved double value: " + result3);
                    break;

                case CloudServicesDemoActionType.SetDouble:
                    CloudServices.SetDouble(key, GetInputValueAsDouble());
                    Log("Double value added.");
                    break;

                case CloudServicesDemoActionType.GetString:
                    string  result4     = CloudServices.GetString(key);
                    if (result4 == null)
                    {
                        Log("Null value");

                        string test = null;
                        Log("Test : " + test);
                    }

                    Log("Saved string value: " + result4);
                    break;

                case CloudServicesDemoActionType.SetString:
                    CloudServices.SetString(key, GetInputValueAsString());
                    Log("String value added.");
                    break;

                case CloudServicesDemoActionType.GetByteArray:
                    byte[] result5 = CloudServices.GetByteArray(key);
                    Log("Fetched byte array value (string representation) = " + ((result5 != null) ? Encoding.UTF8.GetString(result5) : "null"));// String representation: Encoding.UTF8.GetString(result5)
                    break;

                case CloudServicesDemoActionType.SetByteArray:
                    CloudServices.SetByteArray(key, GetInputValueAsByteArray());
                    Log("Byte Array value added.");
                    break;

                case CloudServicesDemoActionType.Synchronize:
                    CloudServices.Synchronize();
                    break;

                case CloudServicesDemoActionType.HasKey:
                    bool hasKey = CloudServices.HasKey(key);
                    Log($"Has {key} key: {hasKey}");
                    break;

                case CloudServicesDemoActionType.RemoveKey:
                    CloudServices.RemoveKey(key);
                    Log("Removed key: " + key);
                    break;
                
                case CloudServicesDemoActionType.GetSnapshot:
                    Log("Received snapshot data");

                    IDictionary snapshot = CloudServices.GetSnapshot();
                    foreach (var eachKey in snapshot.Keys)
                    {
                            string keyString = eachKey.ToString();
                            string valueString = snapshot[keyString]?.ToString();
                            
                            Log($"[{keyString} : {valueString}]");
                    }
                    break;

                case CloudServicesDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kCloudServices);
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Plugin event methods

        private void OnUserChange(CloudServicesUserChangeResult result, Error error)
        {
            var     user    = result.User;
            Log("Received user change callback.");
            Log("User id: " + user.UserId);
            Log("User status: " + user.AccountStatus);
        }

        private void OnSavedDataChange(CloudServicesSavedDataChangeResult result)
        {
            var     changedKeys = result.ChangedKeys;
            Log("Received saved data change callback.");
            Log("Reason: " + result.ChangeReason);
            Log("Total changed keys: " + changedKeys.Length);
            Log("Here are the changed keys:");
            for (int iter = 0; iter < changedKeys.Length; iter++)
            {
                //Fetching local and cloud values for value type which is string in this example.
                string cloudValue;
                string localCacheValue;
                CloudServicesUtility.TryGetCloudAndLocalCacheValues(changedKeys[iter], out cloudValue, out localCacheValue, "default");

                Log(string.Format("[{0}]: {1}  [Cloud Value] : {2} [Local Cache Value] : {3}  ", iter, changedKeys[iter], cloudValue, localCacheValue));
            }
        }

        private void OnSynchronizeComplete(CloudServicesSynchronizeResult result)
        {
            string snapshotJson = ExternalServiceProvider.JsonServiceProvider.ToJson(CloudServices.GetSnapshot());
            Log($"Received synchronize finish callback.");
            Log("Status: " + result.Success);
            Log("Snapshot data: " + snapshotJson);
        }

        #endregion

        #region Private methods

        private string GetKey()
        {
            string  key = m_keyField.text;
            return string.IsNullOrEmpty(key) ? null : m_keyField.text;
        }

        private bool GetInputValueAsBool()
        {
            try
            {
                return Convert.ToBoolean(m_valueField.text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log("Adding default value: false");
            }

            return false;
        }

        private long GetInputValueAsLong()
        {
            try
            {
                return Convert.ToInt64(m_valueField.text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log("Adding default value: 0");
            }

            return 0;
        }

        private double GetInputValueAsDouble()
        {
            try
            {
                return Convert.ToDouble(m_valueField.text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log("Adding default value: 0.0");
            }

            return 0.0;
        }

        private string GetInputValueAsString()
        {
            return m_valueField.text;
        }

        private byte[] GetInputValueAsByteArray()
        {
            return Encoding.UTF8.GetBytes(m_valueField.text);
        }

        #endregion
    }
}
