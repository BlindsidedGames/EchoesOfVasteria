using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Reason for which the product got revoked. 
    /// </summary>
    public enum BillingProductRevocationReason
    {
        /// <summary>
        /// No revocation
        /// </summary>
        None,

        /// <summary>
        /// Unknown reason.
        /// </summary>
        Unknown,

        /// <summary>
        /// The revocation was caused by a developer related issue.
        /// </summary>
        DeveloperIssue
    }
}