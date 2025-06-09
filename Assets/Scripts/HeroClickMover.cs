using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;

/// <summary>One A* agent + personal move-target + selection visuals.</summary>
[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(AIDestinationSetter))]
[RequireComponent(typeof(RVOController))]
public class HeroClickMover : MonoBehaviour
{
    /* ─────────── Serialized ─────────── */

    [Tooltip("Child GameObject that shows the little triangle/arrow when the hero is selected.")] [SerializeField]
    private GameObject selectedIndicator;

    /* ─────────── Internals ─────────── */

    private Transform moveTarget;
    private AIDestinationSetter dst;
    private AIPath ai;

    /* ────────────────────────────────── */

    private void Awake()
    {
        ai = GetComponent<AIPath>();
        dst = GetComponent<AIDestinationSetter>();

        // personal hidden target ------------------------------------
        var go = new GameObject($"{name}_MoveTarget");
        moveTarget = go.transform;
        dst.target = moveTarget;
        go.hideFlags = HideFlags.HideInHierarchy;

        // make sure the indicator starts OFF
        if (selectedIndicator) selectedIndicator.SetActive(false);
    }

    /* ─────────── Public API ─────────── */

    public void SetDestination(Vector2 worldPos)
    {
        moveTarget.position = worldPos;
        ai.SearchPath();
    }

    public void SetDestination(Vector3 worldPos)
    {
        SetDestination((Vector2)worldPos);
    }

    /// <summary>Called by PartyManager whenever this hero becomes (un)selected.</summary>
    public void SetSelected(bool isSel)
    {
        if (selectedIndicator) selectedIndicator.SetActive(isSel);

        // keep the optional tint for extra feedback
        if (TryGetComponent(out SpriteRenderer sr))
            sr.color = isSel ? Color.yellow : Color.white;
    }

    public void Follow(Transform who)
    {
        dst.target = who;
        ai.SearchPath();
    }

    public void ResumePersonalTarget()
    {
        dst.target = moveTarget;
        ai.SearchPath();
    }

    /// <summary>Hold = freeze in place to fight; release to resume normal movement.</summary>
    public void SetHold(bool hold)
    {
        ai.canMove = !hold; // disables locomotion & rotation
        ai.isStopped = hold; // makes sure the agent stops immediately
    }
}