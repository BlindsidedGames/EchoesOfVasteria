using UnityEngine;

/// <summary>WASD / arrow-key top-down movement.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    private Vector2    input;
    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void Update()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")).normalized;
    }

    private void FixedUpdate()
    {
        Vector2 newPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }
}