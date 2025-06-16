using System;
using System.Collections.Generic;
using UnityEngine;
using TimelessEchoes.Attacks;
using Gear;

public class PartyManager : MonoBehaviour
{
    /* ─── Inspector ─── */

    [Header("Gameplay")] [SerializeField] private List<GameObject> heroes = new(5);

    [Header("UI – single card")] [SerializeField]
    private CharacterCardReferences card;

    [Header("Selection (auto-find if empty)")] [SerializeField]
    private SelectionController selector;

    [Header("Camera Anchor (the GO Cinemachine follows)")] [SerializeField]
    private Transform cameraAnchor;

    /* ─── Private ─── */

    private int activeIdx;
    private CameraController camController; // WASD mover on the anchor
    private bool followActiveHero;

    /// <summary>The currently active hero or null if none.</summary>
    public GameObject ActiveHero => IsValidIndex(activeIdx) ? heroes[activeIdx] : null;

    /// <summary>Fires whenever the active hero changes. Parameter is the new hero GameObject.</summary>
    public static Action<GameObject> ActiveHeroChanged;

    // stored delegates so we can unsubscribe on destroy
    private readonly List<System.Action<int, int>> hpChangedDelegates = new();
    private readonly List<System.Action<int, int>> xpChangedDelegates = new();
    private readonly List<System.Action<int>> levelUpDelegates = new();
    private readonly List<HeroGear> gearComponents = new();
    private readonly List<System.Action> gearChangedDelegates = new();

    /* ─── Awake ─── */

    private void Awake()
    {
        if (cameraAnchor) camController = cameraAnchor.GetComponent<CameraController>();

        /* hook pip buttons */
        if (card && card.heroSelectionButtons != null)
            for (var i = 0; i < card.heroSelectionButtons.Length; i++)
            {
                var idx = i;
                if (card.heroSelectionButtons[i])
                    card.heroSelectionButtons[i].onClick.AddListener(() => SetActive(idx));
            }
    }

    /* ─── Start ─── */

    private void Start()
    {
        if (heroes.Count == 0 || card == null)
        {
            enabled = false;
            return;
        }

        /* wire HP / XP / Level / Gear events once */
        for (var i = 0; i < heroes.Count; i++)
        {
            if (!heroes[i])
            {
                hpChangedDelegates.Add(null);
                xpChangedDelegates.Add(null);
                levelUpDelegates.Add(null);
                gearComponents.Add(null);
                gearChangedDelegates.Add(null);
                continue;
            }
            var idx = i;

            var hp = heroes[i].GetComponent<Health>();
            var lv = heroes[i].GetComponent<LevelSystem>();
            var gear = heroes[i].GetComponent<BalanceHolder>()?.Gear;
            System.Action<int, int> hpDel = (cur, max) => UpdateHP(idx, cur, max);
            System.Action<int, int> xpDel = (cur, need) => UpdateXP(idx, cur, need);
            System.Action<int> lvlDel = _ => { if (idx == activeIdx) RefreshCardVisuals(idx); };
            hp.OnHealthChanged += hpDel;
            lv.OnXPChanged += xpDel;
            lv.OnLevelUp += lvlDel;
            hpChangedDelegates.Add(hpDel);
            xpChangedDelegates.Add(xpDel);
            levelUpDelegates.Add(lvlDel);

            if (gear != null)
            {
                System.Action gearDel = () => { if (idx == activeIdx) RefreshCardVisuals(idx); };
                gear.GearChanged += gearDel;
                gearComponents.Add(gear);
                gearChangedDelegates.Add(gearDel);
            }
            else
            {
                gearComponents.Add(null);
                gearChangedDelegates.Add(null);
            }
        }

        SetActive(0); // default hero
    }

    /* ─── Update ─── */

