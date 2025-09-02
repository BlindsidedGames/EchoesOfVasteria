using System;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    internal class RateMyAppPresenter
    {
        

        #region Fields
        private INativeRateMyAppInterface                       m_nativeInterface   = null;

        private RateMyAppConfirmationDialogSettings             m_confirmationDialogSettings;

        private Action<RateMyAppConfirmationPromptActionType>   m_onConfirmationPromptAction = null;
        private bool                                            m_isShowingPrompt       = false;
        private string                                          m_storeId = null;

        #endregion

        #region Constructor

        public RateMyAppPresenter(string storeId, RateMyAppConfirmationDialogSettings settings, Action<RateMyAppConfirmationPromptActionType> onConfirmationPromptAction)
        {
            m_nativeInterface = NativeFeatureActivator.CreateInterface<INativeRateMyAppInterface>(ImplementationSchema.RateMyApp, true);
            m_storeId = storeId;
            m_confirmationDialogSettings = settings;
            m_onConfirmationPromptAction = onConfirmationPromptAction;
        }

        #endregion

        #region Public methods

        public bool IsShowing()
        {
            return m_isShowingPrompt;
        }

        public void Show(bool skipConfirmationPrompt)
        {
            // create prompt
            var     dialogSettings  = m_confirmationDialogSettings;
            if (dialogSettings.CanShow && !skipConfirmationPrompt)
            {
                m_isShowingPrompt = true;

                var     localisationServiceProvider = ExternalServiceProvider.LocalisationServiceProvider;
                var     dialogBuilder               = new AlertDialogBuilder()
                    .SetTitle(localisationServiceProvider.GetLocalisedString(key: RateMyAppLocalisationKey.kTitle, defaultValue: dialogSettings.PromptTitle))
                    .SetMessage(localisationServiceProvider.GetLocalisedString(key: RateMyAppLocalisationKey.kDescription, defaultValue: dialogSettings.PromptDescription))
                    .AddButton(localisationServiceProvider.GetLocalisedString(key: RateMyAppLocalisationKey.kOkButton, defaultValue: dialogSettings.OkButtonLabel), () => OnPromptButtonPressed(RateMyAppConfirmationPromptActionType.Ok))
                    .AddCancelButton(localisationServiceProvider.GetLocalisedString(key: RateMyAppLocalisationKey.kCancelButton, defaultValue: dialogSettings.CancelButtonLabel), () => OnPromptButtonPressed(RateMyAppConfirmationPromptActionType.Cancel));
                if (dialogSettings.CanShowRemindMeLaterButton)
                {
                    dialogBuilder.AddButton(localisationServiceProvider.GetLocalisedString(key: RateMyAppLocalisationKey.kRemindLaterButton, defaultValue: dialogSettings.RemindLaterButtonLabel), () =>  OnPromptButtonPressed(RateMyAppConfirmationPromptActionType.RemindLater));
                }
                var newAlertDialog  = dialogBuilder.Build();
                newAlertDialog.Show();
            }
            else
            {
                ShowReviewWindow();
            }
        }

        #endregion

        #region Private methods

        private void ShowReviewWindow()
        {
            m_nativeInterface.RequestStoreReview(m_storeId);
        }

        private void OnPromptButtonPressed(RateMyAppConfirmationPromptActionType selectedButtonType)
        {
            // reset flag
            m_isShowingPrompt = false;

            if(selectedButtonType == RateMyAppConfirmationPromptActionType.Ok)
            {
                ShowReviewWindow();
            }

            m_onConfirmationPromptAction?.Invoke(selectedButtonType);
        }

        #endregion
    }
}