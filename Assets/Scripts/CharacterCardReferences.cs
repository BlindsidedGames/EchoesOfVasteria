using MPUIKIT;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Holds UI references for one hero card.</summary>
public class CharacterCardReferences : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject ActiveHeroBoarder;
    public MPImage      HeroHealthFill;
    public TMP_Text   HeroHealthText;
    public MPImage      HeroXpFill;        // blue XP bar fill
}