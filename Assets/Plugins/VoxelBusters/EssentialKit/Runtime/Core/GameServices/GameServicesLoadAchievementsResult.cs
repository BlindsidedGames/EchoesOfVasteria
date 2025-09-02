using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="GameServices.LoadAchievements(EventCallback{GameServicesLoadAchievementsResult})"/> operation is completed.
    /// </summary>
    /// @ingroup GameServices
    public class GameServicesLoadAchievementsResult
    {
        #region Properties

        /// <summary>
        /// An array of registered achievements.
        /// </summary>
        public IAchievement[] Achievements { get; private set; }

        #endregion

        #region Constructors

        internal GameServicesLoadAchievementsResult(IAchievement[] achievements)
        {
            // set properties
            Achievements    = achievements;
        }

        #endregion
    }
}