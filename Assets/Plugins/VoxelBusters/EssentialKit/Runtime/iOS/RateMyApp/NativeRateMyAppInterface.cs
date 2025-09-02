#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;
using VoxelBusters.EssentialKit.NativeUICore.iOS;

namespace VoxelBusters.EssentialKit.RateMyAppCore.iOS
{
    public class NativeRateMyAppInterface : NativeRateMyAppInterfaceBase
    {
        #region Constructors

        public NativeRateMyAppInterface()
            : base(isAvailable: true)
        { }

        #endregion

        #region Base methods

        public override void RequestStoreReview(string storeId = null)
        {
            RateMyAppBinding.NPStoreReviewRequestReview(storeId);
        }

        #endregion
    }
}
#endif