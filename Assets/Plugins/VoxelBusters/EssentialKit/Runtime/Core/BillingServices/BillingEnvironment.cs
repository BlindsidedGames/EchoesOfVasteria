using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Server environment where current billing transactions are processed and signed. 
    /// </summary>
    public enum BillingEnvironment
    {
        /// <summary>
        /// The environment is not known.
        /// </summary>
        Unknown,

        /// <summary>
        /// The production environment where real transactions occur.
        /// </summary>
        Production,

        /// <summary>
        /// The sandbox environment used for testing purposes.
        /// </summary>
        Sandbox,

        /// <summary>
        /// The local environment for development purposes.
        /// </summary>
        Local
    }
}