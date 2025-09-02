using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using UnityEngine.Serialization;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    /// <summary>
    /// The BillingServicesUnitySettings class is used to configure the Billing module of Essential Kit.
    /// </summary>
    public partial class BillingServicesUnitySettings : SettingsPropertyGroup
    {
        #region Fields

        [SerializeField, FormerlySerializedAs("m_billingProductMetaArray")]
        [Tooltip("Array contains information of the products used in the app.")]
        private     BillingProductDefinition[]  m_products;

        [SerializeField]
        [Tooltip("If enabled, completed transactions are removed from queue automatically. Else, you need to call FinishTransactions method manually. This is usually set to off if you have external verification system.")]
        private     bool                        m_autoFinishTransactions;

        [SerializeField]
        [Header("Platform specific")]
        [Tooltip("Android specific properties.")]
        private     AndroidPlatformProperties   m_androidProperties;

        #endregion

        #region Properties

        /// <summary>
        /// Gets an array of product definitions.
        /// </summary>
        /// <value>Array of product definitions.</value>
        public BillingProductDefinition[] Products => m_products;

        /// <summary>
        /// Gets a value indicating whether completed transactions are finished automatically.
        /// </summary>
        /// <value>
        ///   <c>true</c> if completed transactions are finished automatically; otherwise, <c>false</c>.
        /// </value>
        public bool AutoFinishTransactions => m_autoFinishTransactions;

        /// <summary>
        /// Gets the Android specific properties.
        /// </summary>
        /// <value>The Android properties.</value>
        public AndroidPlatformProperties AndroidProperties => m_androidProperties;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingServicesUnitySettings"/> class with the specified settings.
        /// </summary>
        /// <param name="isEnabled">if set to <c>true</c> [is enabled].</param>
        /// <param name="products">Array of product definitions.</param>
        /// <param name="autoFinishTransactions">if set to <c>true</c> completed transactions are finished automatically.</param>
        /// <param name="androidProperties">Android specific properties.</param>
        public BillingServicesUnitySettings(bool isEnabled = true, BillingProductDefinition[] products = null, 
                                       bool autoFinishTransactions = true, 
                                       AndroidPlatformProperties androidProperties = null)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kBillingServices)
        {
            // set properties
            m_products                      = products ?? new BillingProductDefinition[0];
            m_autoFinishTransactions        = autoFinishTransactions;
            m_androidProperties             = androidProperties ?? new AndroidPlatformProperties();
        }

        #endregion
    }
}