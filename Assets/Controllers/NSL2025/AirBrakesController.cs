using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class AirBrakesController : MonoBehaviour
{
    public List<GameObject> Paddles = new List<GameObject>();
    public GameObject center;
    public float MaxLength = 0.765f; //inches
    public float MinLength = 0;//inches
    public float MaxActuationVisual = 15;
    public float MinActuationVisual = 0.02f;
    public UnityEvent<Vector3> ChangeDrag;
    public UnityEvent<float> OnAirbrakesChanged;
    private Rigidbody rb;
    public UnityEvent<LogData> DebugLogEvent;
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
    

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        } 
    }

    void SetActuationLength(float length)
    {
        OnAirbrakesChanged?.Invoke(length);

        float percentage = Mathf.InverseLerp(MinLength, MaxLength, length);

        foreach (GameObject paddle in Paddles)
        {
            Vector3 dir = paddle.transform.up;
            float mag = percentage * (MaxActuationVisual - MinActuationVisual) + MinActuationVisual;
            Vector3 newPoint = center.transform.position + dir * mag;
            paddle.transform.position = newPoint;
        }
    }

    float GetLengthFromAngle(float angle)
    {
        float a = -0.0002f;
        float b = 0.0309f;
        float c = -0.0877f;
        return a * angle * angle + b * angle + c;
    }

    public void SetActuationFromAngle(int angle)
    {
        float length = GetLengthFromAngle(angle);
        DebugShortcut($"Received angle: {angle} -> length: {length}");
        SetActuationLength(length);
    }

    public void SetActuationFromPercentage(float percentage)
    {
        percentage = Mathf.Clamp01(percentage); // clamps between 0.0 and 1.0
        float length = Mathf.Lerp(MinLength, MaxLength, percentage);
        SetActuationLength(length);
    }



#if UNITY_EDITOR
    [CustomEditor(typeof(AirBrakesController))]
    public class AirBrakesControllerEditor : Editor
    {
        private float testPercentage = 0.5f; // Slider value for testing

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // Draw default inspector elements

            AirBrakesController controller = (AirBrakesController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Custom Testing Tools", EditorStyles.boldLabel);

            testPercentage = EditorGUILayout.Slider("Test Actuation Percentage", testPercentage, 0f, 1f);

            if (GUILayout.Button("Test SetActuationPercentage"))
            {
                controller.SetActuationFromPercentage(testPercentage);
            }
        }
    }
#endif
}
