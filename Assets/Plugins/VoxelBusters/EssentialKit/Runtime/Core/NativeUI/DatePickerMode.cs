using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Specifies the mode of the date picker to be displayed.
    /// </summary>
    public enum DatePickerMode
    {
        /// <summary>
        /// The date picker should show date only.
        /// </summary>
        Date,

        /// <summary>
        /// The date picker should show time only.
        /// </summary>
        Time,

        /// <summary>
        /// The date picker should show date and time.
        /// </summary>
        DateAndTime,
    }
}