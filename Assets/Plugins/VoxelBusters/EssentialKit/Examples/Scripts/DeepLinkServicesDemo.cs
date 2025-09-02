// internal namespace

using UnityEngine;
using UnityEngine.UI;
using VoxelBusters.CoreLibrary.NativePlugins.DemoKit;

namespace VoxelBusters.EssentialKit.Demo
{
	public class DeepLinkServicesDemo : DemoActionPanelBase<DeepLinkServicesDemoAction, DeepLinkServicesDemoActionType>
	{
		#region Base methods

        protected override void OnEnable()
        {
            base.OnEnable();
            DeepLinkServices.OnUniversalLinkOpen += OnUniversalLinkOpen;
            DeepLinkServices.OnCustomSchemeUrlOpen += OnCustomSchemeUrlOpen;
        }



        protected override void OnDisable()
        {
            base.OnDisable();
            DeepLinkServices.OnUniversalLinkOpen -= OnUniversalLinkOpen;
            DeepLinkServices.OnCustomSchemeUrlOpen -= OnCustomSchemeUrlOpen;
        }

        protected override void OnActionSelectInternal(DeepLinkServicesDemoAction selectedAction)
        {
            /* <!--Load the below html page and open the deep link. Replace the href as per settings in Deep Link Services. For testing, just copy the below html code and host it in a browser.-->
             * <html>
               <body>
               <h1>Deep Link Services Test </h1>
               <br>
               <a href=essentialkit://demo> Open Deep Link</a>
               <br>
               <p>Here, essentialkit was set as scheme and demo was set as the host in Essential Kit Settings under Deep Link Services. </p>
               </body>               
               </html>
             */
            
            switch (selectedAction.ActionType)
            {
                
                case DeepLinkServicesDemoActionType.ResourcePage:
                    ProductResources.OpenResourcePage(NativeFeatureType.kDeepLinkServices);
                    break;

                default:
                    break;
            }
        }

        #endregion
        
        private void OnUniversalLinkOpen(DeepLinkServicesDynamicLinkOpenResult result)
        {
            Log($"[OnUniversalLinkOpen] Url: {result.Url} RawUrlString: {result.RawUrlString}" );
        }
        
        private void OnCustomSchemeUrlOpen(DeepLinkServicesDynamicLinkOpenResult result)
        {
            Log($"[OnCustomSchemeUrlOpen] Url: {result.Url} RawUrlString: {result.RawUrlString}" );
        }
	}
}