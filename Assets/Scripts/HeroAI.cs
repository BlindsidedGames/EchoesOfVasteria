using Pathfinding;
using UnityEngine;
using TimelessEchoes.Attacks;

[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(HeroClickMover))]
public class HeroAI : MonoBehaviour
{
    [Header("AI Behavior")] private CharacterBalanceData balance;
    private BalanceHolder balanceHolder;
    private HeroGear gear;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask blockingLayer;

    private LevelSystem levelSystem;

    public Transform currentTarget;
    public bool isPlayerOverridden;
    private int Level => levelSystem ? levelSystem.Level : 1;
    public float VisionRange => balance ? balance.visionRange + balance.visionRangePerLevel * (Level - 1) : 20f;
    public float SafeDistance => balance ? balance.safeDistance + balance.safeDistancePerLevel * (Level - 1) : 8f;

    // Player issued destination to return to after combat
    public Vector3 lastPlayerDestination;
    private bool hasReturnDestination;
    private bool wasInCombat;

    private AIPath ai;
    private HeroClickMover mover;
    private BasicAttack attacker;

    // Preallocated buffer for non-allocating physics queries
    private readonly Collider2D[] enemyBuffer = new Collider2D[32];

    private ContactFilter2D enemyFilter;
    private ContactFilter2D blockingFilter;

    private void Awake()
    {
        ai = GetComponent<AIPath>();
        mover = GetComponent<HeroClickMover>();
        attacker = GetComponent<BasicAttack>();
        levelSystem = GetComponent<LevelSystem>();
        balanceHolder = GetComponent<BalanceHolder>();
        balance = balanceHolder ? balanceHolder.Balance : null;
        gear = balanceHolder ? balanceHolder.Gear : null;
        if (gear != null)
            gear.GearChanged += ApplySpeed;
        ApplySpeed();
        if (levelSystem != null)
            levelSystem.OnLevelUp += OnLevelChanged;
        enemyFilter = new ContactFilter2D { layerMask = enemyLayer, useLayerMask = true, useTriggers = false };
        blockingFilter = new ContactFilter2D { layerMask = blockingLayer, useLayerMask = true, useTriggers = false };

        // Default destination is the starting position so heroes can return here
        lastPlayerDestination = transform.position;
        hasReturnDestination = true;
    }

    private void Update()
    {
        if (isPlayerOverridden && ai.reachedDestination) isPlayerOverridden = false;

        if (isPlayerOverridden) return;

        FindTarget();

        if (currentTarget != null)
        {
            wasInCombat = true;
            HandleCombatMovement();
        }
        else
        {
            if (wasInCombat)
            {
                wasInCombat = false;
                mover.SetHold(false); // ensure movement resumes
                if (hasReturnDestination)
                {
                    mover.SetDestination(lastPlayerDestination);
                }
            }
        }
    }

    private void HandleCombatMovement()
    {
        if (attacker == null || mover == null) return;

        var distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        var currentAttackRange = attacker.AttackRange;

        // Kiting Logic
        if (distanceToTarget < SafeDistance)
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
        var hitCount = Physics2D.OverlapCircle(transform.position, VisionRange, enemyFilter, enemyBuffer);


        var bestScore = -1f;
        // The ideal distance we want to be from our target
        var idealKiteDistance = SafeDistance + 2f;
        Vector2 bestPoint = transform.position; // Fallback to current position
        bool foundValid = false;

        // If the hero only sees the current target, simply move directly away from it
        if (hitCount <= 1)
        {
            var awayDir = ((Vector2)transform.position - (Vector2)currentTarget.position).normalized;
            var directPoint = (Vector2)currentTarget.position + awayDir * idealKiteDistance;

            if (!Physics2D.OverlapPoint(directPoint, blockingLayer) &&
                !Physics2D.Linecast(transform.position, directPoint, blockingLayer))
                return directPoint;
        }

        // Check a number of points in a circle around our target to find the safest one
        var numCandidatePoints = 16;
        for (var i = 0; i < numCandidatePoints; i++)
        {
            var angle = i * (360f / numCandidatePoints);
            var directionFromTarget = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var candidatePoint = (Vector2)currentTarget.position + directionFromTarget * idealKiteDistance;

            if (Physics2D.OverlapPoint(candidatePoint, blockingLayer))
                continue;

            if (Physics2D.Linecast(transform.position, candidatePoint, blockingLayer))
                continue;

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
                foundValid = true;
            }
        }

        return foundValid ? bestPoint : (Vector2)transform.position;
    }


    private void FindTarget()
    {
        var hitCount = Physics2D.OverlapCircle(transform.position, VisionRange, enemyFilter, enemyBuffer);

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

    public void NotifyPlayerCommand(Vector2 destination)
    {
        isPlayerOverridden = true;
        mover.SetHold(false);
        lastPlayerDestination = destination;
        hasReturnDestination = true;
    }

    private void OnLevelChanged(int _)
    {
        ApplySpeed();
    }

    private void ApplySpeed()
    {
        if (ai == null) return;
        if (balance is HeroBalanceData heroBalance)
        {
            float speed = heroBalance.moveSpeed + heroBalance.moveSpeedPerLevel * (Level - 1);
            if (gear != null)
                speed += gear.TotalMoveSpeed;
            ai.maxSpeed = speed;
        }
    }

    private void OnDestroy()
    {
        if (levelSystem != null)
            levelSystem.OnLevelUp -= OnLevelChanged;
        if (gear != null)
            gear.GearChanged -= ApplySpeed;
    }

}