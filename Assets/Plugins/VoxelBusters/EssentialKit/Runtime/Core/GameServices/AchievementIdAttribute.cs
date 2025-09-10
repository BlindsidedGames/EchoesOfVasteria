using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class AchievementIdAttribute : StringPopupAttribute
    {
        #region Fields

        private     string[]        m_options;

        #endregion

        #region Constructors

        public AchievementIdAttribute()
            : base()
        {
            // set properties
            m_options   = GetAchievementIds();
        }

        #endregion

        #region Private static methods

        private static string[] GetAchievementIds()
        {
            try
            {
                return System.Array.ConvertAll(EssentialKitSettings.Instance.GameServicesSettings.Achievements, (item) => item.Id);
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