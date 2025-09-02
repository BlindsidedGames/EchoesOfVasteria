using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class LeaderboardIdAttribute : StringPopupAttribute
    {
        #region Fields

        private     string[]        m_options;

        #endregion

        #region Constructors

        public LeaderboardIdAttribute()
            : base()
        {
            // set properties
            m_options   = GetLeaderboardIds();
        }

        #endregion

        #region Private static methods

        private static string[] GetLeaderboardIds()
        {
            try
            {
                return System.Array.ConvertAll(EssentialKitSettings.Instance.GameServicesSettings.Leaderboards, (item) => item.Id);
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