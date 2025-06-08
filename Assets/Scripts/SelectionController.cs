using UnityEngine;

public class SelectionController : MonoBehaviour
{
    [SerializeField] private LayerMask heroMask; // Hero layer
    [SerializeField] private PartyManager partyManager; // Drag or auto-link

    private HeroClickMover selectedMover;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryMouseSelect();
        if (Input.GetMouseButtonDown(1)) IssueMove();
    }

    /* ───────── public API for PartyManager hot-keys ───────── */
    public void Select(GameObject heroGO)
    {
        if (heroGO == null) return;

        // Clear previous
        if (selectedMover) selectedMover.SetSelected(false);

        selectedMover = heroGO.GetComponent<HeroClickMover>();
        if (selectedMover) selectedMover.SetSelected(true);
    }

    /* ───────── internal mouse workflow ───────── */
    private void TryMouseSelect()
    {
        Vector2 mWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        var hit = Physics2D.Raycast(mWorld, Vector2.zero, 0f, heroMask);
        if (!hit) return;

        var mover = hit.collider.GetComponent<HeroClickMover>();
        if (!mover) return;

        Select(mover.gameObject); // same logic
        partyManager.NotifyHotSwap(mover.gameObject); // sync stats / UI
    }

    private void IssueMove()
    {
        if (!selectedMover) return;

        var mWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mWorld.z = 0;
        selectedMover.SetDestination((Vector2)mWorld);
    }
}