using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.CoreLibrary.NativePlugins.UnityUI
{
    public interface IUnityUIAlertDialog
    {
        #region Properties

        string Title
        {
            get;
            set;
        }

        string Message
        {
            get;
            set;
        }

        #endregion

        #region Methods

        void AddTextField(string placeholderText);
        void AddActionButton(string title);

        void Show();

        void Dismiss();

        void SetCompletionCallback(Action<int, string[]> callback);

        #endregion
    }
}