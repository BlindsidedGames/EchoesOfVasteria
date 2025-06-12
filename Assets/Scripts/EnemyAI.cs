using Pathfinding;
using UnityEngine;
using TimelessEchoes.Attacks;

// This is a state machine for our AI's behavior.
public enum AIState
{
    Wandering,
    Chasing,
    ReturningHome
}

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Behavior")] [SerializeField]
    private float visionRange = 15f;

    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float strafeDistance = 7f; // The ideal distance for circling
    [SerializeField] private LayerMask heroLayer; // Set this to your "Hero" layer in the inspector

    [Header("Leashing & Wandering")] [SerializeField]
    private float leashDistance = 25f; // How far it can chase from its anchor

    [SerializeField] private float wanderInterval = 3f; // How often to pick a new wander point

    [Header("Rewards")] [SerializeField]
    private int xpReward = 5;
    public int XPReward => xpReward;

    // --- State & References ---
    public AIState CurrentState { get; private set; } = AIState.Wandering;
    private Transform currentTarget;
    private Vector3 spawnAnchor;
    private float timeToNextWander;
    private BasicAttack attacker; // Reference to the attack script

    // --- A* Components ---
    private AIPath ai;
    private Seeker seeker;

    private void Awake()
    {
        ai = GetComponent<AIPath>();
        seeker = GetComponent<Seeker>();
        attacker = GetComponent<BasicAttack>(); // Get the attack component
    }

    public void SetSpawnAnchor(Vector3 anchor)
    {
        spawnAnchor = anchor;
    }

    private void Update()
    {
        // The core state machine logic.
        switch (CurrentState)
        {
            case AIState.Wandering:
                HandleWanderingState();
                break;
            case AIState.Chasing:
                HandleChasingState();
                break;
            case AIState.ReturningHome:
                HandleReturningHomeState();
                break;
        }
    }

    private void HandleWanderingState()
    {
        // Look for a hero to chase.
        FindTarget();
        if (currentTarget != null)
        {
            CurrentState = AIState.Chasing;
            return;
        }

        // Periodically pick a new point to wander to around the anchor.
        if (Time.time > timeToNextWander)
        {
            timeToNextWander = Time.time + wanderInterval;
            var wanderPoint = (Vector2)spawnAnchor + Random.insideUnitCircle * 10f;
            ai.destination = wanderPoint;
            ai.endReachedDistance = 1f;
        }
    }

    private void HandleChasingState()
    {
        // Re-evaluate the best hero target each frame so we always chase the
        // closest opponent instead of tunneling the first one we spotted.
        FindTarget();

        if (currentTarget == null)
        {
            CurrentState = AIState.Wandering;
            ai.destination = transform.position;
            return;
        }

        var distanceToAnchor = Vector2.Distance(transform.position, spawnAnchor);
        if (distanceToAnchor > leashDistance)
        {
            currentTarget = null;
            CurrentState = AIState.ReturningHome;
            return;
        }

        // Tell the attack component to try and fire.
        // It will handle its own cooldowns and range checks.
        if (attacker != null) attacker.Attack(currentTarget);

        // --- Strafing/Kiting Logic (Movement Only) ---
        var distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= strafeDistance)
        {
            ai.endReachedDistance = 0f;
            Vector2 directionToTarget = (currentTarget.position - transform.position).normalized;
            var strafeDirection = new Vector2(directionToTarget.y, -directionToTarget.x);
            ai.destination = currentTarget.position + (Vector3)(strafeDirection * 5f);
        }
        else
        {
            ai.destination = currentTarget.position;
            ai.endReachedDistance = strafeDistance;
        }
    }

    private void HandleReturningHomeState()
    {
        ai.destination = spawnAnchor;
        ai.endReachedDistance = 1f;

        if (Vector2.Distance(transform.position, spawnAnchor) < 1.5f) CurrentState = AIState.Wandering;
    }

    private void FindTarget()
    {
        var potentialTargets = Physics2D.OverlapCircleAll(transform.position, visionRange, heroLayer);
        var closestDist = float.MaxValue;
        Transform closestTarget = null;

        foreach (var hit in potentialTargets)
        {
            var dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestTarget = hit.transform;
            }
        }

        currentTarget = closestTarget;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, strafeDistance);

        if (spawnAnchor != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnAnchor, leashDistance);
        }
    }
#endif
}