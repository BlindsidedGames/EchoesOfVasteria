using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Simulator
{
    public sealed class BillingServicesInterface : NativeBillingServicesInterfaceBase
    {
        #region Constructors

        public BillingServicesInterface()
            : base(isAvailable: true)
        { }

        #endregion

        #region Create methods

        private static BillingProductPlain[] ConvertToProductArray(BillingProductData[] array)
        {
            return SystemUtility.ConvertEnumeratorItems(
                enumerator: ((IEnumerable<BillingProductData>)array).GetEnumerator(), 
                converter: (input) =>
                {
                    string  productId   = input.Id;
                    var     settings    = BillingServices.FindProductDefinitionWithId(productId);
                    if (null == settings)
                    {
                        DebugLogger.LogWarning(EssentialKitDomain.Default, $"Could not find settings for specified id: {productId}.");
                        return null;
                    }

                    var price = new BillingPrice(input.Price, input.PriceCurrencyCode, input.PriceCurrencySymbol, input.DisplayText);
                    var subscriptionInfo = GetSampleSubscriptionInfo(settings);

                    return new BillingProductPlain(
                        id: productId, 
                        platformId: settings.GetPlatformIdForActivePlatform(),
                        type: settings.ProductType,
                        localizedTitle: input.LocalizedTitle,
                        localizedDescription: input.LocalizedDescription,
                        price: price,
                        subscriptionInfo: subscriptionInfo,
                        offers: GetSampleOffers(settings, price),
                        payouts: settings.Payouts);
                },
                includeNullObjects: false);
        }

        private static BillingTransactionPlain[] ConvertToTransactionArray(BillingTransactionData[] array, BillingReceiptVerificationState verificationState)
        {
            return SystemUtility.ConvertEnumeratorItems(
                enumerator: ((IEnumerable<BillingTransactionData>)array).GetEnumerator(), 
                converter: (input) =>
                {
                    string  productId       = input.ProductId;
                    var     settings        = BillingServices.FindProductDefinitionWithId(productId, returnObjectOnFail: true);
                    var     billingProduct  = BillingServices.GetProductWithId(productId, includeInactive: true);
                    if (null == settings)
                    {
                        DebugLogger.LogWarning(EssentialKitDomain.Default, $"Could not find settings for specified id: {productId}.");
                        return null;
                    }

                    var     payment     = new BillingPayment(
                        productId: productId, 
                        productPlatformId: settings.GetPlatformIdForActivePlatform(), 
                        quantity: input.Quantity, 
                        tag: input.Tag);

                    return new BillingTransactionPlain(
                        transactionId: input.TransactionId,
                        product: billingProduct, 
                        requestedQuantity: input.Quantity, 
                        tag: input.Tag,
                        transactionState: input.TransactionState,
                        verificationState: verificationState,
                        transactionDate: input.TransactionDate,
                        receipt: null,
                        environment: input.Environment,
                        applicationBundleIdentifier: input.ApplicationBundleIdentifier,
                        purchasedQuantity: input.PurchasedQuantity,
                        revocationInfo: input.RevocationDate.HasValue ? new BillingProductRevocationInfo(dateUTC: input.RevocationDate.Value, reason: input.RevocationReason.Value) : null,
                        subscriptionStatus: input.SubscriptionStatus,
                        rawData: input.RawData,
                        error: input.Error);
                },
                includeNullObjects: false);
        }

        private static BillingProductSubscriptionInfo GetSampleSubscriptionInfo(BillingProductDefinition product)
        {
            if(product.ProductType != BillingProductType.Subscription)
            {
                return null;
            }

            var billingPeriodUnit = GetPeriodUnitFromSubscriptionProduct(product);

            if(billingPeriodUnit == null)
                return null;

            var info = new BillingProductSubscriptionInfo(groupId: "simulatorGroupId", localizedGroupTitle: "simulatorSubscriptionGroup", level: 1, period: new BillingPeriod(duration: 1, billingPeriodUnit.Value));
            return info;
        }

        private static IEnumerable<BillingProductOffer> GetSampleOffers(BillingProductDefinition productDefinition, BillingPrice price)
        {
            List<BillingProductOffer> offers = new List<BillingProductOffer>();

            if(productDefinition.ProductType != BillingProductType.Subscription)
            {
                return offers;
            }

            var billingPeriodUnit = GetPeriodUnitFromSubscriptionProduct(productDefinition);
            if(billingPeriodUnit == null)
            {
                return offers;
            }

            var random = UnityEngine.Random.Range(0.0f, 1f);
            BillingProductOfferPricingPhase freeTrialPricingPhase = new BillingProductOfferPricingPhase(BillingProductOfferPaymentMode.FreeTrial, new BillingPrice(0.00, "USD", "$", "Free trial"), new BillingPeriod(1, billingPeriodUnit.Value-1), 1);            
            BillingProductOfferPricingPhase promotionalPricingPhase = new BillingProductOfferPricingPhase(BillingProductOfferPaymentMode.PayAsYouGo, new BillingPrice(Math.Round(price.Value * random,2), "USD", "$", $"${price.Value*random:0.##}"), new BillingPeriod(1, billingPeriodUnit.Value-1), 3);
            offers.Add(new BillingProductOffer("sampleOffer1", BillingProductOfferCategory.Introductory, new List<BillingProductOfferPricingPhase>(){freeTrialPricingPhase}));
            offers.Add(new BillingProductOffer("sampleOffer2", BillingProductOfferCategory.Promotional, new List<BillingProductOfferPricingPhase>(){promotionalPricingPhase}));

            return offers;
        }

        private static BillingPeriodUnit? GetPeriodUnitFromSubscriptionProduct(BillingProductDefinition productDefinition)
        {
            var subscriptionDescription = productDefinition.Description;
            BillingPeriodUnit? unit = null;

            if(string.IsNullOrEmpty(subscriptionDescription))
                unit = null;

            if(subscriptionDescription.Contains("week", StringComparison.OrdinalIgnoreCase))
            {
                unit = BillingPeriodUnit.Week;
            }
            else if(subscriptionDescription.Contains("month", StringComparison.OrdinalIgnoreCase))
            {
                unit =  BillingPeriodUnit.Month;
            }
            else if(subscriptionDescription.Contains("year", StringComparison.OrdinalIgnoreCase))
            {
                unit =  BillingPeriodUnit.Year;
            }

            if(unit == null)
            {
                DebugLogger.LogWarning(EssentialKitDomain.Default, $"Set a product description (containing week/month/year text)for the subscription {productDefinition.Id} so that subscription can be relevantly simulated on editor.");
            }

            return unit;
        }


        #endregion

        #region Base class methods

        public override bool CanMakePayments()
        {
            return true;
        }

        public override void RetrieveProducts(BillingProductDefinition[] productDefinitions)
        {
            BillingStoreSimulator.Instance.GetProducts(productDefinitions, (dataArray, error) =>
            {
                var     products    = ConvertToProductArray(dataArray);
                SendRetrieveProductsCompleteEvent(products, null, error);
            });
        }

        public override bool IsProductPurchased(IBillingProduct product)
        {
            return BillingStoreSimulator.Instance.IsProductPurchased(product.Id);
        }

        public override void BuyProduct(string productId, string productPlatformId, BuyProductOptions options)
        {
            // initate request
            BillingStoreSimulator.Instance.BuyProduct(productId, productPlatformId, options.Quantity, options.Tag, (data) =>
            {
                var     transactions    = ConvertToTransactionArray(new BillingTransactionData[] { data }, GetReceiptVerificationState());
                SendPaymentStateChangeEvent(transactions);
            });
        }

        public override IBillingTransaction[] GetTransactions()
        {
            return null;
        }

        public override void FinishTransactions(IBillingTransaction[] transactions)
        { }

        public override void RestorePurchases(bool forceRefresh, string tag)
        {
            BillingStoreSimulator.Instance.RestorePurchases(forceRefresh, tag, (dataArray, error) =>
            {
                var     transactions    = ConvertToTransactionArray(dataArray, GetReceiptVerificationState());
                SendRestorePurchasesCompleteEvent(transactions, error);
            });
        }

        public override void TryClearingUnfinishedTransactions()
        {
        }

        #endregion

        #region Private methods

        private BillingReceiptVerificationState GetReceiptVerificationState()
        {
            return BillingReceiptVerificationState.Success;
        }

        #endregion
    }
}