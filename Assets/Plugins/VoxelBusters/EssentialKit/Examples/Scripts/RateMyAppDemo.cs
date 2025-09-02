using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// key namespaces
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit;
// internal namespace
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public class RateMyAppDemo : DemoActionPanelBase<RateMyAppDemoAction, RateMyAppDemoActionType>
	{
		#region Base methods

        protected override void OnActionSelectInternal(RateMyAppDemoAction selectedAction)
        {
            switch (selectedAction.ActionType)
            {
                case RateMyAppDemoActionType.AskForReviewNow:
					Log("Asking for review."); 
                    RateMyApp.AskForReviewNow();
                    break;

                case RateMyAppDemoActionType.IsAllowedToRate:
					Log("Is allowed to rate: " + RateMyApp.IsAllowedToRate()); 
                    break;
                case RateMyAppDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kRateMyApp);
                    break;

                default:
                    break;
            }
        }


        /*
            //When auto show option is off, you can write something like this to present the rating.
            if(RateMyApp.IsAllowedToRate())
            {
                RateMyApp.AskForReviewNow();
            }
        */
        #endregion
	}
}