using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Simulator
{
    [Serializable]
    public sealed class BillingProductData
    {
        #region Fields

        [SerializeField]
        private     string                  m_id;

        [SerializeField]
        private     BillingProductType      m_productType;

        [SerializeField]
        private     string                  m_localizedTitle;

        [SerializeField]
        private     string                  m_localizedDescription;

        [FormerlySerializedAs("m_localizedPrice")]
        [SerializeField]
        private     double                  m_price;

        [SerializeField]
        private     string                  m_priceCurrencyCode;

        [SerializeField]
        private     string                  m_priceCurrencySymbol;

        [SerializeField]
        private     string                  m_displayText;

        #endregion

        #region Properties

        public string Id
        {
            get
            {
                return m_id;
            }
        }

        public BillingProductType ProductType
        {
            get
            {
                return m_productType;
            }
        }

        public string LocalizedTitle
        {
            get
            {
                return m_localizedTitle;
            }
        }

        public string LocalizedDescription
        {
            get
            {
                return m_localizedDescription;
            }
        }
        public double Price
        {
            get
            {
                return m_price;
            }
        }

        public string PriceCurrencyCode
        {
            get
            {
                return m_priceCurrencyCode;
            }
        }

        public string PriceCurrencySymbol
        {
            get
            {
                return m_priceCurrencySymbol;
            }
        }

        public string DisplayText
        {
            get
            {
                return m_displayText;
            }
        }

        #endregion

        #region Constructors

        public BillingProductData(string id, string title, string description, double price, string currencyCode, string currencySymbol, string displayText, BillingProductType productType)
        {
            // set properties
            m_id                    = id;
            m_productType           = productType;
            m_localizedTitle        = title;
            m_localizedDescription  = description;
            m_price                 = price;
            m_priceCurrencyCode     = currencyCode;
            m_priceCurrencySymbol   = currencySymbol;
            m_displayText           = displayText;
        }

        #endregion
    }
}