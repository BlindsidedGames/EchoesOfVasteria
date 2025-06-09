using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Keeps the five heroes, hot-key selection, stance toggling (Move | Hold),
///     and a single Character Card UI in sync.  Also snaps the camera anchor to
///     the active hero when you press Space.
/// </summary>
public class PartyManager : MonoBehaviour
{
    /* ───────── Inspector ───────── */

    [Header("Gameplay")] [SerializeField] private List<GameObject> heroes = new(5);

    [Header("UI – single card")] [SerializeField]
    private CharacterCardReferences card;

    [Header("Selection (auto-find if empty)")] [SerializeField]
    private SelectionController selector;

    [Header("Camera")]
    [Tooltip("The GameObject the CinemachineCamera follows (has CameraController).")]
    [SerializeField]
    private Transform cameraAnchor;

    /* ───────── Private ───────── */

    private int activeIdx;

    /* ───────── Unity lifecycle ───────── */

    private void Awake()
    {
        /* pip buttons */
        if (card && card.heroSelectionButtons != null)
            for (var i = 0; i < card.heroSelectionButtons.Length; i++)
            {
                var idx = i;
                if (card.heroSelectionButtons[i])
                    card.heroSelectionButtons[i].onClick.AddListener(() => SetActive(idx));
            }

        /* stance button */
        if (card && card.heroStanceButton)
            card.heroStanceButton.onClick.AddListener(CycleActiveHeroStance);
    }

    private void Start()
    {
        if (heroes.Count == 0 || card == null)
        {
            enabled = false;
            return;
        }

        /* hook HP / XP events once */
        for (var i = 0; i < heroes.Count; i++)
        {
            if (!heroes[i]) continue;

            var idx = i;
            var hp = heroes[i].GetComponent<Health>();
            var lv = heroes[i].GetComponent<LevelSystem>();

            hp.OnHealthChanged += (cur, max) => UpdateHP(idx, cur, max);
            lv.OnXPChanged += (cur, need) => UpdateXP(idx, cur, need);
        }

        SetActive(0); // default hero
        ApplyStances(); // initial behaviour
    }

    private void Update()
    {
        /* hero hot-keys */
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetActive(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetActive(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetActive(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetActive(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetActive(4);

        /* stance toggle hot-key (T) */
        if (Input.GetKeyDown(KeyCode.T)) CycleActiveHeroStance();

        /* camera snap hot-key (Space) */
        if (Input.GetKeyDown(KeyCode.Space)) SnapCameraToActiveHero();
    }

    /* ───────── Selection ───────── */

    public void NotifyHotSwap(GameObject heroGO)
    {
        var idx = heroes.IndexOf(heroGO);
        if (idx >= 0) SetActive(idx);
    }

    private void SetActive(int index)
    {
        if (!IsValidIndex(index)) return;
        activeIdx = index;

        /* make SelectionController drive right-click movement */
        if (selector) selector.Select(heroes[index]);

        /* visuals */
        for (var i = 0; i < heroes.Count; i++)
        {
            var on = i == activeIdx;
            if (!heroes[i]) continue;

            if (heroes[i].TryGetComponent(out BasicAttackTelegraphed atk))
                atk.IsPlayerControlled = on;

            if (heroes[i].TryGetComponent(out HeroClickMover mover))
                mover.SetSelected(on);
        }

        RefreshCardVisuals(index);
    }

    /* ───────── Stance ───────── */

    private void CycleActiveHeroStance()
    {
        if (!IsValidIndex(activeIdx)) return;

        heroes[activeIdx].GetComponent<HeroStance>().Cycle();
        ApplyStances();
        RefreshCardVisuals(activeIdx);
    }

    private void ApplyStances()
    {
        foreach (var h in heroes)
        {
            if (!h) continue;

            var mover = h.GetComponent<HeroClickMover>();
            var hs = h.GetComponent<HeroStance>();

            switch (hs.CurrentStance)
            {
                case HeroStance.Stance.Move:
                    mover.ResumePersonalTarget();
                    mover.SetHold(false);
                    break;

                case HeroStance.Stance.Hold:
                    mover.SetHold(true);
                    break;
            }
        }
    }

    /* ───────── Camera snap ───────── */

    private void SnapCameraToActiveHero()
    {
        if (!IsValidIndex(activeIdx) || cameraAnchor == null) return;

        var heroPos = heroes[activeIdx].transform.position;
        cameraAnchor.position = new Vector3(heroPos.x, heroPos.y, cameraAnchor.position.z);
    }

    /* ───────── UI helpers ───────── */

    private void RefreshCardVisuals(int idx)
    {
        if (!IsValidIndex(idx) || card == null) return;

        var hero = heroes[idx];

        /* name / icon */
        if (card.heroNameText) card.heroNameText.text = hero.name;
        if (card.heroIcon && hero.TryGetComponent(out SpriteRenderer sr))
            card.heroIcon.sprite = sr.sprite;

        /* stance text */
        if (card.heroStanceText && hero.TryGetComponent(out HeroStance hs))
            card.heroStanceText.text = hs.CurrentStance == HeroStance.Stance.Move
                ? "<b>Move</b> | Hold"
                : "Move | <b>Hold</b>";

        /* green pips */
        if (card.heroSelectionPips != null)
            for (var i = 0; i < card.heroSelectionPips.Length; i++)
                if (card.heroSelectionPips[i])
                    card.heroSelectionPips[i].SetActive(i == idx);

        /* HP / XP immediate refresh */
        var hp = hero.GetComponent<Health>();
        var lv = hero.GetComponent<LevelSystem>();
        UpdateHP(idx, hp.CurrentHP, hp.MaxHP);
        UpdateXP(idx, lv.CurrentXP, lv.XPNeeded);
    }

    private void UpdateHP(int idx, int cur, int max)
    {
        if (idx != activeIdx || card == null) return;
        card.healthBarFill.fillAmount = (float)cur / max;
        if (card.healthBarText) card.healthBarText.text = $"{cur}/{max}";
    }

    private void UpdateXP(int idx, int cur, int need)
    {
        if (idx != activeIdx || card == null) return;
        card.xpBarFill.fillAmount = need == 0 ? 1 : (float)cur / need;
        if (card.xpBarText) card.xpBarText.text = $"{cur}/{need}";
    }

    /* ───────── helpers ───────── */

    private bool IsValidIndex(int i)
    {
        return i >= 0 && i < heroes.Count && heroes[i];
    }
}