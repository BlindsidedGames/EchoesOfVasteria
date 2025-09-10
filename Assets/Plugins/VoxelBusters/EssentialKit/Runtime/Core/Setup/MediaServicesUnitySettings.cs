using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using UnityEngine.Serialization;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public class MediaServicesUnitySettings : SettingsPropertyGroup
    {
        #region Fields

        [Header("Capture Media Content Settings")]
        [SerializeField, FormerlySerializedAs("m_usesCamera")]
        [Tooltip ("If enabled, permission required to access camera will be added for image capture.")]
        private     bool                    m_usesCameraForImageCapture;

        [SerializeField]
        [Tooltip("If enabled, permission required to access camera will be added for video capture (video capture additionally needs microphone, we add it automatically once you enable this).")]
        private bool m_usesCameraForVideoCapture;


        [Space]
        [Header("Save Media Content Settings")]
        [SerializeField]
        [Tooltip ("If enabled, permission required to save files in photo gallery will be added.")]
        private     bool                    m_savesFilesToPhotoGallery = true;

        [SerializeField]
        [Tooltip ("If enabled, permission required to create custom directories when saving will be added. For ex: permission to create new albums in photo gallery will be added.")]
        private     bool                    m_savesFilesToCustomDirectories = true;

        #endregion

        #region Properties

        public bool UsesCameraForImageCapture => m_usesCameraForImageCapture;
        public bool UsesCameraForVideoCapture => m_usesCameraForVideoCapture;

        public bool SavesFilesToPhotoGallery => m_savesFilesToPhotoGallery;

        public bool SavesFilesToCustomDirectories => m_savesFilesToCustomDirectories;

        #endregion

        #region Constructors

        public MediaServicesUnitySettings(bool isEnabled = true, bool usesCameraForImageCapture = true, bool usesCameraForVideoCapture = false,
            bool savesFilesToGallery = true, bool savesFilesToCustomAlbums = true)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kMediaServices)
        {
            // set properties
            m_usesCameraForImageCapture             = usesCameraForImageCapture;
            m_usesCameraForVideoCapture             = usesCameraForVideoCapture;
            m_savesFilesToPhotoGallery   = savesFilesToGallery;
            m_savesFilesToCustomDirectories = savesFilesToCustomAlbums;
        }

        #endregion
    }
}