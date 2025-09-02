using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.CoreLibrary
{
    [SerializeField]
    public abstract class SettingsPropertyGroup
    {
        #region Fields

        [SerializeField]
        [HideInInspector]
        private     bool        m_isEnabled     = true;

        #endregion

        #region Properties

        public bool IsEnabled
        {
            get
            {
                return m_isEnabled;
            }
            set
            {
                m_isEnabled = value;
            }
        }

        public string Name { get; private set; }

        #endregion

        #region Constructors

        protected SettingsPropertyGroup(string name, bool isEnabled = true)
        {
            // set properties
            m_isEnabled     = isEnabled;
            Name            = name;
        }

        #endregion
    }
}