//
//  NPBillingServicesBinding.mm
//  Native Plugins
//
//  Created by Ashwin kumar on 22/01/19.
//  Copyright (c) 2019 Voxel Busters Interactive LLP. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>
#import "NPBillingServicesDataTypes.h"
#import "NPDefines.h"
#import "BillingServices/BillingServices-swift.h"

@interface NPBillingServicesObserver : NSObject<BillingServicesDelegate>

- (instancetype)initWithCallbacks :(RequestForProductsNativeCallback)requestForProductsCallback
    transactionStateChangeCallback: (TransactionStateChangeNativeCallback) transactionStateChangeCallback
          restorePurchasesCallback:(RestorePurchasesNativeCallback) restorePurchasesCallback;
@property (nonatomic, assign) RequestForProductsNativeCallback requestForProductsCallback;
@property (nonatomic, assign) TransactionStateChangeNativeCallback transactionStateChangeCallback;
@property (nonatomic, assign) RestorePurchasesNativeCallback restorePurchasesCallback;
@end

@implementation NPBillingServicesObserver

- (instancetype)initWithCallbacks :(RequestForProductsNativeCallback)requestForProductsCallback
    transactionStateChangeCallback: (TransactionStateChangeNativeCallback) transactionStateChangeCallback
          restorePurchasesCallback:(RestorePurchasesNativeCallback) restorePurchasesCallback {
    self = [super init];
    
    self.requestForProductsCallback = requestForProductsCallback;
    self.transactionStateChangeCallback = transactionStateChangeCallback;
    self.restorePurchasesCallback = restorePurchasesCallback;
    
    return self;
}

- (void)didInitializeStoreCompleteWithResult:(BillingServicesInitializeStoreResult * _Nonnull)result error:(NSError * _Nullable)error {
    
    // Extracting product ids from BillingProductDefinition
    NSMutableArray *invalidProductIds = [NSMutableArray array];
    [result.invalidProductDefinitions enumerateObjectsUsingBlock:^(BillingProductDefinition* obj, NSUInteger idx, BOOL *stop) {
         [invalidProductIds addObject:obj.identifier];
    }];
            
    int         dataArrayLength = 0;
    void*       dataArray       = NPCreateProductsDataArray(result.products, &dataArrayLength);
    NPArray*    invalidIdArray  = NPCreateArrayOfCString(invalidProductIds);
    self.requestForProductsCallback(dataArray, dataArrayLength, NPCreateError(error), invalidIdArray);
}

- (void)didRestorePurchasesCompleteWithResult:(BillingServicesRestorePurchasesResult * _Nonnull)result error:(NSError * _Nullable)error { 
    int     cArrayLength;
    void*   cArray          = NPCreateTransactionDataArray(result.transactions, &cArrayLength);

    self.restorePurchasesCallback(cArray, cArrayLength, NPCreateError(error));
}

- (void)didTransactionStateChangeWithResult:(BillingServicesTransactionStateChangeResult * _Nonnull)result {
    
    int     cArrayLength;
    void*   cArray          = NPCreateTransactionDataArray(result.transactions, &cArrayLength);
    
    self.transactionStateChangeCallback(cArray, cArrayLength);
}

@end

#pragma mark - Native binding methods
BillingServicesStoreKitImplementation *implementation;
NPBillingServicesObserver *observer;

NPBINDING DONTSTRIP bool NPBillingServicesCanMakePayments()
{
    return [implementation canMakePayments];
}

NPBINDING DONTSTRIP void NPBillingServicesRegisterCallbacks(RequestForProductsNativeCallback requestForProductsCallback,
                                                            TransactionStateChangeNativeCallback transactionStateChangeCallback,
                                                            RestorePurchasesNativeCallback restorePurchasesCallback
                                                            )
{
    observer = [[NPBillingServicesObserver alloc] initWithCallbacks:requestForProductsCallback
                                     transactionStateChangeCallback:transactionStateChangeCallback
                                           restorePurchasesCallback:restorePurchasesCallback];
}

