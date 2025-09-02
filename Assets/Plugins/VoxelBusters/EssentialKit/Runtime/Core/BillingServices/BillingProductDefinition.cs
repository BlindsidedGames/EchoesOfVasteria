using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents an object containing information related to billing product definition. This contains the data you set in settings.
    /// </summary>
    /// @ingroup BillingServices
    [Serializable]
    public class BillingProductDefinition
    {
        #region Fields

        [SerializeField]
        private     string                              m_id;

        [SerializeField]
        private     BillingProductType                  m_productType;

        [SerializeField]
        private     string                              m_title;

        [SerializeField]
        private     string                              m_description;

        [SerializeField]
        private     bool                                m_isInactive = false;

        [SerializeField]
        private     BillingProductPayoutDefinition[]    m_payouts;

        [Header("Platform Specific")]
        [SerializeField]
        private string m_platformId;

        [SerializeField]
        private RuntimePlatformConstantSet m_platformIdOverrides;

        #endregion

        #region Properties

        /// <summary>
        /// The string that identifies the product within Unity environment. (read-only)
        /// </summary>
        public string Id => PropertyHelper.GetValueOrDefault(m_id);

        /// <summary>
        /// The type of the product. (read-only)
        /// </summary>
        public BillingProductType ProductType => m_productType;

        /// <summary>
        /// The name of the product. (read-only)
        /// </summary>
        public string Title => PropertyHelper.GetValueOrDefault(m_title);

        /// <summary>
        /// The description of the product. (read-only)
        /// </summary>
        public string Description => PropertyHelper.GetValueOrDefault(m_description);


        /// <summary>
        /// Indicates whether this product is inactive. A product is considered active if it is considered for purchase.
        /// Note: If your earlier versions had a product and is not used any more, you can just enable this flag and still get the product details of past purchases.
        /// </summary>
        public bool IsInactive => m_isInactive;

        /// <summary>
        /// The payout information. (read-only)
        /// </summary>
        public BillingProductPayoutDefinition[] Payouts => m_payouts;

        /// <summary>
        /// Additional information associated with this product. This information is provided by the developer.
        /// </summary>
        [System.Obsolete("This property is deprecated. Use Payouts instead.", true)]
        public object Tag { get; set; }

        #endregion

        #region Create methods

        /// <summary>
        /// Creates the product settings object.
        /// </summary>
        /// <param name="id">The string that identifies the product within Unity environment.</param>
        /// <param name="platformId">The identifier of product in the store. This is the identifier supplied by the native platform store(Google play console / App Store Connect).</param>
        /// <param name="platformIdOverrides">The overrides for the <see cref="platformId"/>.</param>
        /// <param name="productType">The type of the product. Default is <see cref="BillingProductType.Consumable"/>.</param>
        /// <param name="title">The name of the product.</param>
        /// <param name="description">The description of the product.</param>
        /// <param name="isInactive">Indicates whether this product is inactive. A product is considered active if it is considered for purchase.</param>
        /// <param name="payouts">Payout information associated with this product.</param>
        public BillingProductDefinition(string id = null, string platformId = null,
            RuntimePlatformConstantSet platformIdOverrides = null, BillingProductType productType = BillingProductType.Consumable,
            string title = null, string description = null,
            bool isInactive = false,
            BillingProductPayoutDefinition[] payouts = null)
        {
            // Set properties
            m_id                    = id;
            m_platformId            = platformId?.Trim();
            m_platformIdOverrides   = platformIdOverrides ?? new RuntimePlatformConstantSet();
            m_productType           = productType;
            m_title                 = title;
            m_description           = description;
            m_isInactive            = isInactive;
            m_payouts               = payouts ?? new BillingProductPayoutDefinition[0];
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Returns the store identifier for active platform.
        /// </summary>
        public string GetPlatformIdForActivePlatform()
        {
            return m_platformIdOverrides.GetConstantForActiveOrSimulationPlatform(m_platformId)?.Trim();
        }

        #endregion
    }
}