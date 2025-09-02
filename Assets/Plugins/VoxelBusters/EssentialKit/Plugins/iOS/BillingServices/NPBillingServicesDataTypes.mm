//
//  NPBillingServicesDataTypes.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import "NPBillingServicesDataTypes.h"

void SKProductData::CopyProperties(BillingProduct* product)
{
    SKProductSubscriptionInfoData*  subscriptionInfoData = nil;
    if (product.subscriptionInfo != nil)
    {
        subscriptionInfoData = (SKProductSubscriptionInfoData*)malloc(sizeof(SKProductSubscriptionInfoData));
        subscriptionInfoData->CopyProperties(product.subscriptionInfo);
    }
    
    this->nativeObjectPtr               = (__bridge void*)product;
    this->identifierPtr                 = NPCreateCStringFromNSString(product.identifier);
    this->localizedDescriptionPtr       = NPCreateCStringFromNSString(product.localizedDescription);
    this->localizedTitlePtr             = NPCreateCStringFromNSString(product.localizedTitle);
    this->price                         = (SKPriceData) {   .value = product.price.value,
                                                            .currencyCodePtr = NPCreateCStringFromNSString(product.price.currencyCode),
                                                            .currencySymbolPtr = NPCreateCStringFromNSString(product.price.currencySymbol),
                                                            .localizedPricePtr = NPCreateCStringFromNSString(product.price.localizedDisplay)
                                                        };
    
    this->subscriptionInfoPtr           = subscriptionInfoData;
    this->offersArray                   = NPCreateOffersDataArray(product.offers);
}

SKProductData::~SKProductData()
{
    // release c allocations
    if(subscriptionInfoPtr)
    {
        free(subscriptionInfoPtr);
    }
}

void SKPaymentTransactionData::CopyProperties(BillingTransaction* transaction)
{
    SKProductSubscriptionStatusData*  subscriptionStatusData = nil;
    if (transaction.subscriptionStatus != nil)
    {
        subscriptionStatusData = (SKProductSubscriptionStatusData*)malloc(sizeof(SKProductSubscriptionStatusData));
        subscriptionStatusData->CopyProperties(transaction.subscriptionStatus);
    }
    
    // copy values
    this->nativeObjectPtr                   = NPRetainWithOwnershipTransfer(transaction);
    this->identifierPtr                     = NPCreateCStringFromNSString(transaction.identifier);
    this->datePtr                           = NPCreateCStringFromNSString(NPCreateNSStringFromNSDate(transaction.date));
    this->transactionState                  = (int) transaction.state;
    this->receiptPtr                        = NPCreateCStringFromNSString(transaction.receipt);
    this->environment                       = (int)transaction.environment;
    this->applicationBundleIdentifierPtr    = NPCreateCStringFromNSString(transaction.applicationBundleIdentifier);
    this->productIdentifierPtr              = NPCreateCStringFromNSString(transaction.productIdentifier);
    this->productType                       = (int) transaction.productType;
    this->requestedQuantity                 = (int) transaction.requestedQuantity;
    this->purchasedQuantity                 = (int)transaction.purchasedQuantity;
    this->revocationDatePtr                 = NPCreateCStringFromNSString(NPCreateNSStringFromNSDate(transaction.revocationDate));
    this->revocationReason                  = (int) transaction.revocationReason;
    this->purchaseTagPtr                    = NPCreateCStringFromNSString(transaction.purchaseTag);
    this->subscriptionStatusDataPtr         = subscriptionStatusData;
    this->rawDataPtr                        = NPCreateCStringFromNSString(transaction.rawData);
    this->errorData                         = NPCreateError(transaction.error);
}


SKPaymentTransactionData::~SKPaymentTransactionData()
{
    // release c allocations
    free(subscriptionStatusDataPtr);
}

