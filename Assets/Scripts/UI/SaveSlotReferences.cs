using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    public class SaveSlotReferences : MonoBehaviour
    {
        public Button saveDeleteButton;
        public Button loadButton;
        public TMP_Text loadDeleteText;
        public Button toggleSafetyButton;
        public TMP_Text fileNameText;
        public TMP_Text playtimeText;
        public TMP_Text lastPlayedText;

        [HideInInspector] public Image toggleDeleteImage;
        [HideInInspector] public bool deleteMode;
        public DateTime? lastPlayed;
    }
}