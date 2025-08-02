using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimelessEchoes.UI
{
    [Serializable]
    public class SaveSlotReferences
    {
        public int slot;
        public Button saveDeleteButton;
        public TMP_Text saveDeleteText;
        public Button loadButton;
        public Button toggleDeleteButton;
        public TMP_Text fileNameText;
        public TMP_Text playtimeText;
        public TMP_Text lastPlayedText;

        [HideInInspector] public Image toggleDeleteImage;
        [HideInInspector] public bool deleteMode;
        [HideInInspector] public DateTime? lastPlayed;
    }
}

