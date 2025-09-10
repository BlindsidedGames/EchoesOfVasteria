using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using System.Globalization;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    internal class RateMyAppController
    {
        #region Fields

        private     RateMyAppUnitySettings                  m_settings     = null;
        private     RateMyAppPresenter                      m_presenter;
        private     RateMyAppControllerStateInfo            m_stateInfo             = null;
        private     Action<RateMyAppConfirmationPromptActionType>  m_onConfirmationPromptAction;

        #endregion

        #region Unity methods

        public RateMyAppController(RateMyAppUnitySettings settings, string storeId)
        {
            // initialise
            m_settings                          = settings;
            m_presenter                         = new RateMyAppPresenter(storeId, settings.ConfirmationDialogSettings, OnConfirmationDialogAction);
            m_stateInfo                         = LoadStateInfo() ?? new RateMyAppControllerStateInfo();

            RecordAppLaunch();

            if(settings.AutoShow)
            {
                CallbackDispatcher.InvokeOnMainThread(() => {
                    SurrogateCoroutine.StopCoroutine(CheckForPresentation());
                    SurrogateCoroutine.StartCoroutine(CheckForPresentation());
                });
            }
        }
        

        private IEnumerator CheckForPresentation()
        {
            while(true)
            {
                if(!m_presenter.IsShowing())
                {
                    if(CanShow())
                    {
                        m_presenter.Show(skipConfirmationPrompt: false);
                    }
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        #endregion

        #region Public methods

        public void SetConfirmationDialogCallback(Action<RateMyAppConfirmationPromptActionType> onAction)
        {
            m_onConfirmationPromptAction   = onAction;
        }

        public void Show(bool skipConfirmationPrompt = false)
        {
            m_presenter.Show(skipConfirmationPrompt);
        }


        #endregion

        #region Private methods

        private void OnConfirmationDialogAction(RateMyAppConfirmationPromptActionType selectedButtonType)
        {
            switch (selectedButtonType)
            {
                case RateMyAppConfirmationPromptActionType.RemindLater:
                    break;

                case RateMyAppConfirmationPromptActionType.Cancel:
                    m_stateInfo.IsActive    = false;
                    SetDirty();
                    break;

                case RateMyAppConfirmationPromptActionType.Ok:
                    m_stateInfo.VersionLastRated    = Application.identifier;
                    SetDirty();
                    break;
            }

            m_onConfirmationPromptAction?.Invoke(selectedButtonType);
        }

        

        private void RecordAppLaunch()
        {
            m_stateInfo.AppLaunchCount++;
            SetDirty();
        }

        private void SetPromptLastShown(DateTime dateTime, bool incrementPromptCount)
        {
            m_stateInfo.PromptLastShown = dateTime;
            if (incrementPromptCount)
            {
                m_stateInfo.PromptCount++;
            }
            SetDirty();
        }

        private bool CheckIfValidatorConditionsAreSatisfied()
        {
            // check whether constraints are satisfied
            var     currentTime         = DateTime.UtcNow;
            var     promptLastShown     = m_stateInfo.PromptLastShown;
            if (promptLastShown == null)
            {
                SetPromptLastShown(currentTime, incrementPromptCount: false);
                return false;
            }

            var     constraints         = (m_stateInfo.PromptCount == 0) ? m_settings.ConstraintsSettings.InitialPromptConstraints : m_settings.ConstraintsSettings.RepeatPromptConstraints;
            if (m_stateInfo.AppLaunchCount > constraints.MinLaunches && constraints.MinLaunches != -1)
            {
                int     hoursSincePromptLastShown   = (int)(currentTime - promptLastShown.Value.ToUniversalTime()).TotalHours;
                if (hoursSincePromptLastShown > constraints.MinHours)
                {
                    SetPromptLastShown(currentTime, incrementPromptCount: true);
                    return true;
                }
            }
            
            return false;
        }

        #endregion

        #region Serialize methods

        private void SetDirty()
        {
            SaveStateInfo(m_stateInfo);
        }

        private const string kPrefKey   = "rma_state";
        private RateMyAppControllerStateInfo LoadStateInfo()
        {
            string savedValue   = PlayerPrefs.GetString(kPrefKey);
            if (!string.IsNullOrEmpty(savedValue))
            {
                return JsonUtility.FromJson<RateMyAppControllerStateInfo>(savedValue);
            }
            return null;
        }

        private void SaveStateInfo(RateMyAppControllerStateInfo stateInfo)
        {
            Assert.IsArgNotNull(stateInfo, "stateInfo");

            string  jsonStr     = JsonUtility.ToJson(stateInfo);
            PlayerPrefs.SetString(kPrefKey, jsonStr);
        }

        #endregion

        #region IRateMyAppValidator implementation

        public bool CanShow()
        {
            // check if user has denied to show
            if (!m_stateInfo.IsActive)
            {
                return false;
            }

            // if we don't want any prompts, -1 is set in settings.
            if (m_settings.ConstraintsSettings.InitialPromptConstraints.MinLaunches == -1 && 
                m_settings.ConstraintsSettings.RepeatPromptConstraints.MinLaunches == -1) 
            {
                return false;
            }
            
            // check if rating is provided already
            var     versionLastRated    = m_stateInfo.VersionLastRated;
            if (!string.IsNullOrEmpty(versionLastRated))
            {
                if(!m_settings.AllowReratingForNewVersion)
                {
                    return false;    
                }

                // check if version matches, then it means app is already reviewed for this version
                string  currentVersion  = Application.version;
                if (string.Compare(currentVersion, versionLastRated, StringComparison.InvariantCulture) <= 0)
                {
                    return false;
                }
            }
            
            return CheckIfValidatorConditionsAreSatisfied();        
        }

#endregion
#region Nested types

        #endregion
    }
}