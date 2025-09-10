using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// The state of a billing product payment.
    /// </summary>
    public enum BillingTransactionState
    {
        Unknown,
        /// <summary> Transaction is being added to the server queue. </summary>
        Purchasing,

        /// <summary> Transaction is in queue, user has been charged. </summary>
        Purchased,

        /// <summary> Transaction was cancelled or failed before being added to the server queue. </summary>
        Failed,

        /// <summary> This transaction restores content previously purchased by the user. </summary>
        [Obsolete("This state is deprecated. Instead, just use OnRestorePurchasesComplete event with Purchased status to identify restored/past purchases.", true)] //Obsolete:2024
        Restored,

        /// <summary> The transaction is in the queue, but its final status is pending external action. </summary>
        Deferred,

        /// <summary> This transaction was refunded back to the user. You can restrict/remove associated item. </summary>
        Refunded,
    }
}