using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SolidPropellantRocket : MonoBehaviour
{
    public float initialThrust = 1000f; // Maximum thrust (N)
    public float burnTime = 5f;        // Total burn time (s)
    public float rocketMass = 10f;     // Dry mass of the rocket (kg)
    public float fuelMass = 5f;        // Mass of the fuel (kg)
    public float nozzleEfficiency = 0.9f; // Thrust efficiency
    public float dragCoefficient = 0.3f;  // Drag coefficient (dimensionless)
    public Vector3 currentDrag;
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
    private bool launched = false; // Flag to check if the rocket has been launched
    public UnityEvent<LogData> DebugLogEvent;
    public float flaps_length=0;

    public void DebugShortcut(string msg, LogType logType = LogType.Default)
    {
        switch (logType)
        {
            case LogType.Default:
                Debug.Log(msg);
                break;
            case LogType.Warning:
                Debug.Log(msg);
                break;
            case LogType.Error:
                Debug.Log(msg);
                break;
            default:
                Debug.Log(msg);
                break;
        }
        DebugLogEvent.Invoke(new LogData { message = msg, logType = logType });
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            DebugShortcut("Rigidbody component is missing!", LogType.Error);
            enabled = false;
            return;
        }

        rb.isKinematic = true; // Prevent movement until launch
    }

    public void Launch()
    {
        if (launched)
        {
            DebugShortcut("Rocket already launched!", LogType.Error);
            return;
        }

        launched = true;
        rb.isKinematic = false; // Allow physics simulation
        remainingFuelMass = fuelMass;
        thrustDecayRate = initialThrust / burnTime;
        rb.mass = rocketMass + fuelMass; // Set initial total mass
        rb.velocity = transform.right * initialHorizontalVelocity; // Apply initial horizontal speed

        DebugShortcut("🚀 LAUNCH!");
    }

    void FixedUpdate()
    {
        if (!launched) return; // Don't update physics until launched

        if (elapsedTime < burnTime && remainingFuelMass > 0)
        {
            // Calculate current thrust
            float currentThrust = Mathf.Lerp(initialThrust, 0, elapsedTime / burnTime) * nozzleEfficiency;
            Vector3 thrustForce = transform.up * currentThrust;
            rb.AddForce(thrustForce);

            // Apply thrust torque if misaligned with center of mass
            Vector3 thrustPoint = transform.TransformPoint(centerOfMassOffset);
            Vector3 torque = Vector3.Cross(thrustPoint - rb.worldCenterOfMass, thrustForce);
            rb.AddTorque(torque);

            // Update fuel mass
            float fuelBurnRate = fuelMass / burnTime;
            float fuelBurned = fuelBurnRate * Time.fixedDeltaTime;
            remainingFuelMass -= fuelBurned;
            remainingFuelMass = Mathf.Max(remainingFuelMass, 0);
            rb.mass = rocketMass + remainingFuelMass; // Update mass dynamically

            elapsedTime += Time.fixedDeltaTime;
        }

        if (rb.velocity.magnitude > velocityThreshold)
        {
            ApplyDragForceAndTorque();
            rb.AddForce(windForce);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ApplyDragForceAndTorque()
    {
        float velocity = rb.velocity.y;
        Vector3 dragForce = (-0.7207f + 0.05836f*velocity + 0.2469f*flaps_length + 0.00494f*(velocity*velocity) + -0.3308f*velocity*flaps_length + 17.65f*(flaps_length*flaps_length) + -0.0000009656f*(velocity*velocity*velocity) + 0.002397f*(velocity*velocity)*flaps_length + 0.373f*velocity*(flaps_length*flaps_length) + -22.71f*(flaps_length*flaps_length*flaps_length))* -rb.velocity.normalized;

        Vector3 dragPoint = transform.TransformPoint(centerOfPressureOffset);
        rb.AddForceAtPosition(dragForce, dragPoint);

        Vector3 torque = Vector3.Cross(dragPoint - rb.worldCenterOfMass, dragForce);
        rb.AddTorque(torque);
    }

    public void ChangeAirbrakesLength(float new_length){
        flaps_length = new_length;
    }
    public void SetThrust(float newThrust){
        initialThrust = newThrust;
        DebugShortcut($"Set Thrust: {initialThrust}");
    }
    public void SetThrust(string newThrust){
        SetThrust(float.Parse(newThrust));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SolidPropellantRocket))]
public class SolidPropellantRocketEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Draw the default fields

        SolidPropellantRocket rocket = (SolidPropellantRocket)target;

        if (GUILayout.Button("🚀 LAUNCH!"))
        {
            rocket.Launch();
        }
    }
}
#endif