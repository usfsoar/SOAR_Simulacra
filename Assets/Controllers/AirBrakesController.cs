using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AirBrakesController : MonoBehaviour
{
    public List<GameObject> Paddles = new List<GameObject>();
    public GameObject center;
    public float MaxActuation = 15;
    public float MinActuation = 0.02f;

    public SolidPropellantRocket rocketController;

    void SetActuationDegree(float percentage)
    {
        foreach (GameObject paddle in Paddles)
        {
            Vector3 dir = paddle.transform.up;
            float mag = percentage * (MaxActuation-MinActuation)+MinActuation;
            Vector3 newPoint = center.transform.position + dir * mag;
            paddle.transform.position = newPoint;
        }

        float dragMultiplier = Mathf.Lerp(1f, 5f, percentage);
        if (rocketController != null)
        {
            rocketController.IncreaseDrag(dragMultiplier);
        }
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

            if (GUILayout.Button("Test SetActuationDegree"))
            {
                controller.SetActuationDegree(testPercentage);
            }
        }
    }
#endif
}
