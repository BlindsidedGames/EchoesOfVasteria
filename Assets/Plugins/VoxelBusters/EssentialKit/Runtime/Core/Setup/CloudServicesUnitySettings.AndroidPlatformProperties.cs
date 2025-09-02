using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public partial class CloudServicesUnitySettings
    {
        [Serializable]
        public class AndroidPlatformProperties 
        {
            #region Fields

            [SerializeField]
            [ReadOnly("On Android, both Cloud Services and Game Services internally use Google Play Services. So, setting play services application id in Game Services settings will get reflected here.")]
            [Tooltip ("Your application id in Google Play services. Set this value in Game Services settings -> Android Properties -> Play Services Application Id.")]
            private     string      m_playServicesApplicationId;

            #endregion

            #region Properties

            public string PlayServicesApplicationId  
            { 
                get 
                { 
                    return m_playServicesApplicationId; 
                } 
                set 
                { 
                    m_playServicesApplicationId = value; 
                } 
            }
    
            #endregion

            #region Constructors

            public AndroidPlatformProperties(string playServicesApplicationId = null)
            {
                // set properties
                m_playServicesApplicationId     = playServicesApplicationId;
            }

            #endregion
        }
    }
}