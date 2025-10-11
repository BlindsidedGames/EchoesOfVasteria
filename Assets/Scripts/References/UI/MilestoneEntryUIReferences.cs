using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace References.UI
{
    /// <summary>
    /// Holds references for a milestone UI entry.
    /// </summary>
    public class MilestoneEntryUIReferences : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text passiveText;
        [SerializeField] private TMP_Text activeText;
        [SerializeField] private TMP_Text setText;
        [SerializeField] private Image setIcon;
        [SerializeField] private Image toggleImage;
        [SerializeField] private Button toggleButton;
        [SerializeField] private GameObject taskImageObject;
        [SerializeField] private Image taskImage;

        public TMP_Text NameText => nameText;
        public TMP_Text PassiveText => passiveText;
        public TMP_Text ActiveText => activeText;
        public TMP_Text SetText => setText;
        public Image SetIcon => setIcon;
        public Image ToggleImage => toggleImage;
        public Button ToggleButton => toggleButton;
        public GameObject TaskImageObject => taskImageObject;
        public Image TaskImage => taskImage;
    }
}
