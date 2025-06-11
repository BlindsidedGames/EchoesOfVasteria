#if UNITY_IOS || UNITY_ANDROID
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelBusters.CoreLibrary;
using VoxelBusters.EssentialKit;

public class IAPManager : MonoBehaviour
{
    #region Inspector Fields -------------------------------------------------

    [FormerlySerializedAs("productID")]
    [Header("Product IDs")]
    [Tooltip("Your product’s platform-specific ID (as set up in App Store / Play Console)")]
    public string devOptsID;

    [Tooltip("Product ID for Infinite Offline-Time upgrade")]
    public string infiniteOTProductID;

    // Query-able flag for other classes
    public static bool InfiniteOTPurchased { get; private set; }

    [Header("UI References")] [SerializeField]
    private GameObject devOpts;

    [SerializeField] private GameObject devOptsPurchase;
    [SerializeField] private GameObject iOTPurchaseButton;

    [FormerlySerializedAs("productPriceText")] [SerializeField]
    private TMP_Text devOptsPriceText;

    public TMP_Text infiniteOTPriceText;
    public TMP_Text offlineTimeText;

    #endregion

    #region Private state ----------------------------------------------------

    private IBillingProduct _devOpts; // Primary non-consumable
    private IBillingProduct _infiniteOTProduct; // Infinite offline-time

    #endregion

    #region Unity lifecycle --------------------------------------------------

    private void OnEnable()
    {
        // 1. Subscriptions ---------------------------------------------------
        BillingServices.OnInitializeStoreComplete += OnInitializeStoreComplete;
        BillingServices.OnTransactionStateChange += OnTransactionStateChange;
        BillingServices.OnRestorePurchasesComplete += OnRestorePurchasesComplete;

        // 2. Kick off store init once we’re listening ------------------------
        if (BillingServices.IsAvailable())
            BillingServices.InitializeStore();
        else
            Debug.LogWarning("Billing not available on this platform.");
    }

    private void OnDisable()
    {
        BillingServices.OnInitializeStoreComplete -= OnInitializeStoreComplete;
        BillingServices.OnTransactionStateChange -= OnTransactionStateChange;
        BillingServices.OnRestorePurchasesComplete -= OnRestorePurchasesComplete;
    }

    #endregion

    #region Store callbacks --------------------------------------------------

    private void OnInitializeStoreComplete(BillingServicesInitializeStoreResult result, Error error)
    {
        if (error != null)
        {
            Debug.LogError($"Store initialization failed: {error}");
            return;
        }

        _devOpts = BillingServices.GetProductWithId(devOptsID);
        _infiniteOTProduct = BillingServices.GetProductWithId(infiniteOTProductID);

        if (_devOpts != null && devOptsPriceText != null)
            devOptsPriceText.text = _devOpts.Price.LocalizedText;
        else
            Debug.LogError($"Product '{devOptsID}' not returned from store.");
        if (_infiniteOTProduct != null && infiniteOTPriceText != null)
            infiniteOTPriceText.text = _infiniteOTProduct.Price.LocalizedText;
        else
            Debug.LogError($"Product '{infiniteOTProductID}' not returned from store.");

        // Reflect prev-purchased state --------------------------------------
        SetDevOptsActive(BillingServices.IsProductPurchased(devOptsID));
        InfiniteOfflineActivate(BillingServices.IsProductPurchased(infiniteOTProductID));
    }

    private void OnTransactionStateChange(BillingServicesTransactionStateChangeResult result)
    {
        foreach (var txn in result.Transactions)
            switch (txn.TransactionState)
            {
                case BillingTransactionState.Purchased: // covers fresh + family-share receipts
                    HandlePurchase(txn.Product.Id);
                    break;

                case BillingTransactionState.Failed:
                    Debug.LogError($"Purchase failed for {txn.Product.Id}: {txn.Error}");
                    break;
            }
    }

    private void OnRestorePurchasesComplete(BillingServicesRestorePurchasesResult result, Error error)
    {
        if (error != null)
        {
            Debug.LogError($"Restore failed: {error}");
            return;
        }

        Debug.Log($"Restore complete. {result.Transactions.Length} transactions restored.");
        foreach (var txn in result.Transactions) HandlePurchase(txn.Product.Id);
    }

    #endregion

    #region Public API -------------------------------------------------------

    /// <summary>Initiate purchase flow for the primary product.</summary>
    public void PurchaseProduct()
    {
        if (_devOpts == null)
        {
            Debug.LogError("Product not initialized or not available.");
            return;
        }

        if (!BillingServices.CanMakePayments())
        {
            Debug.LogWarning("Purchases are not allowed on this device.");
            return;
        }

        BillingServices.BuyProduct(_devOpts, null); // disambiguate overload
    }

    /// <summary>Initiate purchase flow for the Infinite OT upgrade.</summary>
    public void PurchaseInfiniteOT()
    {
        if (_infiniteOTProduct == null)
        {
            Debug.LogError("Infinite OT product not initialized.");
            return;
        }

        BillingServices.BuyProduct(_infiniteOTProduct, null);
    }

    /// <summary>Restore non-consumables and subscriptions.</summary>
    public void RestorePurchases()
    {
        BillingServices.RestorePurchases(false);
    }

    #endregion

    #region Helpers ----------------------------------------------------------

    private void HandlePurchase(string purchasedId)
    {
        if (purchasedId.Equals(devOptsID))
        {
            Debug.Log($"Purchase successful: {devOptsID}");
            SetDevOptsActive(true);
        }
        else if (purchasedId.Equals(infiniteOTProductID))
        {
            Debug.Log($"Purchase successful: {infiniteOTProductID}");
            InfiniteOfflineActivate(true);
            if (iOTPurchaseButton) iOTPurchaseButton.SetActive(false);
        }
    }

    private void SetDevOptsActive(bool active)
    {
        if (devOpts) devOpts.SetActive(active);
        if (devOptsPurchase) devOptsPurchase.SetActive(!active);
    }

    private void InfiniteOfflineActivate(bool active)
    {
        InfiniteOTPurchased = active;
        iOTPurchaseButton.SetActive(!active);
        offlineTimeText.text = "<b>Time Gathered</b> | \u221e";
    }

    #endregion
}
#endif