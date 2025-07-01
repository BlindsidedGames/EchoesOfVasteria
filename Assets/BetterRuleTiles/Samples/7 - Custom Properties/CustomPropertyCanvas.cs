using UnityEngine;
using UnityEngine.UI;

namespace VinTools.BetterRuleTiles.Sample
{
    public class CustomPropertyCanvas : MonoBehaviour
    {
        public CustomPropertyTest propertyTest;
        public Text valueText;

        private void OnEnable()
        {
            if (valueText == null) valueText = FindFirstObjectByType<Text>();

            if (propertyTest == null) propertyTest = FindFirstObjectByType<CustomPropertyTest>();
            if (propertyTest != null) propertyTest.onReadValue += DisplayValue;
        }

        private void OnDisable()
        {
            if (propertyTest != null) propertyTest.onReadValue -= DisplayValue;
        }

        private void DisplayValue(string msg)
        {
            valueText.text = "Move your cursor over the tile to see it's values.\n\n" + msg;
        }
    }
}