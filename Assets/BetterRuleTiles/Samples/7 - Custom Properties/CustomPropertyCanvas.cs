using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VinTools.BetterRuleTiles.Sample
{
    public class CustomPropertyCanvas : MonoBehaviour
    {
        public CustomPropertyTest propertyTest;
        public Text valueText;

#if UNITY_2023_1_OR_NEWER
        private void OnEnable()
        {
            if (valueText == null) valueText = FindAnyObjectByType<Text>();

            if (propertyTest == null) propertyTest = FindAnyObjectByType<CustomPropertyTest>();
            if (propertyTest != null) propertyTest.onReadValue += DisplayValue;
        }
#else
        private void OnEnable()
        {
            if (valueText == null) valueText = FindObjectOfType<Text>();

            if (propertyTest == null) propertyTest = FindObjectOfType<CustomPropertyTest>();
            if (propertyTest != null) propertyTest.onReadValue += DisplayValue;
        }
#endif

        private void OnDisable()
        {
            if (propertyTest != null) propertyTest.onReadValue -= DisplayValue;
        }

        void DisplayValue(string msg)
        {
            valueText.text = "Move your cursor over the tile to see it's values.\n\n" + msg;
        }
    }
}