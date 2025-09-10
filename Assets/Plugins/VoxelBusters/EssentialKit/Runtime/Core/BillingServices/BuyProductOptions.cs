using System;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents options for purchasing a <see cref="IBillingProduct"/>.
    /// </summary>
    /// <remarks>
    /// This class provides a fluent API for setting up options for a purchase request.
    /// </remarks>
    /// <example>
    /// The following code demonstrates how to create an instance of <see cref="BuyProductOptions"/> with offer redeem details:
    /// <code>
    /// var offerRedeemDetails = new BillingProductOfferRedeemDetails.Builder()
    ///             .SetIosPlatformProperties("offerId", "keyId", "nonce", "signature", 123456)
    ///             .SetAndroidPlatformProperties("offerId")
    ///             .Build();
    /// var options = new BuyProductOptions.Builder()
    ///     .SetTag("your-uuid-v4-tag")
    ///     .SetQuantity(2)
    ///     .SetOfferRedeemDetails(offerRedeemDetails)
    ///     .Build();
    /// </code>
    /// </example>
    /// @ingroup BillingServices
    public sealed class BuyProductOptions
    {
        /// <summary>
        /// Gets the default <see cref="BuyProductOptions"/> instance.
        /// </summary>
        /// <remarks>
        /// Default options have quantity set to 1 and no custom tag associated.
        /// </remarks>
        public static BuyProductOptions Default { get; } = new BuyProductOptions();

        /// <summary>
        /// Gets or sets a custom tag to be passed along with the purchase request.
        /// </summary>
        /// <value>The custom tag associated with the purchase request. Default value is <c>null</c>.</value>
        /// @attention
        /// The tag should be a valid UUID v4 formatted string.
        /// @remark
        /// This can be used as a filter to identify the purchase requests related to a specific user.
        public string Tag { get; private set; } = null;

        /// <summary>
        /// Gets or sets the quantity to be purchased.
        /// </summary>
        /// <value>The quantity to be purchased. Default value is 1.</value>
        /// <remarks>
        /// Must be greater than 0.
        /// </remarks>
        public int Quantity { get; private set; } = 1;

        /// <summary>
        /// Gets or sets the offer redeem details for the product offer.
        /// </summary>
        /// <value>The offer redeem details for the product offer. Default value is <c>null</c>.</value>
        /// <remarks>
        /// Use this property to provide the details necessary for redeeming a product offer.
        /// </remarks>
        public BillingProductOfferRedeemDetails OfferRedeemDetails { get; private set; } = null;


        private BuyProductOptions() {}


        public override string ToString()
        {
            return string.Format("[Tag={0}, Quantity={1}, OfferRedeemDetails={2}]", Tag, Quantity, OfferRedeemDetails);
        }



        /// <summary>
        /// Builder class for <see cref="BuyProductOptions"/>.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// Options being built.
            /// </summary>
            private BuyProductOptions m_options;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            public Builder()
            {
                m_options = new BuyProductOptions();
            }

            /// <summary>
            /// Sets a custom tag to be passed along with the purchase request.
            /// </summary>
            /// <param name="tag">Tag to be associated with the purchase.</param>
            /// <returns>Current Builder instance.</returns>
            public Builder SetTag(string tag)
            {
                var isValid = Guid.TryParse(tag, out _);
                if(isValid)
                {
                    m_options.Tag = tag;
                }
                else
                {
                    throw new ArgumentException("Tag must be a valid UUID v4 formatted string.");
                }
                
                return this;
            }

            /// <summary>
            /// Sets the quantity to be purchased.
            /// </summary>
            /// <param name="quantity">Quantity to be purchased. Must be greater than 0.</param>
            /// <returns>Current Builder instance.</returns>
            public Builder SetQuantity(int quantity)
            {
                m_options.Quantity = quantity;
                return this;
            }


            /// <summary>
            /// Sets the offer redeem details for the product offer.
            /// </summary>
            /// <param name="offerRedeemDetails">Offer redeem details for the product offer.</param>
            /// <returns>Current Builder instance.</returns>
            public Builder SetOfferRedeemDetails(BillingProductOfferRedeemDetails offerRedeemDetails)
            {
                m_options.OfferRedeemDetails = offerRedeemDetails;
                return this;
            }

            /// <summary>
            /// Creates the <see cref="BuyProductOptions"/> instance.
            /// </summary>
            /// <returns>
            /// Instance of <see cref="BuyProductOptions"/>.
            /// </returns>
            /// <exception cref="VBException">
            /// Thrown if <see cref="Quantity"/> is less than 1.
            /// </exception>
            public BuyProductOptions Build()
            {
                // Ensure that quantity is greater than 0
                if (m_options.Quantity <= 0)
                {
                    throw new VBException("Quantity can't be less than 1.");
                }

                return m_options;
            }

        }
    }
}