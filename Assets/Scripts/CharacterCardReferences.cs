using MPUIKIT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Holds UI references for one hero card.</summary>
public class CharacterCardReferences : MonoBehaviour
{
    [Header("Card Visuals")] public TMP_Text heroNameText;
    public MPImage heroIcon;
    public TMP_Text heroDamageText;
    public TMP_Text heroDefenseText;
    public Button codexButton;

    [Header("Bars")] public MPImage healthBarFill;
    public TMP_Text healthBarText; // e.g., "100/100"
    public MPImage xpBarFill;
    public TMP_Text xpBarText; // e.g., "123/450"

    [Header("Party Selection")] public GameObject[] heroSelectionPips;
    public Button[] heroSelectionButtons;

    /// <summary>
    /// Updates the card's icon with the provided sprite.
    /// </summary>
    /// <param name="sprite">The sprite to display for the hero.</param>
    public void UpdateHeroIcon(Sprite sprite)
    {
        if (heroIcon) heroIcon.sprite = sprite;
    }
}
