//
//  NPBillingServicesDataTypes.h
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>
#import "NPKit.h"
#import <BillingServices/BillingServices-Swift.h>

// callback signatures
typedef void (*RequestForProductsNativeCallback)(void* productsPtr, int length, NPError error, NPArray* invalidProductIds);
typedef void (*BuyProductNativeCallback)(NPError error);
typedef void (*TransactionStateChangeNativeCallback)(void* transactionsPtr, int length);
typedef void (*RestorePurchasesNativeCallback)(void* transactionsPtr, int length, NPError error);

// utility methods

void* NPCreateProductsDataArray(NSArray<BillingProduct*>* array, int* length);
void* NPCreateTransactionDataArray(NSArray<BillingTransaction*>* array, int* length);
void ReleaseProductsDataArray(void* array, int length);
void ReleaseTransactionsDataArray(void* array, int length);

void* NPCreateOfferData(BillingProductOffer* offer);
NPArrayWrapper NPCreateOffersDataArray(NSArray<BillingProductOffer*>* array);
NPArrayWrapper NPCreateOfferPricingPhasesDataArray(NSArray<BillingProductOfferPricingPhase*>* array);


typedef enum : NSInteger
{
    NPStoreReceiptVerificationStateNotChecked,
    NPStoreReceiptVerificationStateSuccess,
    NPStoreReceiptVerificationStateFailed
} NPStoreReceiptVerificationState;

struct SKPriceData
{
    double              value;
    void*               currencyCodePtr;
    void*               currencySymbolPtr;
    void*               localizedPricePtr;
};
typedef struct SKPriceData SKPriceData;


// blittable product object
struct SKProductData
{
    // properties
    void*               nativeObjectPtr;
    void*               identifierPtr;
    void*               localizedTitlePtr;
    void*               localizedDescriptionPtr;
    SKPriceData         price;
    void*               subscriptionInfoPtr;
    NPArrayWrapper      offersArray;
    
    // constructors
    ~SKProductData();
    
    // methods
    void CopyProperties(BillingProduct* product);
};
typedef struct SKProductData SKProductData;


// blittable transaction object
struct SKPaymentTransactionData
{
    void*                           nativeObjectPtr;
    void*                           identifierPtr;
    void*                           datePtr;
    int                             transactionState;
    void*                           receiptPtr;
    int                             environment;
    void*                           applicationBundleIdentifierPtr;
    void*                           productIdentifierPtr;
    int                             productType;
    int                             requestedQuantity;
    int                             purchasedQuantity;
    void*                           revocationDatePtr;
    int                             revocationReason;
    void*                           purchaseTagPtr;
    
    void*                           subscriptionStatusDataPtr;
    void*                           rawDataPtr;
    NPError                         errorData;

    // constructors
    ~SKPaymentTransactionData();

    // methods
    void CopyProperties(BillingTransaction* transaction);
};
typedef struct SKPaymentTransactionData SKPaymentTransactionData;

typedef enum : NSInteger
{
    Day,
    Week,
    Month,
    Year
} SKPeriodUnit;


struct SKBillingPeriodData
{
    double  duration;
    int     unit;
};
typedef struct SKBillingPeriodData SKBillingPeriodData;


struct SKProductSubscriptionInfoData
{
    // properties
    void*               nativeObjectPtr;
    void*               groupIdentifierPtr;
    void*               localizedGroupTitlePtr;
    int                 level;
    SKBillingPeriodData period;

    // constructors
    ~SKProductSubscriptionInfoData();
    
    // methods
    void CopyProperties(BillingProductSubscriptionInfo* subscriptionInfo);
};
typedef struct SKProductSubscriptionInfoData SKProductSubscriptionInfoData;


struct SKProductOfferData
{
    // properties
    void*               nativeObjectPtr;
    void*               identifierPtr;
    int                 category;
    NPArrayWrapper      pricingPhasesArray;
    
    // constructors
    ~SKProductOfferData();
    
    // methods
    void CopyProperties(BillingProductOffer* offer);
};
typedef struct SKProductOfferData SKProductOfferData;

struct SKProductOfferPricingPhaseData
{
    // properties
    int                 paymentMode;
    SKPriceData         price;
    SKBillingPeriodData period;
    int                 repeatCount;
    
    // constructors
    ~SKProductOfferPricingPhaseData();
    
    // methods
    void CopyProperties(BillingProductOfferPricingPhase* pricingPhase);
};
typedef struct SKProductOfferPricingPhaseData SKProductOfferPricingPhaseData;

struct SKProductSubscriptionStatusData
{
    // properties
    void*               groupIdentifierPtr;
    void*               renewalInfoPtr;
    void*               expirationDatePtr;
    int                 isUpgraded;
    void*               appliedOfferIdentifier;
    int                 appliedOfferType;
    
    // constructors
    ~SKProductSubscriptionStatusData();
    
    // methods
    void CopyProperties(BillingProductSubscriptionStatus* subscriptionStatus);
};
typedef struct SKProductSubscriptionStatusData SKProductSubscriptionStatusData;


struct SKProductSubscriptionRenewalInfoData
{
    // properties
    int                 state;
    void*               applicableOfferIdentifierPtr;
    int                 applicableOfferCategory;
    void*               lastRenewedDatePtr;
    void*               lastRenewalIdPtr;
    int                 isAutoRenewEnabled;
    int                 expirationReason;
    void*               renewalDatePtr;
    void*               gracePeriodExpirationDatePtr;
    int                 priceIncreaseStatus;
    
    // constructors
    ~SKProductSubscriptionRenewalInfoData();
    
    // methods
    void CopyProperties(BillingProductSubscriptionRenewalInfo* renewalInfo);
};
typedef struct SKProductSubscriptionRenewalInfoData SKProductSubscriptionRenewalInfoData;

struct SKProductOfferRedeemDetailsData
{
    const char* offerIdPtr;
    const char* keyIdPtr;
    const char* noncePtr;
    const char* signaturePtr;
    long        timestamp;
};
typedef struct SKProductOfferRedeemDetailsData SKProductOfferRedeemDetailsData;

struct SKProductBuyOptionsData
{
    // properties
    int                                 quantity;
    const char*                         tagPtr;
    SKProductOfferRedeemDetailsData     offerReddemDetails;
};
typedef struct SKProductBuyOptionsData SKProductBuyOptionsData;
