using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Utility class for handling cloud services related operations.
    /// </summary>
    /// @ingroup CloudServices
    public static class CloudServicesUtility
    {
        /// <summary>
        /// Gets the cloud and local cache value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">Specify the type of the value stored in cloud.</typeparam>
        /// <param name="key"> string used to identify the value stored in the cloud data store.</param>
        /// <param name="cloudValue">The value available in Cloud storage.</param>
        /// <param name="localCacheValue">The value available in local cache.</param>
        /// <param name="localCacheDefaultValue">The default value to be used when specified key doesn't exist in local cache.</param>
        /// <returns></returns>
        public static bool TryGetCloudAndLocalCacheValues<T>(string key, out T cloudValue, out T localCacheValue, T localCacheDefaultValue = default)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    cloudValue          = (T)(object)CloudServices.GetBool(key);
                    localCacheValue     = (T)(object)CloudServices.LocalCache.GetBool(key, (bool)(object)localCacheDefaultValue);
                    return true;

                case TypeCode.Int32:
                    cloudValue          = (T)(object)CloudServices.GetInt(key);
                    localCacheValue     = (T)(object)CloudServices.LocalCache.GetLong(key, (long)(object)localCacheDefaultValue);
                    return true;

                case TypeCode.Int64:
                    cloudValue          = (T)(object)CloudServices.GetLong(key);
                    localCacheValue     = (T)(object)CloudServices.LocalCache.GetLong(key, (long)(object)localCacheDefaultValue);
                    return true;

                case TypeCode.Single:
                    cloudValue          = (T)(object)CloudServices.GetFloat(key);
                    localCacheValue     = (T)(object)CloudServices.LocalCache.GetDouble(key, (double)(object)localCacheDefaultValue);
                    return true;

                case TypeCode.Double:
                    cloudValue          = (T)(object)CloudServices.GetDouble(key);
                    localCacheValue     = (T)(object)CloudServices.LocalCache.GetDouble(key, (double)(object)localCacheDefaultValue);
                    return true;

                case TypeCode.String:
                    cloudValue          = (T)(object)CloudServices.GetString(key);
                    localCacheValue     = (T)(object)CloudServices.LocalCache.GetString(key, (string)(object)localCacheDefaultValue);
                    return true;

                case TypeCode.Object:
                    if (typeof(T) == typeof(byte[]))
                    {
                        cloudValue      = (T)(object)CloudServices.GetByteArray(key);
                        localCacheValue = (T)(object)CloudServices.LocalCache.GetByteArray(key, (byte[])(object)localCacheDefaultValue);
                        return true;
                    }
                    else
                    {
                        cloudValue      = default;
                        localCacheValue = default;
                        return false;
                    }

                default:
                    cloudValue          = default;
                    localCacheValue     = default;
                    return false;
            }
        }
    }
}