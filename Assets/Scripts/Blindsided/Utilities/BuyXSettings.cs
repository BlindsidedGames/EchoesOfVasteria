using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.SaveData.TextColourStrings;
using static Blindsided.SaveData.SaveData;

namespace Blindsided.Utilities
{
    public class BuyXSettings : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text buttonText;

        [SerializeField] private Button ExtraBuyOptionsButton;
        [SerializeField] private TMP_Text ExtraBuyOptionsText;

        [SerializeField] private Button RoundedBulkBuyButton;
        [SerializeField] private TMP_Text RoundedBulkBuyText;


        private void Start()
        {
            button.onClick.AddListener(SetButton);
            LoadState();
            ExtraBuyOptionsButton.onClick.AddListener(SetExtraBuyOptions);
            RoundedBulkBuyButton.onClick.AddListener(SetRoundedBulkBuy);
            SetExtraBuyOptionsText();
            SetRoundedBulkBuyText();
        }

        private void SetExtraBuyOptions()
        {
            ExtraBuyOptions = !ExtraBuyOptions;
            SetExtraBuyOptionsText();
        }

        private void SetExtraBuyOptionsText()
        {
            ExtraBuyOptionsText.text = ExtraBuyOptions ? "On" : "Off";
        }

        private void SetRoundedBulkBuy()
        {
            RoundedBulkBuy = !RoundedBulkBuy;
            SetRoundedBulkBuyText();
        }

        private void SetRoundedBulkBuyText()
        {
            RoundedBulkBuyText.text = RoundedBulkBuy ? "On" : "Off";
        }

        private void LoadState()
        {
            switch (PurchaseMode)
            {
                case BuyMode.Buy1:
                    buttonText.text = $"{ColourOrange}1";
                    break;
                case BuyMode.Buy10:
                    buttonText.text = $"{ColourOrange}10";
                    break;
                case BuyMode.Buy50:
                    buttonText.text = $"{ColourOrange}50";
                    break;
                case BuyMode.Buy100:
                    buttonText.text = $"{ColourOrange}100";
                    break;
                case BuyMode.BuyMax:
                    buttonText.text = $"{ColourOrange}Max";
                    break;
            }

            EventHandler.UpdateUi();
        }

        public void SetButton()
        {
            switch (PurchaseMode)
            {
                case BuyMode.Buy1:
                    var bm = ExtraBuyOptions ? BuyMode.Buy10 : BuyMode.Buy50;
                    var s = ExtraBuyOptions ? $"{ColourOrange}10" : $"{ColourOrange}50";
                    SetMode(bm, s);
                    break;
                case BuyMode.Buy10:
                    SetMode(BuyMode.Buy50, $"{ColourOrange}50");
                    break;
                case BuyMode.Buy50:
                    var bm50 = ExtraBuyOptions ? BuyMode.Buy100 : BuyMode.BuyMax;
                    var s50 = ExtraBuyOptions ? $"{ColourOrange}100" : $"{ColourOrange}Max";
                    SetMode(bm50, s50);
                    break;
                case BuyMode.Buy100:
                    SetMode(BuyMode.BuyMax, $"{ColourOrange}Max");
                    break;
                case BuyMode.BuyMax:
                    SetMode(BuyMode.Buy1, $"{ColourOrange}1");
                    break;
            }
        }

        private void SetMode(BuyMode m, string s)
        {
            PurchaseMode = m;
            buttonText.text = s;
            EventHandler.UpdateUi();
        }
    }
}