using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// The AddressBookUnitySettings class is used to configure Address Book module of Essential Kit.
    /// </summary>
    [Serializable]
    public class AddressBookUnitySettings : SettingsPropertyGroup
    {
        #region Fields

        /// <summary>
        /// The default image to be used for contact.
        /// </summary>
        [SerializeField]
        [Tooltip("The default image to be used for contact.")]
        private         Texture2D           m_defaultImage;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the default image to be used for contact.
        /// </summary>
        public Texture2D DefaultImage => m_defaultImage;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressBookUnitySettings"/> class with the specified settings.
        /// </summary>
        /// <param name="isEnabled">if set to <c>true</c> [is enabled].</param>
        public AddressBookUnitySettings(bool isEnabled = true)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kAddressBook)
        { 
            // set properties
            m_defaultImage      = null;
        }

        #endregion
    }
}