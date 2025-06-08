using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// • Holds up to 5 heroes in a list  
/// • Hot-keys 1-5 swap active hero  
/// • Non-active heroes follow the active one  
/// • Updates UI health / XP bars & active border  
/// • Cinemachine vCam follows the active hero
/// </summary>
public class PartyManager : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private List<GameObject> heroes = new(5);

    [Header("UI")]
    [SerializeField] private List<CharacterCardReferences> cards = new(5);

    [Header("Camera")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    // If using Cinemachine 3 Core, swap to: CinemachineCamera

    private int activeIdx = 0;

    private void Start()
    {
        if (heroes.Count == 0) { enabled = false; return; }

        // Hook health + XP events
        for (int i = 0; i < heroes.Count; i++)
        {
            int idx = i;

            var hp  = heroes[i].GetComponent<Health>();
            var lvl = heroes[i].GetComponent<LevelSystem>();

            hp.OnHealthChanged  += (cur, max)        => UpdateCardHP(idx, cur, max);
            lvl.OnXPChanged     += (cur, need)       => UpdateCardXP(idx, cur, need);

            UpdateCardHP(idx,  hp.CurrentHP, hp.MaxHP);
            UpdateCardXP(idx,  lvl.CurrentXP,  lvl.XPNeeded);
        }

        SetActiveHero(0);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetActiveHero(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetActiveHero(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetActiveHero(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetActiveHero(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetActiveHero(4);
    }

    /* ─────────────────────────────────────────────────────────────────────── */

    private void SetActiveHero(int index)
    {
        if (index < 0 || index >= heroes.Count) return;
        activeIdx = index;

        for (int i = 0; i < heroes.Count; i++)
        {
            bool isActive = (i == activeIdx);

            heroes[i].GetComponent<PlayerMovement>().enabled = isActive;

            var follower = heroes[i].GetComponent<HeroFollower>();
            follower.enabled = !isActive;
            follower.target  = heroes[activeIdx].transform;

            heroes[i].GetComponent<BasicAttackTelegraphed>().IsPlayerControlled = isActive;

            // UI border on/off
            if (i < cards.Count && cards[i] != null)
                cards[i].ActiveHeroBoarder.SetActive(isActive);
        }

        if (cinemachineCamera != null)
            cinemachineCamera.Follow = heroes[activeIdx].transform;
    }

    private void UpdateCardHP(int idx, int cur, int max)
    {
        if (idx >= cards.Count || cards[idx] == null) return;

        cards[idx].HeroHealthFill.fillAmount = (float)cur / max;
        cards[idx].HeroHealthText.text       = $"{cur}/{max}";
    }

    private void UpdateCardXP(int idx, int cur, int need)
    {
        if (idx >= cards.Count || cards[idx] == null) return;

        cards[idx].HeroXpFill.fillAmount = need == 0 ? 1f : (float)cur / need;
    }
}
