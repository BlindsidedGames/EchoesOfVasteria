using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [System.Serializable]
    /// <summary>
    /// Represents payout information associated with the product.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductPayoutDefinition
    {
        #region Fields

        [SerializeField]
        private     BillingProductPayoutCategory    m_category;

        [SerializeField]
        private     string                      m_variant;

        [SerializeField]
        private     double                      m_quantity;

        [SerializeField]
        private     string                      m_data;

        [SerializeField]
        private     string                      m_description;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the category of the payout.
        /// </summary>
        public BillingProductPayoutCategory Category => m_category;

        /// <summary>
        /// Gets the variant of the payout.
        /// </summary>
        public string Variant => PropertyHelper.GetValueOrDefault(m_variant);

        /// <summary>
        /// Gets the quantity of the payout.
        /// </summary>
        public double Quantity => m_quantity;

        /// <summary>
        /// Gets the additional data associated with the payout.
        /// </summary>
        public string Data => PropertyHelper.GetValueOrDefault(m_data);

        /// <summary>
        /// Gets the description of the payout.
        /// </summary>
        public string Description => PropertyHelper.GetValueOrDefault(m_description);

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingProductPayoutDefinition"/> class.
        /// </summary>
        /// <param name="payoutType">The category of the payout.</param>
        /// <param name="subtype">The variant of the payout.</param>
        /// <param name="quantity">The quantity of the payout.</param>
        /// <param name="data">The additional data associated with the payout.</param>
        /// <param name="description">The description of the payout.</param>
        public BillingProductPayoutDefinition(BillingProductPayoutCategory payoutType, string subtype = null,
            double quantity = 1, string data = null, string description = null)
        {
            // Set properties
            m_category    = payoutType;
            m_variant       = subtype;
            m_quantity      = quantity;
            m_data          = data;
            m_description   = description;
        }

        #endregion
        
        #region Base methods

        public override string ToString() 
        {
            var     sb  = new StringBuilder(128);
            sb.Append("BillingProductPayoutDefinition { ")
                .Append($"Category: {Category} ")
                .Append($"Variant: {Variant} ")
                .Append($"Quantity: {Quantity} ")
                .Append($"Data: {Data} ")
                .Append($"Description: {Description}")
                .Append("}");
            return sb.ToString();
        }

        #endregion
    }
}