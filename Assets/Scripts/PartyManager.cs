using System.Collections.Generic;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    [Header("Gameplay")] [SerializeField] private List<GameObject> heroes = new(5);

    [Header("UI")] [SerializeField] private List<CharacterCardReferences> cards = new(5);

    [Header("Selection (auto-find if empty)")] [SerializeField]
    private SelectionController selector;

    private int activeIdx;

    /* ─── Unity ─── */

    private void Start()
    {
        if (heroes.Count == 0)
        {
            enabled = false;
            return;
        }

        for (var i = 0; i < heroes.Count; i++)
        {
            if (!heroes[i]) continue;

            var idx = i;
            var hp = heroes[i].GetComponent<Health>();
            var lv = heroes[i].GetComponent<LevelSystem>();

            hp.OnHealthChanged += (c, m) => UpdateHP(idx, c, m);
            lv.OnXPChanged += (c, n) => UpdateXP(idx, c, n);

            UpdateHP(idx, hp.CurrentHP, hp.MaxHP);
            UpdateXP(idx, lv.CurrentXP, lv.XPNeeded);
        }

        SetActive(0); // default hero 0 selected
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetActive(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetActive(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetActive(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetActive(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetActive(4);
    }

    /* ─── called by SelectionController when you LEFT-click a hero ─── */
    public void NotifyHotSwap(GameObject heroGO)
    {
        var idx = heroes.IndexOf(heroGO);
        if (idx >= 0) SetActive(idx);
    }

    /* ─── core hot-key logic ─── */
    private void SetActive(int index)
    {
        if (index < 0 || index >= heroes.Count || !heroes[index]) return;
        activeIdx = index;

        // 🔑 tell SelectionController FIRST so right-click works instantly
        if (selector) selector.Select(heroes[activeIdx]);

        // then update flags & UI
        for (var i = 0; i < heroes.Count; i++)
        {
            var on = i == activeIdx;
            if (!heroes[i]) continue;

            heroes[i].GetComponent<BasicAttackTelegraphed>().IsPlayerControlled = on;

            if (heroes[i].TryGetComponent(out HeroClickMover mover))
                mover.SetSelected(on);

            if (i < cards.Count && cards[i])
                cards[i].ActiveHeroBoarder.SetActive(on);
        }
    }

    /* ─── UI helpers ─── */
    private void UpdateHP(int idx, int cur, int max)
    {
        if (idx >= cards.Count || !cards[idx]) return;
        cards[idx].HeroHealthFill.fillAmount = (float)cur / max;
        cards[idx].HeroHealthText.text = $"{cur}/{max}";
    }

    private void UpdateXP(int idx, int cur, int need)
    {
        if (idx >= cards.Count || !cards[idx]) return;
        cards[idx].HeroXpFill.fillAmount = need == 0 ? 1 : (float)cur / need;
    }
}