using UnityEngine;

public class GravityControl : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 initialGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialGravity = Physics.gravity;
    }

    void FixedUpdate()
    {
        // Apply gravity manually to rotation
        Quaternion gravityRotation = Quaternion.FromToRotation(transform.up, -Physics.gravity.normalized) * transform.rotation;
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, gravityRotation, Time.fixedDeltaTime * 2));

        // Counteract gravity's effect on position
        rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
    }
}
