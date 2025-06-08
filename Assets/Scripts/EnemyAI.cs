using UnityEngine;
using System.Linq;

/// <summary>Simple vision-based chase + attack.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] private float viewRange = 6f;
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void FixedUpdate()
    {
        var heroes = GameObject.FindGameObjectsWithTag("Hero");
        if (heroes.Length == 0) return;

        Transform closest = heroes
            .OrderBy(h => (h.transform.position - transform.position).sqrMagnitude)
            .First().transform;

        float dist = Vector2.Distance(transform.position, closest.position);
        if (dist > viewRange) return;              // out of sight

        Vector2 dir = (closest.position - transform.position).normalized;
        rb.MovePosition(rb.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, viewRange);
    }
#endif
}