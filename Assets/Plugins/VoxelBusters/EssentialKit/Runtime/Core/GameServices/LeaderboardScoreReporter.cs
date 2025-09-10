using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class LeaderboardScoreReporter : MonoBehaviour
    {
        #region Fields

        [SerializeField, LeaderboardId]
        private     string                      m_leaderboardId;

        [SerializeField]
        private     ReportScoreFinishEvent      m_onReportScoreFinish;

        #endregion

        #region Public methods

        public void ReportScore(long score)
        {
            // check whether behaviour is enabled
            if (!enabled)
            {
                return;
            }

            // initiate request
            GameServices.ReportScore(
                leaderboardId: m_leaderboardId,
                value: score,
                callback: (success, error) =>
                {
                    // send result
                    m_onReportScoreFinish?.Invoke(success, error);
                });
        }

        #endregion

        #region Nested types

        [Serializable]
        private class ReportScoreFinishEvent : UnityEvent<bool, Error>
        { }

        #endregion
    }
}