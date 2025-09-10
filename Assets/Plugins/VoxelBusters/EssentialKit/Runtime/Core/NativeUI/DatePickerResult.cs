using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// This class contains the information retrieved when <see cref="DatePicker.OnCloseCallback"/> is invoked.
    /// </summary>
    /// @ingroup NativeUI
    public class DatePickerResult
    {
        #region Properties

        /// <summary>
        /// Gets the selected date from the date picker.
        /// </summary>
        /// <value>The selected date, or null if no date was selected.</value>
        public DateTime? SelectedDate { get; private set; }

        #endregion

        #region Constructors

        internal DatePickerResult(DateTime? selectedDate)
        {
            // Set properties
            SelectedDate    = selectedDate;
        }

        #endregion
    }
}