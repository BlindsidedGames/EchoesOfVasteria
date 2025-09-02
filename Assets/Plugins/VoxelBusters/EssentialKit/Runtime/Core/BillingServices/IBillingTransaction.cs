using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Provides an interface to access transaction information of the purchased product.
    /// </summary>
    /// @ingroup BillingServices
    public interface IBillingTransaction
    {
        #region Properties

        /// <summary>
        /// The string that uniquely identifies a payment transaction. (read-only)
        /// </summary>
        string Id { get; }

        /// <summary>  [Deprecated]Returns the payment request associated with this transaction.
        /// </summary>
        /// @deprecated since v3.0.0    
        [Obsolete("This property is deprecated. Use the properties available in IBillingPayment interface", false)] //Obsolete:2024
        IBillingPayment Payment { get; }

        /// <summary>
        /// The product associated with this transaction. 
        /// </summary>
        /// @warning
        /// Check if the product is null before accessing the properties of this object. It's possible in future, you may delete a product and get its transaction details. 
        /// @note
        /// To avoid having null, you can mark a product inactive in our settings so that you can still get the details of this product.
        IBillingProduct Product { get; }

        /// <summary>
        /// The number of units to be purchased. This should be a non-zero number.
        /// </summary>
        int RequestedQuantity { get; }

        /// <summary>
        /// An optional information provided by the developer at the time of calling BuyProduct provided via BuyProductOptions.
        /// This can be used to identify a user or anything specific to the product purchase.
        /// </summary>
        /// @note This must be UUID v4 format identifier and can be generated with Guid.NewGuid().
        string Tag { get; }

        /// <summary>
        /// The UTC date and time, when user initiated this transaction.
        /// </summary>
        DateTime DateUTC { get; }

        /// <summary>
        /// The local date and time, when user initiated this transaction.
        /// </summary>
        DateTime Date { get; }

        /// <summary>
        /// The current state of the transaction. (read-only)
        /// </summary>
        BillingTransactionState TransactionState { get; }

        /// <summary>
        /// The current state of the validation.
        /// </summary>
        BillingReceiptVerificationState ReceiptVerificationState { get; set; }

        /// <summary>
        /// The receipt associated with this transaction.
        /// @note This is same as PurchaseToken on Android and on iOS this will be null (as starting from StoreKit2, server side validation isn't required and verifyReceipt endpoint is deprecated.)
        /// </summary>
        string Receipt {get; }

        /// <summary>Gets the billing environment.</summary>
        /// <returns>The billing environment.</returns>
        BillingEnvironment Environment { get; }

        /// <summary>
        /// Gets the quantity of the product that the user has purchased.
        /// </summary>
        int PurchasedQuantity { get; }

        /// <summary>
        /// Gets the revocation info if this product is revoked
        /// </summary>
        BillingProductRevocationInfo RevocationInfo { get; } 
        
        /// <summary>
        /// Gets the subscription status of a billing product.
        /// </summary>
        /// <returns>The subscription status of the billing product.</returns>
        BillingProductSubscriptionStatus SubscriptionStatus { get; }

        /// <summary>
        /// Json string representation of a dictionary containing transaction (iOS & Android) and signature (Android only) data  . This can be used for accessing platform specific details.
        /// </summary>
        /// @remark
        /// This contains transaction details with "transaction" key identifier. 
        /// On Android, additionally it contains "signature" key identifier.
        string RawData { get; }

        /// <summary>
        /// An object describing the error that occurred while processing the transaction. (read-only)
        /// </summary>
        Error Error { get; }

        /// <summary>
        /// Android specific properties useful for receipt validation on server. (read-only)
        /// </summary>
        [Obsolete("This property is obsolete. Use data from RawData properties", true)]
        BillingTransactionAndroidProperties AndroidProperties { get; }

        #endregion
    }
}