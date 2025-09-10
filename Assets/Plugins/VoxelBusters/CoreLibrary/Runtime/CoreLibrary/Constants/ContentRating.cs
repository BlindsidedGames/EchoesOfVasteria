using UnityEngine;
using System.Collections;

namespace VoxelBusters.CoreLibrary
{
    /// <summary>
    /// Enum specifying the level of content maturity for an app.
    /// </summary>
    /// <description>
    /// This enum is used to specify the level of content maturity for an app.
    /// </description>
    public enum ContentRating
    {
        Unspecified = 0,

        /// <summary> Content suitable for general audiences, including families. </summary>
        GeneralAudience,

        /// <summary> Content suitable only for mature audiences. </summary>
        MatureAudience,

        /// <summary> Content suitable for most audiences with parental guidance. </summary>
        ParentalGuidance,

        /// <summary> Content suitable for teen and older audiences. </summary>
        TeensAndOlder,
    }  
}