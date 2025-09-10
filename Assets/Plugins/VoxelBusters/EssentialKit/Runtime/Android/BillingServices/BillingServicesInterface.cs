#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public sealed class BillingServicesInterface : NativeBillingServicesInterfaceBase
    {
        #region Static fields

        private static  NativeGoogleBillingServices    m_instance;

        #endregion

        #region Constructors

        public BillingServicesInterface()
            : base(isAvailable: true)
        {
            try
            {
                Debug.Log("Creating Billing Services Interface : Android");
                m_instance = new NativeGoogleBillingServices(NativeUnityPluginUtility.GetContext());
                BillingServicesUnitySettings settings = BillingServices.UnitySettings;

                m_instance.Initialise(settings.AndroidProperties.PublicKey, new NativeBillingTransactionStateListener()
                {
                    onStartedCallback = (nativeTransaction) =>
                    {
                        BillingTransaction transaction = Converter.From(nativeTransaction);
                        SendPaymentStateChangeEvent(new BillingTransaction[] { transaction });
                    },
                    onUpdatedCallback = (nativeTransaction) =>
                    {
                        BillingTransaction transaction = Converter.From(nativeTransaction);
                        SendPaymentStateChangeEvent(new BillingTransaction[] { transaction });
                    },
                    onFailedCallback = (nativeTransaction, errorInfo) =>
                   {
                       BillingTransaction transaction = Converter.From(nativeTransaction, errorInfo.Convert(BillingServicesError.kDomain));
                       SendPaymentStateChangeEvent(new BillingTransaction[] { transaction });
                   }
                });
                
                SetProducts(settings.Products);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception : " + e.Message + "   \n  " + e.StackTrace.ToString());
            }
        }

        #endregion

        #region Base class methods

        public override bool CanMakePayments()
        {
            return m_instance.CanMakePayments();
        }

        public override bool IsProductPurchased(IBillingProduct product)
        {
            return m_instance.IsProductPurchased(product.PlatformId);
        }

        public override void RetrieveProducts(BillingProductDefinition[] productDefinitions)
        {
            SetProducts(productDefinitions);
            m_instance.FetchProductDetails(new NativeFetchBillingProductsListener()
            {
                onSuccessCallback = (NativeList<NativeBillingProduct> nativeList, NativeArrayBuffer<string> invalidIds) =>
                {
                    //Filter billing products which are listed in the settings only
                    List<NativeBillingProduct> nativeBillingProducts = nativeList.Get();
                    List<BillingProduct> filteredBillingProducts = new List<BillingProduct>();

                    foreach (NativeBillingProduct each in nativeBillingProducts)
                    {
                        var settings = BillingServices.FindProductDefinitionWithPlatformId(each.GetIdentifier());
                        if (settings != null)
                        {
                            filteredBillingProducts.Add(new BillingProduct(settings.Id, settings.ProductType, each, settings.Payouts));
                        }
                    }

                    BillingProduct[] products = filteredBillingProducts.ToArray();
                    SendRetrieveProductsCompleteEvent(products, invalidIds.GetArray(), null);
                },
                onFailureCallback = (errorInfo) =>
                {
                    SendRetrieveProductsCompleteEvent(null, null, errorInfo.Convert(BillingServicesError.kDomain));
                }
            });
        }

        public override void BuyProduct(string productId, string productPlatformId, BuyProductOptions options)
        {
            m_instance.BuyProduct(productPlatformId, options.Quantity, options.Tag?.ToString(), offerId: options.OfferRedeemDetails?.AndroidPlatformProperties?.OfferId);
        }

        public override IBillingTransaction[] GetTransactions()
        {
            NativeList<NativeBillingTransaction> nativeList = m_instance.GetIncompleteBillingTransactions();
            IBillingTransaction[] transactions = Converter.From(nativeList.Get());

            return transactions;
        }

        public override void FinishTransactions(IBillingTransaction[] transactions)
        {
            foreach(IBillingTransaction each in transactions)
            {
                //Should only finish transactions for purchased or restored
                if(each.TransactionState != BillingTransactionState.Purchased)
                {
                    continue;
                }

                m_instance.FinishBillingTransaction(each.Product.PlatformId, each.ReceiptVerificationState == BillingReceiptVerificationState.Success);
            }
        }

        public override void RestorePurchases(bool forceRefresh, string tag)
        {
            m_instance.RestorePurchases(forceRefresh, tag?.ToString(), new NativeRestorePurchasesListener()
            {
                onSuccessCallback = (nativeTransactions) =>
                {
                    DebugLogger.Log("RestorePurchases : onSuccessCallback - " + nativeTransactions);
                    IBillingTransaction[] transactions = Converter.From(nativeTransactions.Get());
                    SendRestorePurchasesCompleteEvent(transactions, null);
                },
                onFailureCallback = (errorInfo) =>
                {
                    SendRestorePurchasesCompleteEvent(null, errorInfo.Convert(BillingServicesError.kDomain));
                }
            });
        }

        public override void TryClearingUnfinishedTransactions()
        {
            m_instance.TryClearingIncompleteTransactions();
        }

        #endregion

        #region Helpers

        private void SetProducts(BillingProductDefinition[] productDefinitions)
        {
            BillingProductDefinition[] consumableProducts = productDefinitions.Where(item => item.ProductType == BillingProductType.Consumable).ToArray();
            var consumableProductIds = Array.ConvertAll(consumableProducts, (item) => item.GetPlatformIdForActivePlatform());

            BillingProductDefinition[] nonConsumableProducts = productDefinitions.Where(item => item.ProductType == BillingProductType.NonConsumable).ToArray();
            var nonConsumableProductIds = Array.ConvertAll(nonConsumableProducts, (item) => item.GetPlatformIdForActivePlatform());

            BillingProductDefinition[] subscriptionProducts = productDefinitions.Where(item => item.ProductType == BillingProductType.Subscription).ToArray();
            var subscriptionProductIds = Array.ConvertAll(subscriptionProducts, (item) => item.GetPlatformIdForActivePlatform());

            m_instance.SetProducts(consumableProductIds, nonConsumableProductIds, subscriptionProductIds);
        }

        #endregion
    }
}
#endif