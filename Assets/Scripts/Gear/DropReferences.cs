using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.Gear
{
    public class DropReferences : MonoBehaviour
    {
        public MPImageBasic rarityImage; //Use rarityImage.OutlineColor to set the outline color based on rarity
        public Image iconImage;
        public TMP_Text nameText;
        public TMP_Text statsText;
        public Button equipButton; //equips item to currently selected hero
        public Button dismantleButton; //dismantles item
        public MPImageBasic timerFillBar;
    }
}