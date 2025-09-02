using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.NativeUICore
{
    public interface INativeAlertDialogInterface : INativeObject
    {   
        #region Events

        event AlertButtonClickInternalCallback OnButtonClick;

        #endregion

        #region Methods

        // setters and getter methods
        void SetTitle(string value);

        string GetTitle();

        void SetMessage(string value);

        string GetMessage();

        /// <summary>
        ///  Add a text field to the dialog.
        /// </summary>
        /// <param name="options">(Optional) Create a <see cref="TextInputFieldOptions"/> instance with <see cref="TextInputFieldOptions.Builder"/></param>
        void AddTextInputField(TextInputFieldOptions options = null);

        // action methods
        void AddButton(string text, bool isCancelType);

        // presentation methods
        void Show();
        
        void Dismiss();

        #endregion
    }
}