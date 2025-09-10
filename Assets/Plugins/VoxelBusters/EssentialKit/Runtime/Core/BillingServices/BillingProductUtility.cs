using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    public static class BillingUtility
    {
        #region Static methods

        public static bool IsNull(this IBillingProduct billingProduct)
        {
            return (billingProduct == null) || string.Equals(BillingServices.kNullProductId, billingProduct.Id);
        }

        #endregion
    }
}