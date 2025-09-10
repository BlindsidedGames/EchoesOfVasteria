using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class AchievementProgressReporter : MonoBehaviour
    {
        #region Fields

        [SerializeField, AchievementId]
        private     string                      m_achievementId;

        [SerializeField]
        private     ReportProgressFinishEvent   m_onReportProgressFinish;

        #endregion

        #region Public methods

        public void ReportProgress(double percentageCompleted)
        {
            // check whether behaviour is enabled
            if (!enabled)
            {
                return;
            }

            // initiate request
            GameServices.ReportAchievementProgress(
                achievementId: m_achievementId,
                percentageCompleted: percentageCompleted,
                callback: (success, error) =>
                {
                    // send result
                    m_onReportProgressFinish?.Invoke(success, error);
                });
        }

        #endregion

        #region Nested types

        [Serializable]
        private class ReportProgressFinishEvent : UnityEvent<bool, Error>
        { }

        #endregion
    }
}