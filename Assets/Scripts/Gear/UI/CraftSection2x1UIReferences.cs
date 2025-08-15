using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear.UI
{
    /// <summary>
    /// Reference holder for a 2x1 craft section UI.
    /// </summary>
    public class CraftSection2x1UIReferences : MonoBehaviour
    {
        public ImageReference craftArrow;
        public Sprite invalidArrow;
        public Sprite validArrow;
        public Image resultImage;
        public TMP_Text resultText;
        public Image cost1Image;
        public TMP_Text cost1Text;
        public Image cost2Image;
        public TMP_Text cost2Text;
        public TMP_Text maxCraftsText;
        public Button craftButton;
        public Button craftAllButton;
    }
}
