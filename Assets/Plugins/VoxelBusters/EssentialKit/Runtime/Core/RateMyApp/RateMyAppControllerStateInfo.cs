using System;
using System.Globalization;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    internal class RateMyAppControllerStateInfo
    {
        #region Fields

        [SerializeField]
        private     string      m_versionLastRated;

        [SerializeField]
        private     int         m_appLaunchCount;
        
        [SerializeField]
        private     string      m_promptLastShown;

        [SerializeField]
        private     int         m_promptCount;

        [SerializeField]
        private     bool        m_isActive;

        #endregion

        #region Properties

        public string VersionLastRated
        {
            get
            {
                return m_versionLastRated;
            }
            set
            {
                m_versionLastRated  = value;
            }
        }

        public int AppLaunchCount
        {
            get
            {
                return m_appLaunchCount;
            }
            set
            {
                m_appLaunchCount    = value;
            }
        }

        public DateTime? PromptLastShown
        {
            get
            {
                return string.IsNullOrEmpty(m_promptLastShown) ? null : DeserializeDateTime(m_promptLastShown);
            }
            set
            {
                m_promptLastShown   = (value == null) ? null : SerializeDateTime(value.Value);
            }
        }

        public int PromptCount
        {
            get
            {
                return m_promptCount;
            }
            set
            {
                m_promptCount   = value;
            }
        }

        public bool IsActive
        {
            get
            {
                return m_isActive;
            }
            set
            {
                m_isActive  = value;
            }
        }

        #endregion

        #region Constructors

        public RateMyAppControllerStateInfo()
        {
            // set properties
            m_versionLastRated  = null;
            m_appLaunchCount    = 0;
            m_promptLastShown   = null;
            m_promptCount       = 0;
            m_isActive          = true;
        }

        #endregion

        #region Static methods

        private static string SerializeDateTime(DateTime dateTime)
        {
            return dateTime.ToString("O");
        }

        private static DateTime? DeserializeDateTime(string dateTimeStr)
        {
            DateTime    dateTime;
            
            if (DateTime.TryParse(dateTimeStr, CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                return dateTime;
            }
            return null;
        }

        #endregion
    }
}