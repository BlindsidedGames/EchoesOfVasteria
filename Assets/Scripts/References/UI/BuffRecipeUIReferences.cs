using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace References.UI
{
    public class BuffRecipeUIReferences : MonoBehaviour
    {
        public Image iconImage;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text durationText;
        public Button purchaseButton;
        public CostResourceUIReferences costSlotPrefab;
        public GameObject costGridLayoutParent;
    }
}