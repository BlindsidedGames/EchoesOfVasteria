using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Blindsided.SaveData.StaticReferences;
using static Blindsided.SaveData.TextColourStrings;
using static Blindsided.SaveData.SaveData;

namespace Blindsided.Utilities
{
    public class NotationSetter : MonoBehaviour
    {
        public Button notationButton;
        public TMP_Text notationButtonText;

        private void Start()
        {
            notationButton.onClick.AddListener(SetButton);
            LoadState();
        }

        private void LoadState()
        {
            switch (Notation)
            {
                case NumberTypes.Standard:
                    notationButtonText.text = $"{ColourOrange}Standard";
                    break;
                case NumberTypes.Scientific:
                    notationButtonText.text = $"{ColourOrange}Scientific";
                    break;
                case NumberTypes.Engineering:
                    notationButtonText.text = $"{ColourOrange}Engineering";
                    break;
            }

            EventHandler.UpdateUi();
        }

        private void SetButton()
        {
            switch (Notation)
            {
                case NumberTypes.Standard:
                    SetMode(NumberTypes.Scientific, $"{ColourOrange}Scientific");
                    break;
                case NumberTypes.Scientific:
                    SetMode(NumberTypes.Engineering, $"{ColourOrange}Engineering");
                    break;
                case NumberTypes.Engineering:
                    SetMode(NumberTypes.Standard, $"{ColourOrange}Standard");
                    break;
            }
        }

        private void SetMode(NumberTypes mode, string text)
        {
            Notation = mode;
            notationButtonText.text = text;
            EventHandler.UpdateUi();
        }
    }
}