NPBINDING DONTSTRIP void NPBillingServicesInit(bool autoFinishTransactions)
{
    BillingServicesSettings *settings = [[BillingServicesSettings alloc] initWithAutoFinishTransactions:autoFinishTransactions];
    implementation = [[BillingServicesStoreKitImplementation alloc] initWithSettings:settings delegate: observer];
}

NPBINDING DONTSTRIP void NPBillingServicesRequestForBillingProducts(const char** productIds, int length)
{
    NSArray<NSString*>* productIdArray  = NPCreateArrayOfNSString(productIds, length);
    NSMutableArray<BillingProductDefinition *> *definitions = [[NSMutableArray<BillingProductDefinition*> alloc] init];
    
    for (NSString *each in productIdArray) {
        BillingProductDefinition *definition = [[BillingProductDefinition alloc] initWithIdentifier:each];
        [definitions addObject:definition];
    }
    
    [implementation initializeStore:definitions];
}

NPBINDING DONTSTRIP BOOL NPBillingServicesIsProductPurchased(const char* productId)
{
    return [implementation isProductPurchased:NPCreateNSStringFromCString(productId)];
}


NPBINDING DONTSTRIP void NPBillingServicesBuyProduct(const char* productId, SKProductBuyOptionsData buyOptions)
{
    BillingProductOfferRedeemDetails *offerRedeemDetails = nil;
    NSUUID *tagUUID = nil;
    
    if(buyOptions.tagPtr != nil) {
        NSString *tagStr    = NPCreateNSStringFromCString(buyOptions.tagPtr);
        tagUUID     = [[NSUUID alloc] initWithUUIDString:tagStr];
    }
    
    if(buyOptions.offerReddemDetails.offerIdPtr != nil) {
        
        if(buyOptions.offerReddemDetails.keyIdPtr == nil || buyOptions.offerReddemDetails.noncePtr == nil || buyOptions.offerReddemDetails.signaturePtr == nil) {
            NSLog(@"[Warning] Required parameters for offer details are not provided. Not applying this offer.");
        }
        else
        {
            offerRedeemDetails = [[BillingProductOfferRedeemDetails alloc]   initWithOfferId:NPCreateNSStringFromCString(buyOptions.offerReddemDetails.offerIdPtr)
                                                                                       keyId:NPCreateNSStringFromCString(buyOptions.offerReddemDetails.keyIdPtr)
                                                                                       nonce:[[NSUUID alloc] initWithUUIDString:NPCreateNSStringFromCString(buyOptions.offerReddemDetails.noncePtr)]
                                                                                   signature:[[NSData alloc] initWithBase64EncodedString:NPCreateNSStringFromCString(buyOptions.offerReddemDetails.signaturePtr) options:0]
                                                                                   timestamp:buyOptions.offerReddemDetails.timestamp];
        }
    }
    
    BuyProductOptions *options = [[BuyProductOptions alloc] initWithQuantity:buyOptions.quantity tag:tagUUID offerRedeemDetails:offerRedeemDetails];
    
    NSError *error = [implementation buyProduct:NPCreateNSStringFromCString(productId) :options];
    
    if (error != nil) {
        NSLog(@"BuyProduct: %@", [error localizedDescription]);
    }
}

NPBINDING DONTSTRIP void* NPBillingServicesGetTransactions(int* length)
{
    
    NSArray<BillingTransaction*> *transactions    = [implementation getUnfinishedTransactions];
    
    // convert native object to blittable type
    return NPCreateTransactionDataArray(transactions, length);
}

NPBINDING DONTSTRIP void NPBillingServicesRestorePurchases(bool forceRefresh, const char* tag)
{
    [implementation restorePurchases:forceRefresh :NPCreateNSStringFromCString(tag)];
}

NPBINDING DONTSTRIP void NPBillingServicesFinishTransactions(void** transactionsPtr, int length)
{
    // create native transaction object array
    for (int iter = 0; iter < length; iter++)
    {
        BillingTransaction*   transaction = (__bridge BillingTransaction*)transactionsPtr[iter];
        [transaction finish];
    }
}

NPBINDING DONTSTRIP bool NPBillingServicesTryClearingUnfinishedTransactions() //Can remove passing bool as it's not required
{
    [implementation tryClearingUnfinishedTransactions];
    return true;
}
