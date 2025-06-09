using UnityEngine;

/// <summary>
///     A lightweight WASD (or arrow-key) mover intended only for the camera to follow.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;

    private Rigidbody2D rb;
    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")).normalized;
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
    }

    public void SnapTo(Vector2 pos)
    {
        rb.position = pos;
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }
}