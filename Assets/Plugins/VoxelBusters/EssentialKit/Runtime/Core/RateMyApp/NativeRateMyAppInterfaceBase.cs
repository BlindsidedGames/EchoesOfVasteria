using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.RateMyAppCore
{
    public abstract class NativeRateMyAppInterfaceBase : NativeFeatureInterfaceBase, INativeRateMyAppInterface
    {
        #region Constructors

        protected NativeRateMyAppInterfaceBase(bool isAvailable)
            : base(isAvailable)
        { }

        #endregion

        #region INativeUtilityInterface implementation

        public abstract void RequestStoreReview(string storeId = null);
        
        #endregion
    }
}