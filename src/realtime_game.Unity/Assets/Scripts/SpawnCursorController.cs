using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpawnCursorController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float stageRadius = 9f;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
    }

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(h, 0, v);

        Vector3 nextPos = rb.position + dir * moveSpeed * Time.fixedDeltaTime;

        // ”ÍˆÍ§ŒÀ
        nextPos.y = 0;
        if (nextPos.magnitude > stageRadius)
        {
            nextPos = nextPos.normalized * stageRadius;
        }

        rb.MovePosition(nextPos);
    }
}