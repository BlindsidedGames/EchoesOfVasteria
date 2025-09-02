using System;
using UnityEngine;

namespace VoxelBusters.EssentialKit.GameServicesCore.Simulator
{
    [Serializable]
    public sealed class ScoreData
    {
        #region Fields

        [SerializeField]
        private         string          m_playerId;

        [SerializeField]
        private         string          m_leaderboardId;

        [SerializeField]
        private         string          m_leaderboardPlatformId;

        [SerializeField]
        private         long            m_value;     

        [SerializeField]
        private         string          m_tag;      

        #endregion

        #region Properties

        public string PlayerId
        {
            get
            {
                return m_playerId;
            }
            set
            {
                m_playerId    = value;
            }
        }

        public string LeaderboardId
        {
            get
            {
                return m_leaderboardId;
            }
            set
            {
                m_leaderboardId    = value;
            }
        }

        public string LeaderboardPlatformId
        {
            get
            {
                return m_leaderboardPlatformId;
            }
            set
            {
                m_leaderboardPlatformId = value;
            }
        }

        public long Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;
            }
        }

        public string Tag
        {
            get
            {
                return m_tag;
            }
            set
            {
                m_tag = value;
            }
        }

        #endregion
    }
}