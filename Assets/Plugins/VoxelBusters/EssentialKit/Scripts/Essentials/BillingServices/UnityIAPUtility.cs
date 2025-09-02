#if ESSENTIAL_KIT_USE_UNITY_IAP_BILLING
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    public static class UnityIAPUtility
    {
#region Unity methods

        public static ProductDefinition ConvertToUnityProductDefinition(BillingProductDefinition productDefinition)
        {
            return new ProductDefinition(id: productDefinition.Id,
                                         storeSpecificId: productDefinition.GetPlatformIdForActivePlatform(),
                                         type: ConvertToUnityProductType(productDefinition.ProductType),
                                         enabled: true,
                                         payouts: CollectionUtility.ConvertAll(productDefinition.Payouts, (item) => ConvertToUnityPayoutDefinition(item)));
        }

        public static PayoutDefinition ConvertToUnityPayoutDefinition(BillingProductPayoutDefinition payoutDefinition)
        {
            return new PayoutDefinition(type: ConvertToUnityPayoutType(payoutDefinition.Category),
                                        subtype: payoutDefinition.Variant,
                                        quantity: payoutDefinition.Quantity,
                                        data: payoutDefinition.Data);
        }

        public static ProductType ConvertToUnityProductType(BillingProductType productType)
        {
            switch (productType)
            {
                case BillingProductType.Consumable:
                    return ProductType.Consumable;

                case BillingProductType.NonConsumable:
                    return ProductType.NonConsumable;

                case BillingProductType.Subscription:
                    return ProductType.Subscription;

                default:
                    throw VBException.SwitchCaseNotImplemented(productType);
            }
        }

        public static PayoutType ConvertToUnityPayoutType(BillingProductPayoutCategory payoutCategory)
        {
            switch (payoutCategory)
            {
                case BillingProductPayoutCategory.Currency:
                    return PayoutType.Currency;

                case BillingProductPayoutCategory.Item:
                    return PayoutType.Item;

                case BillingProductPayoutCategory.Other:
                    return PayoutType.Other;

                default:
                    throw VBException.SwitchCaseNotImplemented(payoutCategory);
            }
        }

#endregion

#region Native methods

        public static BillingProductPayoutDefinition ConvertToNativePayout(PayoutDefinition definition)
        {
            return new BillingProductPayoutDefinition(
                payoutType: ConvertToNativePayoutType(definition.type),
                subtype: definition.subtype,
                quantity: definition.quantity,
                data: definition.data);
        }

        public static BillingProductPayoutCategory ConvertToNativePayoutType(PayoutType payoutType)
        {
            switch (payoutType)
            {
                case PayoutType.Currency:
                    return BillingProductPayoutCategory.Currency;

                case PayoutType.Item:
                    return BillingProductPayoutCategory.Item;

                case PayoutType.Other:
                case PayoutType.Resource:
                    return BillingProductPayoutCategory.Other;

                default:
                    throw VBException.SwitchCaseNotImplemented(payoutType);
            }
        }

        public static IBillingProduct ConvertToNativeProduct(Product unityProduct)
        {
            return new BillingProductPlain(id: unityProduct.definition.id,
                                           platformId: unityProduct.definition.storeSpecificId,
                                           localizedTitle: unityProduct.metadata.localizedTitle,
                                           localizedDescription: unityProduct.metadata.localizedDescription,
                                           price: unityProduct.metadata.localizedPriceString,
                                           localizedPrice: unityProduct.metadata.localizedPriceString,
                                           priceCurrencyCode: unityProduct.metadata.isoCurrencyCode,
                                           payouts: CollectionUtility.ConvertAll(new List<PayoutDefinition>(unityProduct.definition.payouts), converter: (item) => ConvertToNativePayout(item)));
        }

        public static IBillingTransaction ConvertToNativeTransaction(Product unityProduct)
        {
            return null;
        }

        public static Error ConvertToNativeError(InitializationFailureReason failureReason)
        {
            switch (failureReason)
            {
                case InitializationFailureReason.PurchasingUnavailable:
                    return BillingError.StoreNotAvailable;

                case InitializationFailureReason.NoProductsAvailable:
                    return BillingError.ConfigurationInvalid;

                case InitializationFailureReason.AppNotKnown:
                    return BillingError.StoreNotAvailable;

                default:
                    throw VBException.SwitchCaseNotImplemented(failureReason);
            }
        }

        public static Error ConvertToNativeError(PurchaseFailureReason failureReason)
        {
            switch (failureReason)
            {
                case PurchaseFailureReason.PurchasingUnavailable:
                    return BillingError.PaymentNotAllowed;

                case PurchaseFailureReason.ProductUnavailable:
                    return BillingError.ProductUnavailable;

                case PurchaseFailureReason.SignatureInvalid:
                    return BillingError.SignatureInvalid;

                case PurchaseFailureReason.UserCancelled:
                    return BillingError.PaymentCancelled;

                case PurchaseFailureReason.PaymentDeclined:
                    return BillingError.PaymentDeclined;

                case PurchaseFailureReason.ExistingPurchasePending:
                case PurchaseFailureReason.DuplicateTransaction:
                    return BillingError.PaymentInvalid;

                case PurchaseFailureReason.Unknown:
                default:
                    return BillingError.Unknown;
            }
        }

#endregion
    }
}
#endif