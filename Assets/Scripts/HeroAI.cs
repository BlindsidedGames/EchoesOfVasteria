using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(HeroClickMover))]
public class HeroAI : MonoBehaviour
{
    [Header("AI Behavior")] [SerializeField]
    private float visionRange = 20f;

    [SerializeField] private float safeDistance = 8f;
    [SerializeField] private LayerMask enemyLayer;

    public Transform currentTarget;
    public bool isPlayerOverridden;

    private AIPath ai;
    private HeroClickMover mover;
    private BasicAttackTelegraphed attacker;

    // Preallocated buffer for non-allocating physics queries
    private readonly Collider2D[] enemyBuffer = new Collider2D[32];
    private ContactFilter2D enemyFilter;

    private void Awake()
    {
        ai = GetComponent<AIPath>();
        mover = GetComponent<HeroClickMover>();
        attacker = GetComponent<BasicAttackTelegraphed>();
        enemyFilter = new ContactFilter2D { layerMask = enemyLayer, useLayerMask = true, useTriggers = false };
    }

    private void Update()
    {
        if (isPlayerOverridden && ai.reachedDestination) isPlayerOverridden = false;

        if (isPlayerOverridden) return;

        FindTarget();

        if (currentTarget != null) HandleCombatMovement();
    }

    private void HandleCombatMovement()
    {
        if (attacker == null || mover == null) return;

        var distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        var currentAttackRange = attacker.AttackRange;

        // Kiting Logic
        if (distanceToTarget < safeDistance)
        {
            // Find the best position to kite to that avoids other enemies.
            var kitePoint = FindSafeKitePoint();
            mover.SetDestination(kitePoint);
            ai.endReachedDistance = 1f;
            mover.SetHold(false);

            attacker.Attack(currentTarget);
        }
        else if (distanceToTarget > currentAttackRange)
        {
            // Advance: Move towards the enemy but stop at attack range.
            mover.SetDestination(currentTarget.position);
            ai.endReachedDistance = currentAttackRange;
            mover.SetHold(false);
        }
        else
        {
            // In the sweet spot: Stop moving and command an attack.
            mover.SetHold(true);
            attacker.Attack(currentTarget);
        }
    }

    /// <summary>
    ///     Finds the best point to move to by sampling points on a circle around the target.
    ///     The "best" point is the one furthest away from any *other* nearby enemies.
    /// </summary>
    /// <returns>A safe world position to move to.</returns>
    private Vector2 FindSafeKitePoint()
    {
        // Get all enemies in vision range using a non-allocating query
        var hitCount = Physics2D.OverlapCircle(transform.position, visionRange, enemyFilter, enemyBuffer);

        var bestScore = -1f;
        // The ideal distance we want to be from our target
        var idealKiteDistance = safeDistance + 2f;
        Vector2 bestPoint = transform.position; // Fallback to current position

        // Check a number of points in a circle around our target to find the safest one
        var numCandidatePoints = 16;
        for (var i = 0; i < numCandidatePoints; i++)
        {
            var angle = i * (360f / numCandidatePoints);
            var directionFromTarget = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var candidatePoint = (Vector2)currentTarget.position + directionFromTarget * idealKiteDistance;

            float score;

            // Determine score based on distance from other enemies (ignoring the current target)
            bool hasOtherEnemy = false;
            float minDistance = float.MaxValue;
            for (var j = 0; j < hitCount; j++)
            {
                var collider = enemyBuffer[j];
                if (collider.transform == currentTarget) continue;

                hasOtherEnemy = true;
                var dist = Vector2.Distance(candidatePoint, collider.transform.position);
                if (dist < minDistance) minDistance = dist;
            }

            if (hasOtherEnemy)
            {
                score = minDistance;
            }
            else
            {
                // If there are no other enemies, the best point is simply the one on the circle
                // that is furthest from our current position (encouraging movement).
                score = Vector2.Distance(candidatePoint, transform.position);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidatePoint;
            }
        }

        return bestPoint;
    }


    private void FindTarget()
    {
        var hitCount = Physics2D.OverlapCircle(transform.position, visionRange, enemyFilter, enemyBuffer);
        var closestDist = float.MaxValue;
        Transform closestTarget = null;

        for (var i = 0; i < hitCount; i++)
        {
            var hit = enemyBuffer[i];
            var dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestTarget = hit.transform;
            }
        }

        currentTarget = closestTarget;
    }

    public void NotifyPlayerCommand()
    {
        isPlayerOverridden = true;
        mover.SetHold(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        if (attacker != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attacker.AttackRange);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
    }
#endif
}