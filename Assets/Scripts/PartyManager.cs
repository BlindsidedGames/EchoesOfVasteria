using System.Collections.Generic;
using UnityEngine;

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

    // stored delegates so we can unsubscribe on destroy
    private readonly List<System.Action<int, int>> hpChangedDelegates = new();
    private readonly List<System.Action<int, int>> xpChangedDelegates = new();

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

        /* wire HP / XP events once */
        for (var i = 0; i < heroes.Count; i++)
        {
            if (!heroes[i])
            {
                hpChangedDelegates.Add(null);
                xpChangedDelegates.Add(null);
                continue;
            }
            var idx = i;

            var hp = heroes[i].GetComponent<Health>();
            var lv = heroes[i].GetComponent<LevelSystem>();
            System.Action<int, int> hpDel = (cur, max) => UpdateHP(idx, cur, max);
            System.Action<int, int> xpDel = (cur, need) => UpdateXP(idx, cur, need);
            hp.OnHealthChanged += hpDel;
            lv.OnXPChanged += xpDel;
            hpChangedDelegates.Add(hpDel);
            xpChangedDelegates.Add(xpDel);
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

            if (i < hpChangedDelegates.Count && hpChangedDelegates[i] != null)
                hp.OnHealthChanged -= hpChangedDelegates[i];

            if (i < xpChangedDelegates.Count && xpChangedDelegates[i] != null)
                lv.OnXPChanged -= xpChangedDelegates[i];
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

            if (heroes[i].TryGetComponent(out BasicAttackTelegraphed atk))
                atk.IsPlayerControlled = on;

            if (heroes[i].TryGetComponent(out HeroClickMover mover))
                mover.SetSelected(on);
        }

        SnapCameraToActiveHero();

        RefreshCardVisuals(index);
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

        /* name / icon */
        if (card.heroNameText) card.heroNameText.text = hero.name;
        if (card.heroIcon && hero.TryGetComponent(out SpriteRenderer sr))
            card.heroIcon.sprite = sr.sprite;

        /* green pips */
        if (card.heroSelectionPips != null)
            for (var i = 0; i < card.heroSelectionPips.Length; i++)
                if (card.heroSelectionPips[i])
                    card.heroSelectionPips[i].SetActive(i == idx);

        /* damage / defense */
        if (card.heroDamageText && hero.TryGetComponent(out BasicAttackTelegraphed atk))
            card.heroDamageText.text = $"Damage: {atk.BaseDamage}";

        if (card.heroDefenseText && hero.TryGetComponent(out HeroStats stats))
            card.heroDefenseText.text = $"Defense: {stats.Defense}";

        /* HP / XP */
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

    /* ─── helpers ─── */

    private bool IsValidIndex(int i)
    {
        return i >= 0 && i < heroes.Count && heroes[i];
    }
}