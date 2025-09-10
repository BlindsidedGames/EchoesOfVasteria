using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
namespace VoxelBusters.EssentialKit
{
    public partial class CloudServicesUnitySettings
    {
        [Serializable]
        public class IosPlatformProperties 
        {
            #region Fields

            [SerializeField]
            [Tooltip ("Enable this if you want to replace the entitlement identifiers with absolute values.")]
            private     bool      m_substituteEntitlementIdentifiers;

            #endregion

            #region Properties

            public bool SubstituteEntitlementIdentifiers  
            { 
                get 
                { 
                    return m_substituteEntitlementIdentifiers; 
                } 
                set 
                { 
                    m_substituteEntitlementIdentifiers = value; 
                } 
            }
    
            #endregion

            #region Constructors

            public IosPlatformProperties(bool substituteEntitlementIdentifiers = false)
            {
                // set properties
                m_substituteEntitlementIdentifiers     = substituteEntitlementIdentifiers;
            }

            #endregion
        }
    }
}