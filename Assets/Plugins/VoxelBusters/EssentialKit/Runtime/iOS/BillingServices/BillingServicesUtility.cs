#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

// ReSharper disable CheckNamespace
namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    internal static class BillingServicesUtility
    {
        #region Converter methods

        public static IBillingProduct[] CreateProductArray(IntPtr productsPtr, int length)
        {
	        // ReSharper disable once CoVariantArrayConversion
	        return MarshalUtility.ConvertNativeArrayItems<SKProductData, BillingProduct>(
                arrayPtr: productsPtr,
                length: length, 
                converter: (input) =>
                {
                    string  platformId  = MarshalUtility.ToString(input.IdentifierPtr);
                    var     settings    = BillingServices.FindProductDefinitionWithPlatformId(platformId);
                    if (settings == null)
                    {
                        DebugLogger.LogWarning(EssentialKitDomain.Default, $"Could not find settings for specified platform id: {platformId}.");
                        return null;
                    }

                    return new BillingProduct(
                        nativeObjectPtr: input.NativeObjectPtr,
                        id: settings.Id,
                        platformId: platformId,
                        type: settings.ProductType,
                        localizedTitle: MarshalUtility.ToString(input.LocalizedTitlePtr),
                        localizedDescription: MarshalUtility.ToString(input.LocalizedDescriptionPtr),
                        price: new BillingPrice(input.Price.Value, input.Price.CurrencyCodePtr.AsString(), input.Price.CurrencySymbolPtr.AsString(), input.Price.LocalizedPricePtr.AsString()),
                        offers: new List<BillingProductOffer>(ConvertToBillingProductOffers(input.OffersArray)),
                        subscriptionInfo: ConvertToSubscriptionInfo(input.SubscriptionInfoPtr),
                        payouts: settings.Payouts);
                }, 
                includeNullObjects: false);
        }

        public static IBillingTransaction[] CreateTransactionArray(IntPtr transactionsPtr, int length)
        {
	        // ReSharper disable once CoVariantArrayConversion
	        return MarshalUtility.ConvertNativeArrayItems<SKPaymentTransactionData, BillingTransaction>(
                arrayPtr: transactionsPtr,
                length: length, 
                converter: (input) =>
                {
                    var     nativeObjectPtr      = input.NativeObjectPtr;
                    var     verificationState     = input.TransactionState == BillingTransactionState.Purchased ? BillingReceiptVerificationState.Success : BillingReceiptVerificationState.Failed;
                    var     productPlatformId   = MarshalUtility.ToString(input.ProductIdentifierPtr);
                    var     productDefinition   = BillingServices.FindProductDefinitionWithPlatformId(productPlatformId, returnObjectOnFail: true);
                    var     product             = BillingServices.GetProductWithId(productDefinition.Id, includeInactive: true);

                    return new BillingTransaction(
                        nativeObjectPtr: nativeObjectPtr,
                        transactionId: MarshalUtility.ToString(input.IdentifierPtr),
                        product: product,
                        requestedQuantity: input.RequestedQuantity,
                        tag: input.PurchaseTagPtr.AsString(),
                        transactionState: input.TransactionState,
                        verificationState: verificationState,
                        date: input.DatePtr.AsDateTime(),
                        receipt: input.ReceiptPtr.AsString(),
                        environment: input.Environment,
                        applicationBundleIdentifier: input.ApplicationBundleIdentifierPtr.AsString(),
                        purchasedQuantity: input.PurchasedQuantity,
                        revocationInfo: input.RevocationDatePtr != IntPtr.Zero ? new BillingProductRevocationInfo(input.RevocationDatePtr.AsDateTime(), input.RevocationReason) : null,
                        subscriptionStatus: ConvertToSubscriptionStatus(input.SubscriptionStatusPtr),
                        rawData: input.RawDataPtr.AsString(),
                        error: input.ErrorData.Convert(domain: null));
                },
                includeNullObjects: false);
        }

        public static IBillingPayment CreatePayment(IntPtr productIdentifierPtr, int quantity, IntPtr tagPtr)
        {
            string  productPlatformId   = MarshalUtility.ToString(productIdentifierPtr);
            var     productDefinition   = BillingServices.FindProductDefinitionWithPlatformId(productPlatformId, returnObjectOnFail: true);
            var     tagStr              = MarshalUtility.ToString(tagPtr);

            return new BillingPayment(
                productId: productDefinition.Id,
                productPlatformId: productPlatformId,
                quantity: quantity,
                tag: tagStr);
        }

        private static BillingProductSubscriptionInfo ConvertToSubscriptionInfo(IntPtr subscriptionInfoDataPtr)
        {
                if(subscriptionInfoDataPtr == IntPtr.Zero)
                {
                    return null;
                }

                SKSubscriptionInfoData data = MarshalUtility.PtrToStructure<SKSubscriptionInfoData>(subscriptionInfoDataPtr);

                return new BillingProductSubscriptionInfo(
                    groupId: data.GroupIdentifierPtr.AsString(),
                    localizedGroupTitle: data.LocalizedGroupTitlePtr.AsString(),
                    level: data.Level,
                    period: new BillingPeriod(data.Period.Duration, (BillingPeriodUnit)data.Period.Unit)
                );
        }

        private static BillingProductSubscriptionStatus ConvertToSubscriptionStatus(IntPtr subscriptionStatusDataPtr)
        {
                if(subscriptionStatusDataPtr == IntPtr.Zero)
                {
                    return null;
                }

                SKSubscriptionStatusData data = MarshalUtility.PtrToStructure<SKSubscriptionStatusData>(subscriptionStatusDataPtr);

                return new BillingProductSubscriptionStatus(
                    groupId: data.GroupIdentifierPtr.AsString(),
                    renewalInfo: ConvertToRenewalInfo(data.RenewalInfoPtr),
                    expirationDate: data.ExpirationDatePtr.AsDateTime(),
                    isUpgraded: data.IsUpgraded == 1,
                    appliedOfferIdentifier: data.AppliedOfferIdentifier.AsString(),
                    appliedOfferCategory: data.AppliedOfferCategory
                );
        }

        private static BillingProductOfferPricingPhase[] ConvertToBillingProductOfferPricingPhases(NativeArray array)
        {
             return MarshalUtility.ConvertNativeArrayItems<SkProductOfferPricingPhaseData, BillingProductOfferPricingPhase>(
                arrayPtr: array.Pointer,
                length: array.Length,
                converter: ConvertToBillingProductOfferPricingPhase,
                includeNullObjects: false);
        }

        private static BillingProductOfferPricingPhase ConvertToBillingProductOfferPricingPhase(SkProductOfferPricingPhaseData input)
        {
                return new BillingProductOfferPricingPhase(
                    paymentMode: (BillingProductOfferPaymentMode)input.PaymentMode,
                    price: new BillingPrice(input.Price.Value, input.Price.CurrencyCodePtr.AsString(), input.Price.CurrencySymbolPtr.AsString(), input.Price.LocalizedPricePtr.AsString()),
                    period: new BillingPeriod(input.Period.Duration, (BillingPeriodUnit)input.Period.Unit),
                    repeatCount: input.RepeatCount
                );
        }

        private static BillingProductOffer ConvertToBillingProductOffer(SKProductOfferData data)
        {
                return new BillingProductOffer(
                        id: data.IdentifierPtr.AsString(),
                        category: (BillingProductOfferCategory) data.Category,
                        pricingPhases: new List<BillingProductOfferPricingPhase>(ConvertToBillingProductOfferPricingPhases(data.PricingPhasesArray))
                );
        }

        private static BillingProductOffer[] ConvertToBillingProductOffers(NativeArray promotionalOffersArray)
        {
                NativeArray array = promotionalOffersArray;
                return MarshalUtility.ConvertNativeArrayItems<SKProductOfferData, BillingProductOffer>(
                arrayPtr: array.Pointer,
                length: array.Length,
                converter: ConvertToBillingProductOffer,
                includeNullObjects: false);
        }

        private static BillingProductSubscriptionRenewalInfo ConvertToRenewalInfo(IntPtr renewalInfoPtr)
        {
                if(renewalInfoPtr == IntPtr.Zero)
                {
                    return null;
                }

                SKSubscriptionRenewalInfoData data = MarshalUtility.PtrToStructure<SKSubscriptionRenewalInfoData>(renewalInfoPtr);
                return new BillingProductSubscriptionRenewalInfo(
                    state: data.State,
                    applicableOfferIdentifier: data.ApplicableOfferIdentifierPtr.AsString(),
                    applicableOfferCategory: data.ApplicableOfferCategory,
                    lastRenewedDate: data.LastRenewedDatePtr.AsDateTime(),
                    lastRenewalId: data.LastRenewalIdPtr.AsString(),
                    isAutoRenewEnabled: data.IsAutoRenewEnabled == 1,
                    expirationReason: data.ExpirationReason == BillingProductSubscriptionExpirationReason.None ? null : data.ExpirationReason,
                    renewalDate: data.RenewalDatePtr.AsOptionalDateTime(),
                    gracePeriodExpirationDate: data.GracePeriodExpirationDatePtr.AsOptionalDateTime(),
                    priceIncreaseStatus: data.PriceIncreaseStatus
                );
        }

        #endregion
    }
}
#endif