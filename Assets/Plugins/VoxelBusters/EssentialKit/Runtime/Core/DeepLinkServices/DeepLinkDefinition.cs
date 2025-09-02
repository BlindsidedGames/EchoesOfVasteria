using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents definition of deep link which can be used to configure deep link services.
    /// </summary>
    /// @ingroup DeepLinkServices
    [Serializable]
    public class DeepLinkDefinition
    {
        #region Fields
        
        [SerializeField, DefaultValue("identifier")]
        private     string                      m_identifier;

        [SerializeField, DefaultValue("applinks")]
        private     string                      m_serviceType;

        [SerializeField]
        private     string                      m_scheme;

        [SerializeField]
        private     string                      m_host;

        [SerializeField]
        private     string                      m_path;

        #endregion

        #region Properties
        /// <summary>
        /// Gets the identifier for the deep link.
        /// </summary>
        public string Identifier => PropertyHelper.GetValueOrDefault(
            instance: this,
            fieldAccess: (field) => field.m_identifier,
            value: m_identifier);

        /// <summary>
        /// Gets the service type for the deep link.
        /// </summary>
        public string ServiceType => PropertyHelper.GetValueOrDefault(
            instance: this,
            fieldAccess: (field) => field.m_serviceType,
            value: m_serviceType);

        /// <summary>
        /// Gets the scheme for the deep link.
        /// </summary>
        public string Scheme => PropertyHelper.GetValueOrDefault(m_scheme);

        /// <summary>
        /// Gets the host for the deep link.
        /// </summary>
        public string Host => PropertyHelper.GetValueOrDefault(m_host);

        /// <summary>
        /// Gets the path for the deep link.
        /// </summary>
        public string Path => PropertyHelper.GetValueOrDefault(m_path);

        #endregion

        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DeepLinkDefinition"/> class.
        /// </summary>
        /// <param name="identifier">The identifier for the deep link.</param>
        /// <param name="serviceType">The service type for the deep link.</param>
        /// <param name="scheme">The scheme for the deep link.</param>
        /// <param name="host">The host for the deep link.</param>
        /// <param name="path">The path for the deep link.</param>
        public DeepLinkDefinition(string identifier = null, string serviceType = null,
            string scheme = null, string host = null,
            string path = null)
        {
            // set properties
            m_identifier    = PropertyHelper.GetValueOrDefault(
                instance: this,
                fieldAccess: (field) => field.m_identifier,
                value: identifier);
            m_serviceType   = PropertyHelper.GetValueOrDefault(
                instance: this,
                fieldAccess: (field) => field.m_serviceType,
                value: serviceType);
            m_scheme        = scheme;
            m_host          = host;
            m_path          = path;
        }

        #endregion
    }
}