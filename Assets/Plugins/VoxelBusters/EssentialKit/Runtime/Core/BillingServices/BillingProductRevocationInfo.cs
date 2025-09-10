using System;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents information about the revocation of a billing product.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductRevocationInfo
    {
        /// <summary>
        /// Gets the revocation date of the object, if available.
        /// </summary>
        /// <value>
        /// The revocation date of the object, or null if not available.
        /// </value>
        public DateTime Date { get; }

        /// <summary>
        /// Gets the UTC date and time of revocation, if applicable.
        /// </summary>
        /// <value>The UTC date and time of revocation, or null if not applicable.</value>
        public DateTime DateUTC  { get; }

        /// <summary>
        /// Gets the reason for revocation of a billing product.
        /// </summary>
        /// <value>The reason for revocation, if available; otherwise, null.</value>
        public BillingProductRevocationReason Reason { get; }


        public BillingProductRevocationInfo(DateTime dateUTC, BillingProductRevocationReason reason)
        {
            Date = dateUTC.ToLocalTime();;
            DateUTC = dateUTC;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"[BillingProductRevocationInfo: Date={Date}, DateUTC={DateUTC}, Reason={Reason}]";
        }
    }
}