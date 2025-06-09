using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Holds UI references for one hero card.</summary>
public class CharacterCardReferences : MonoBehaviour
{
    [Header("Visuals")] public GameObject ActiveHeroBoarder;
    public MPImage HeroHealthFill;
    public TMP_Text HeroHealthText;
    public MPImage HeroXpFill; // blue XP bar fill

    [Header("New Visuals")] public TMP_Text heroNameText;
    public MPImage heroIcon;

    public TMP_Text heroDamageText;
    public TMP_Text heroDefenseText;

    public TMP_Text heroStanceText;
    public Button heroStanceButton;
    public Button codexButton;

    public MPImage xpBarFill;
    public TMP_Text xpBarText; // "XP: 123 / 456"
    public MPImage healthBarFill;
    public TMP_Text healthBarText; // "HP: 123 / 456"

    public GameObject[] heroSelectionPips;
    public Button[] heroSelectionButtons;
}