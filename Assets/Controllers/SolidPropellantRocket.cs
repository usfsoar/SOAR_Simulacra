
using UnityEngine;

public class SolidPropellantRocket : MonoBehaviour
{
    public float initialThrust = 1000f; // Maximum thrust (N)
    public float burnTime = 5f;        // Total burn time (s)
    public float rocketMass = 10f;     // Dry mass of the rocket (kg)
    public float fuelMass = 5f;        // Mass of the fuel (kg)
    public float nozzleEfficiency = 0.9f; // Thrust efficiency
    public float dragCoefficient = 0.3f;  // Drag coefficient (dimensionless)
    public float crossSectionalArea = 0.1f; // Cross-sectional area (m^2)
    public float gravity = 9.81f;          // Gravitational acceleration (m/s^2)
    public float initialHorizontalVelocity = 10f; // Horizontal speed (m/s)
    public Vector3 windForce = new Vector3(2f, 0, 0); // Constant wind force
    public Vector3 centerOfMassOffset = new Vector3(0, -1f, 0); // Offset for center of mass
    public Vector3 centerOfPressureOffset = new Vector3(0, -2f, 0); // Offset for aerodynamic forces
    public float velocityThreshold = 1f; // Minimum velocity to consider drag and wind

    private Rigidbody rb;
    private float remainingFuelMass;
    private float thrustDecayRate;
    private float elapsedTime;

    bool isGrounded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody component is missing!");
            enabled = false;
            return;
        }

        // Initialize rocket parameters
        remainingFuelMass = fuelMass;
        thrustDecayRate = initialThrust / burnTime;
        rb.mass = rocketMass + fuelMass; // Total initial mass

        // Set center of mass offset
        rb.centerOfMass = centerOfMassOffset;

        // Add initial horizontal velocity
        rb.velocity = transform.right * initialHorizontalVelocity;
    }

    void FixedUpdate()
    {
        if (elapsedTime < burnTime && remainingFuelMass > 0)
        {
            // Calculate current thrust
            float currentThrust = Mathf.Lerp(initialThrust, 0, elapsedTime / burnTime) * nozzleEfficiency;

            // Apply thrust force along the rocket's forward direction
            Vector3 thrustForce = transform.up * currentThrust;
            rb.AddForce(thrustForce);

            // Apply thrust torque if misaligned with center of mass
            Vector3 thrustPoint = transform.TransformPoint(centerOfMassOffset);
            Vector3 torque = Vector3.Cross(thrustPoint - rb.worldCenterOfMass, thrustForce);
            rb.AddTorque(torque);

            // Update fuel mass and rocket mass
            float fuelBurnRate = fuelMass / burnTime; // kg/s
            float fuelBurned = fuelBurnRate * Time.fixedDeltaTime;
            remainingFuelMass -= fuelBurned;
            remainingFuelMass = Mathf.Max(remainingFuelMass, 0); // Avoid negative fuel
            rb.mass = rocketMass + remainingFuelMass; // Update mass dynamically

            // Increment elapsed time
            elapsedTime += Time.fixedDeltaTime;
        }

        // Apply aerodynamic forces and wind only if above velocity threshold
        if (rb.velocity.magnitude > velocityThreshold)
        {
            ApplyDragForceAndTorque();
            rb.AddForce(windForce);
        }
        else{
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ApplyDragForceAndTorque()
    {
        // Calculate drag force
        float velocity = rb.velocity.magnitude;
        float dragForceMagnitude = 0.5f * dragCoefficient * crossSectionalArea * velocity * velocity;
        Vector3 dragForce = -rb.velocity.normalized * dragForceMagnitude;

        // Apply drag force at the center of pressure
        Vector3 dragPoint = transform.TransformPoint(centerOfPressureOffset);
        rb.AddForceAtPosition(dragForce, dragPoint);

        // Calculate and apply torque from drag
        Vector3 torque = Vector3.Cross(dragPoint - rb.worldCenterOfMass, dragForce);
        rb.AddTorque(torque);
    }

    bool IsGrounded()
    {
        RaycastHit hit;
        return Physics.Raycast(transform.position, Vector3.down, out hit, 0.5f);
    }

    public void IncreaseDrag(float dragMultiplier)
    {
        // Ensure the drag multiplier is clamped to avoid invalid values
        dragMultiplier = Mathf.Clamp(dragMultiplier, 1f, 5f); // Minimum is no change, maximum is 5x drag

        // Increase the drag coefficient based on the multiplier
        dragCoefficient *= dragMultiplier;

        Debug.Log($"Drag increased. New Drag Coefficient: {dragCoefficient}");
    }


    void Update()
    {
        // Debugging info
        Debug.Log($"Time: {elapsedTime:F2}s, Fuel Mass: {remainingFuelMass:F2}kg, Velocity: {rb.velocity}, Grounded: {IsGrounded()}");
    }
}