void SKProductSubscriptionInfoData::CopyProperties(BillingProductSubscriptionInfo* subscriptionInfo)
{
    this->nativeObjectPtr               = (__bridge void*)subscriptionInfo;
    this->groupIdentifierPtr            = NPCreateCStringFromNSString(subscriptionInfo.groupIdentifier);
    this->localizedGroupTitlePtr        = NPCreateCStringFromNSString(subscriptionInfo.groupDisplayName);
    this->level                         = (int)subscriptionInfo.level;
    
    // Period
    this->period = (SKBillingPeriodData) { .duration =  subscriptionInfo.period.duration, .unit = (int)subscriptionInfo.period.unit };
}

SKProductSubscriptionInfoData::~SKProductSubscriptionInfoData()
{
    // release c allocations
}


void SKProductSubscriptionStatusData::CopyProperties(BillingProductSubscriptionStatus* subscriptionStatus)
{
    
    SKProductSubscriptionRenewalInfoData*  renewalInfoData = nil;
    
    if(subscriptionStatus.renewalInfo != nil) {
        renewalInfoData = (SKProductSubscriptionRenewalInfoData*)malloc(sizeof(SKProductSubscriptionRenewalInfoData));
        renewalInfoData->CopyProperties(subscriptionStatus.renewalInfo);
    }
    
    this->groupIdentifierPtr            = NPCreateCStringFromNSString(subscriptionStatus.groupIdentifier);
    this->renewalInfoPtr = renewalInfoData;
    this->expirationDatePtr =  NPCreateCStringFromNSString(NPCreateNSStringFromNSDate(subscriptionStatus.expirationDate));
    this->isUpgraded = subscriptionStatus.isUpgraded;
    this->appliedOfferIdentifier = NPCreateCStringFromNSString(subscriptionStatus.appliedOfferIdentifier);
    this->appliedOfferType = (int)subscriptionStatus.appliedOfferType;
}

SKProductSubscriptionStatusData::~SKProductSubscriptionStatusData()
{
    // release c allocations
    if(renewalInfoPtr)
    {
        free(renewalInfoPtr);
    }
}


void SKProductSubscriptionRenewalInfoData::CopyProperties(BillingProductSubscriptionRenewalInfo* renewalInfo)
{
    
    this->state            = (int) renewalInfo.state;
    this->applicableOfferIdentifierPtr = NPCreateCStringFromNSString(renewalInfo.applicableOfferIdentifier);
    this->applicableOfferCategory = (int)renewalInfo.applicableOfferType;
    this->lastRenewedDatePtr = NPCreateCStringFromNSString(NPCreateNSStringFromNSDate(renewalInfo.lastRenewedDate));
    this->lastRenewalIdPtr = NPCreateCStringFromNSString(renewalInfo.lastRenewalId);
    this->isAutoRenewEnabled = (int) renewalInfo.isAutoRenewEnabled;
    this->expirationReason = (int) renewalInfo.expirationReason;
    this->renewalDatePtr = NPCreateCStringFromNSString( NPCreateNSStringFromNSDate(renewalInfo.renewalDate));
    this->gracePeriodExpirationDatePtr = NPCreateCStringFromNSString( NPCreateNSStringFromNSDate(renewalInfo.gracePeriodExpirationDate));
    this->priceIncreaseStatus = (int) renewalInfo.priceIncreaseStatus;
}

SKProductSubscriptionRenewalInfoData::~SKProductSubscriptionRenewalInfoData()
{
    // release c allocations
}

void SKProductOfferData::CopyProperties(BillingProductOffer* offer)
{
    this->identifierPtr = NPCreateCStringFromNSString(offer.identifier);
    this->category = (int)offer.category;
    this->pricingPhasesArray = NPCreateOfferPricingPhasesDataArray(offer.pricingPhases);
}

SKProductOfferData::~SKProductOfferData()
{
    // release c allocations
}