    private void Update()
    {
        /* hero hot-keys */
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetActive(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetActive(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetActive(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetActive(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetActive(4);

        /* follow toggle (Space) */
        if (Input.GetKeyDown(KeyCode.Space)) ToggleCameraFollow();
    }

    /* ─── LateUpdate – keep camera glued when following ─── */

    private void LateUpdate()
    {
        if (followActiveHero && IsValidIndex(activeIdx) && cameraAnchor)
        {
            var p = heroes[activeIdx].transform.position;
            cameraAnchor.position = new Vector3(p.x, p.y, cameraAnchor.position.z);
        }
    }

    private void OnDestroy()
    {
        if (card && card.heroSelectionButtons != null)
            foreach (var btn in card.heroSelectionButtons)
                if (btn) btn.onClick.RemoveAllListeners();

        for (var i = 0; i < heroes.Count; i++)
        {
            if (!heroes[i]) continue;
            var hp = heroes[i].GetComponent<Health>();
            var lv = heroes[i].GetComponent<LevelSystem>();
            var gear = i < gearComponents.Count ? gearComponents[i] : null;

            if (i < hpChangedDelegates.Count && hpChangedDelegates[i] != null)
                hp.OnHealthChanged -= hpChangedDelegates[i];

            if (i < xpChangedDelegates.Count && xpChangedDelegates[i] != null)
                lv.OnXPChanged -= xpChangedDelegates[i];

            if (i < levelUpDelegates.Count && levelUpDelegates[i] != null)
                lv.OnLevelUp -= levelUpDelegates[i];

            if (gear != null && i < gearChangedDelegates.Count && gearChangedDelegates[i] != null)
                gear.GearChanged -= gearChangedDelegates[i];
        }
    }

    /* ─── Selection ─── */

    public void NotifyHotSwap(GameObject heroGO)
    {
        var idx = heroes.IndexOf(heroGO);
        if (idx >= 0) SetActive(idx);
    }

    private void SetActive(int index)
    {
        if (!IsValidIndex(index)) return;
        activeIdx = index;

        /* tell SelectionController so right-click still works */
        if (selector) selector.Select(heroes[index]);

        /* visuals */
        for (var i = 0; i < heroes.Count; i++)
        {
            var on = i == activeIdx;
            if (!heroes[i]) continue;

            if (heroes[i].TryGetComponent(out BasicAttack atk))
                atk.IsPlayerControlled = on;

            if (heroes[i].TryGetComponent(out HeroClickMover mover))
                mover.SetSelected(on);
        }

        if (followActiveHero)
            SnapCameraToActiveHero();

        RefreshCardVisuals(index);

        ActiveHeroChanged?.Invoke(ActiveHero);
    }

    /* ─── Camera helpers ─── */

    private void SnapCameraToActiveHero()
    {
        if (!IsValidIndex(activeIdx) || cameraAnchor == null) return;

        var p = heroes[activeIdx].transform.position;
        cameraAnchor.position = new Vector3(p.x, p.y, cameraAnchor.position.z);
    }

    private void ToggleCameraFollow()
    {
        followActiveHero = !followActiveHero;

        if (followActiveHero) SnapCameraToActiveHero(); // snap on engage
        if (camController) camController.enabled = !followActiveHero; // WASD off/on
    }

    /* ─── UI refresh helpers ─── */

    private void RefreshCardVisuals(int idx)
    {
        if (!IsValidIndex(idx) || card == null) return;
        var hero = heroes[idx];
        var lv = hero.GetComponent<LevelSystem>();

        /* name / icon */
        if (card.heroNameText)
            card.heroNameText.text = lv ? $"{hero.name} | Lvl {lv.Level}" : hero.name;
        var sr = hero.GetComponentInChildren<SpriteRenderer>();
        if (sr) card.UpdateHeroIcon(sr.sprite);

        /* green pips */
        if (card.heroSelectionPips != null)
            for (var i = 0; i < card.heroSelectionPips.Length; i++)
                if (card.heroSelectionPips[i])
                    card.heroSelectionPips[i].SetActive(i == idx);

        /* damage / defense (include gear bonuses) */
        var balanceHolder = hero.GetComponent<BalanceHolder>();
        var gear = balanceHolder ? balanceHolder.Gear : null;
        var level = lv ? lv.Level : 1;

        if (card.heroDamageText)
        {
            var dmg = 0;
            if (balanceHolder && balanceHolder.Balance)
            {
                dmg = balanceHolder.Balance.GetDamage(level);
                if (balanceHolder.Balance is HeroBalanceData)
                    dmg += KillCodexBuffs.BonusDamage;
            }
            if (gear != null) dmg += gear.TotalDamage;
            card.heroDamageText.text = $"Damage: {dmg}";
        }

        var hp = hero.GetComponent<Health>();
        if (card.heroDefenseText && hp)
        {
            var def = 0;
            if (balanceHolder && balanceHolder.Balance)
            {
                def = balanceHolder.Balance.GetDefense(level);
                if (balanceHolder.Balance is HeroBalanceData)
                    def += KillCodexBuffs.BonusDefense;
            }
            if (gear != null) def += gear.TotalDefense;
            card.heroDefenseText.text = $"Defense: {def}";
        }

        /* attack speed / movement */
        if (balanceHolder && balanceHolder.Balance)
        {
            if (card.heroAttackSpeedText)
            {
                var rate = balanceHolder.Balance.GetAttackRate(level);
                if (gear != null) rate /= 1f + gear.TotalAttackSpeed;
                var speed = Mathf.Approximately(rate, 0f) ? 0f : 1f / rate;
                card.heroAttackSpeedText.text = $"Attack Speed: {speed:0.##}";
            }

            if (card.heroSpeedText && balanceHolder.Balance is HeroBalanceData heroBal)
            {
                var speed = heroBal.GetMoveSpeed(level);
                if (gear != null) speed += gear.TotalMoveSpeed;
                card.heroSpeedText.text = $"Movement: {speed:0.##}";
            }
        }

        /* HP / XP */
        UpdateHP(idx, hp.CurrentHP, hp.MaxHP);
        if (lv)
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

    /* ─── helpers ─── */

    private bool IsValidIndex(int i)
    {
        return i >= 0 && i < heroes.Count && heroes[i];
    }
}