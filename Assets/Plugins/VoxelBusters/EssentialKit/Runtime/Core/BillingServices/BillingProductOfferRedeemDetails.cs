namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents details of a product offer to be redeemed.
    /// </summary>
    /// <example>
    /// The following code snippet demonstrates how to create an instance of <see cref="BillingProductOfferRedeemDetails"/>:
    /// <code>
    /// var offerRedeemDetails = new BillingProductOfferRedeemDetails.Builder()
    ///     .SetIosPlatformProperties("offerId", "keyId", "nonce", "signature", 123456)
    ///     .SetAndroidPlatformProperties("offerId")
    ///     .Build();
    /// </code>
    /// @remark This class considers only properties relavent to the platform. So you can just set the properties for iOS or Android at once or with dummy values for other platforms.
    /// </example>
    /// @ingroup BillingServices
    public class BillingProductOfferRedeemDetails
    {
        /// <summary>
        /// Represents the iOS platform properties for redeeming a product offer.
        /// </summary>
        public IosProperties IosPlatformProperties { get; private set; }

        /// <summary>
        /// Represents the Android platform properties for redeeming a product offer.
        /// </summary>
        public AndroidProperties AndroidPlatformProperties { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingProductOfferRedeemDetails"/> class.
        /// </summary>
        private BillingProductOfferRedeemDetails()
        {
            // Initialize the properties
            IosPlatformProperties = null;
            AndroidPlatformProperties = null;
        }


        /// <summary>
        /// Represents the iOS platform properties for redeeming a product offer.
        /// </summary>
        public class IosProperties
        {
            /**
            * The id of the product offer
            */
            public string OfferId { get; private set; }

            /**
            * A string that identifies the private key you use to generate the signature
            */
            public string KeyId { get; private set; }

            /**
            * The anti-replay value used in the signature. Use lowercase.
            */
            public string Nonce { get; private set; }

            /**
            * The cryptographic signature of the offer parameters, which you generate on your server.
             *\note The signature need to be a Base64-encoded string.
            */
            public string Signature {get; private set;}

            /**
            * The UNIX time, in milliseconds, when you generate the signature.
            */
            public long Timestamp {get; private set;}

            /**
            * Initializes a new instance of the <see cref="IosProperties"/> class.
            *
            * @param offerId of the product offer.
            * @param keyId A string that identifies the private key you use to generate the signature.
            * @param nonce The anti-replay value used in the signature.
            * @param signature The cryptographic signature of the offer parameters.
            * @param timestamp The UNIX time, in milliseconds, when you generate the signature.
            * \note The signature need to be a Base64-encoded string.
            */
            internal IosProperties(string offerId, string keyId, string nonce, string signature, long timestamp)
            {
                OfferId = offerId;
                KeyId = keyId;
                Nonce = nonce;
                Signature = signature;
                Timestamp = timestamp;
            }

            public override string ToString()
            {
                return string.Format("[OfferId={0}, KeyId={1}, Nonce={2}, Signature={3}, Timestamp={4}]", OfferId, KeyId, Nonce, Signature, Timestamp);
            }
        }

        /// <summary>
        /// Represents the Android platform properties for redeeming a product offer.
        /// </summary>
        public class AndroidProperties
        {
            /// <summary>
            /// The offer Id of the product offer.
            /// </summary>
            public string OfferId { get; private set; }
           
            /// <summary>
            /// Initializes a new instance of the <see cref="AndroidProperties"/> class.
            /// </summary>
            /// <param name="offerId">The offer ID of the product offer.</param>
            internal AndroidProperties(string offerId)
            {
                OfferId = offerId;
            }

            public override string ToString()
            {
                return string.Format("[OfferId={0}]", OfferId);
            }
        }

        /// <summary>
        /// Represents a builder class for <see cref="BillingProductOfferRedeemDetails"/>.
        /// </summary>
        public class Builder
        {

            private BillingProductOfferRedeemDetails m_request;

            public Builder()
            {
                m_request = new BillingProductOfferRedeemDetails();
            }

            /// <summary>
            /// Sets the iOS platform properties for the <see cref="BillingProductOfferRedeemDetails"/> being built.
            /// </summary>
            /// <param name="offerId">The offer id of the product offer.</param>
            /// <param name="keyId">keyId A string that identifies the private key you use to generate the signature.</param>
            /// <param name="nonce">The anti-replay value used in the signature.</param>
            /// <param name="signature">The cryptographic signature of the offer parameters (base64).</param>
            /// <param name="timestamp">The UNIX time, in milliseconds, when you generate the signature.</param>
            /// \note The signature need to be a Base64-encoded string.
            /// <returns>The current Builder instance.</returns>
            public Builder SetIosPlatformProperties(string offerId, string keyId, string nonce, string signature, long timestamp)
            {
                m_request.IosPlatformProperties = new IosProperties(offerId, keyId, nonce, signature, timestamp);
                return this;
            }

            /// <summary>
            /// Sets the Android platform properties for the <see cref="BillingProductOfferRedeemDetails"/> being built.
            /// </summary>
            /// <param name="offerId">The offer Id of the product offer.</param>
            /// <returns>The current Builder instance.</returns>
            public Builder SetAndroidPlatformProperties(string offerId)
            {
                m_request.AndroidPlatformProperties = new AndroidProperties(offerId);
                return this;
            }

            /// <summary>
            // Builds a BillingProductOfferRedeemRequest instance.
            /// </summary>
            public BillingProductOfferRedeemDetails Build()
            {
                return m_request;
            }   
        }

        public override string ToString()
        {
            return string.Format("[IosPlatformProperties={0}, AndroidPlatformProperties={1}]", IosPlatformProperties, AndroidPlatformProperties);
        }
    }
}