void SKProductOfferPricingPhaseData::CopyProperties(BillingProductOfferPricingPhase* pricingPhase)
{
    this->paymentMode   = (int) pricingPhase.paymentMode;
    this->price         = (SKPriceData) {   .value = pricingPhase.price.value,
                                                            .currencyCodePtr = NPCreateCStringFromNSString(pricingPhase.price.currencyCode),
                                                            .currencySymbolPtr = NPCreateCStringFromNSString(pricingPhase.price.currencySymbol),
                                                            .localizedPricePtr = NPCreateCStringFromNSString(pricingPhase.price.localizedDisplay)
                                                        };
    this->period = (SKBillingPeriodData) { .duration =  pricingPhase.period.duration, .unit = (int)pricingPhase.period.unit };
    this->repeatCount = (int) pricingPhase.repeatCount;
}

SKProductOfferPricingPhaseData::~SKProductOfferPricingPhaseData()
{
    // release c allocations
}


#pragma mark - Utility methods

void* NPCreateProductsDataArray(NSArray<BillingProduct*>* array, int* length)
{
    if (array)
    {
        // set length
        *length     = (int)[array count];
        
        // create data array
        SKProductData*      newDataArray    = (SKProductData*)calloc(*length, sizeof(SKProductData));
        for (int iter = 0; iter < *length; iter++)
        {
            BillingProduct* product         = [array objectAtIndex:iter];
            SKProductData*  newDataObject   = &newDataArray[iter];
            newDataObject->CopyProperties(product);
        }
        
        return newDataArray;
    }
    else
    {
        *length     = -1;
        
        return nil;
    }
}

void* NPCreateTransactionDataArray(NSArray<BillingTransaction*>* array, int* length)
{
    if (array)
    {
        // set length
        *length     = (int)[array count];
        
        // create data array
        SKPaymentTransactionData*       newDataArray    = (SKPaymentTransactionData*)calloc(*length, sizeof(SKPaymentTransactionData));
        for (int iter = 0; iter < *length ; iter++)
        {
            BillingTransaction*       transaction     = [array objectAtIndex:iter];
            SKPaymentTransactionData*   newDataObject   = &newDataArray[iter];
            newDataObject->CopyProperties(transaction);
        }
        
        return newDataArray;
    }
    else
    {
        *length     = -1;
        
        return nil;
    }
}

void* NPCreateOfferData(BillingProductOffer* offer)
{
    SKProductOfferData*  offerData   = (SKProductOfferData*)malloc(sizeof(SKProductOfferData));
    offerData->CopyProperties(offer);
    
    return offerData;
}

NPArrayWrapper NPCreateOffersDataArray(NSArray<BillingProductOffer*>* array)
{
    SKProductOfferData*      newDataArray = nil;
    int length = -1;
    
    if (array)
    {
        // set length
        length     = (int)[array count];
        
        // create data array
        newDataArray    = (SKProductOfferData*)calloc(length, sizeof(SKProductOfferData));
        for (int iter = 0; iter < length; iter++)
        {
            BillingProductOffer* offer         = [array objectAtIndex:iter];
            SKProductOfferData*  newDataObject   = &newDataArray[iter];
            newDataObject->CopyProperties(offer);
        }
    }
    
    return (NPArrayWrapper) {.ptr = newDataArray, .length = length};
}

NPArrayWrapper NPCreateOfferPricingPhasesDataArray(NSArray<BillingProductOfferPricingPhase*>* array)
{
    SKProductOfferPricingPhaseData*      newDataArray = nil;
    int length = -1;

    
    if (array)
    {
        // set length
        length     = (int)[array count];
        
        // create data array
        newDataArray    = (SKProductOfferPricingPhaseData*)calloc(length, sizeof(SKProductOfferPricingPhaseData));
        for (int iter = 0; iter < length; iter++)
        {
            BillingProductOfferPricingPhase* pricingPhase       = [array objectAtIndex:iter];
            SKProductOfferPricingPhaseData*  newDataObject      = &newDataArray[iter];
            newDataObject->CopyProperties(pricingPhase);
        }
    }
        
    return (NPArrayWrapper) {.ptr = newDataArray, .length = length};
}
