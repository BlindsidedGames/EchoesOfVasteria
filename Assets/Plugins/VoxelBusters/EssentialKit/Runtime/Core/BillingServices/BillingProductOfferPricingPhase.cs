
namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents a phase of pricing for a billing product offer.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductOfferPricingPhase
    {
        /// <summary>
        /// Gets the payment mode for the offer.
        /// </summary>
        public BillingProductOfferPaymentMode PaymentMode { get; private set; }

        /// <summary>
        /// Gets the offer price charged for the specified period.
        /// </summary>
        public BillingPrice Price { get; private set; }

        /// <summary>
        /// Gets the period for which the offer price is applied. 
        /// For PayAsYouGo, the period value will be the billing cycle period, and the total period will be Period * RepeatCount.
        /// </summary>
        public BillingPeriod Period { get; private set; }

        /// <summary>
        /// Gets the number of times the pricing phase repeats.
        /// <remarks>
        /// For <see cref="BillingProductOfferPaymentMode.PayAsYouGo"/>, the period value will be the billing cycle period, and the total period will be Period * RepeatCount.
        /// For other payment modes, this value will be always 1.
        /// </remarks>
        /// </summary>
        public int RepeatCount { get; private set; }

        public BillingProductOfferPricingPhase(BillingProductOfferPaymentMode paymentMode, BillingPrice price, BillingPeriod period, int repeatCount)
        {
            PaymentMode = paymentMode;
            Price = price;
            Period = period;
            RepeatCount = repeatCount;
        }

        public override string ToString()
        {
            return string.Format("[PaymentMode={0}, Price={1}, Period={2}, RepeatCount={3}]", PaymentMode, Price, Period, RepeatCount);
        }
    }
}