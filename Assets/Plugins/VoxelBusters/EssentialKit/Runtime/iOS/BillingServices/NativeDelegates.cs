#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    internal delegate void RequestForProductsNativeCallback(IntPtr productsPtr, int length, NativeError error, ref NativeArray invalidProductIds);

    internal delegate void TransactionStateChangeNativeCallback(IntPtr transactionsPtr, int length);
    
    internal delegate void RestorePurchasesNativeCallback(IntPtr transactionsPtr, int length, NativeError error);
}
#endif