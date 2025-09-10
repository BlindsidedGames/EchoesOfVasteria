using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class BillingProductIdAttribute : StringPopupAttribute
    {
        #region Fields

        private     string[]        m_options;

        #endregion

        #region Constructors

        public BillingProductIdAttribute()
            : base()
        {
            // set properties
            m_options   = GetProductIds();
        }

        #endregion

        #region Private static methods

        private static string[] GetProductIds()
        {
            try
            {
                return System.Array.ConvertAll(EssentialKitSettings.Instance.BillingServicesSettings.Products, (item) => item.Id);
            }
            catch
            {
                return new string[0];
            }
        }

        #endregion

        #region Base class methods

        protected override string[] GetDynamicOptions()
        {
            return m_options;
        }

        #endregion
    }
}