using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RocketController : MonoBehaviour
{
    public RocketSerialController sc;
    public UIManager uiManager;
    private bool alt_req_rec = false;
    private CSVDataWriter serialLog;

    private List<Vector3> positions = new List<Vector3>();
    private bool isFollowingData = false;
    private int currentIndex = 0;
    private float lerpTime = 0f;
    private float lerpDuration = 1f;
    public GameObject payloadCoupler;
    public GameObject motorCoupler;
    public GameObject payloadCouplerDistanceRef;
    public GameObject motorCouplerDistanceSensor;
    public int payloadCouplerDirection = 0;
    float startTime=0;

    void FixedUpdate()
    {
        if (isFollowingData && currentIndex < positions.Count - 1)
        {
            float targetTime = positions[currentIndex + 1].x;
            if ((Time.time-startTime) >= targetTime)
            {
                currentIndex++;
                lerpTime = 0f;
                //Define the lerpduration as the difference between the current and next time
                lerpDuration = positions[currentIndex].x - positions[currentIndex - 1].x;
            }

            if (currentIndex < positions.Count - 1)
            {
                // Interpolate between the current and next position
                Vector3 startPosition = positions[currentIndex];
                Vector3 endPosition = positions[currentIndex + 1];
                lerpTime += Time.deltaTime;
                float progress = lerpTime / lerpDuration;
                transform.position = Vector3.Lerp(startPosition, endPosition, progress);

                // Look towards the next point with the y-axis
                Vector3 direction = endPosition - startPosition;
                if (direction != Vector3.zero)
                {
                    // direction = new Vector3(direction.y, direction.x, direction.z); // Swap x and y for rotation
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, progress);
                }
            }
        }
        //if finished following data, enable gravity
        else if(currentIndex == positions.Count - 1)
        {
            GetComponent<Rigidbody>().useGravity = true;
        }

    }

    // Update is called once per frame
    void Update()
    {
        while (RocketSerialController.messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message, sc);
        }
        PayloadCouplerHandler();
    }

    void PayloadCouplerHandler()
    {
        if (payloadCouplerDirection < 0)
        {
            // payloadCoupler.transform.localPosition += transform.forward * Time.deltaTime * 0.2f;
            //Move paylod coupler away form the center of the rocket
            Vector3 dir = payloadCoupler.transform.position - transform.position;
            payloadCoupler.transform.position -= dir.normalized * Time.deltaTime * 0.1f;
        }
        //If direction is > add transform.forward to the payloadCoupler local position
        else if (payloadCouplerDirection > 0)
        {
            Vector3 dir = payloadCoupler.transform.position - transform.position;
            payloadCoupler.transform.position += dir.normalized * Time.deltaTime * 0.3f;
        }
        //If direction is 0, stop the payloadCoupler
        else
        {
            payloadCoupler.transform.localPosition = payloadCoupler.transform.localPosition;
        }
    }

    public void AddThrust(float thrust)
    {
        // Add force to the rocket
        GetComponent<Rigidbody>().AddForce(transform.forward * thrust);
    }

    public void ProcessMessage(string message, RocketSerialController serialController)
    {
        if (message == "FAKE_ALT:GET")
        {
            var altitude = transform.position.y;
            // Round it
            altitude = (float)Math.Round(altitude, 2);
            //Add some noise and sometimes an outlier
            //there's a 5% chance of an outlier
            //There's a 10% chance of noise
            if (UnityEngine.Random.value < 0.05f)
            {
                altitude += 100;
            }
            else if (UnityEngine.Random.value < 0.1f)
            {
                altitude += UnityEngine.Random.Range(-10, 10);
            }

            // Save altitude to the log as floats
            // serialLog.WriteData(new List<float> { Time.time, altitude });
            byte[] altitudeBytes = BitConverter.GetBytes(altitude);
            byte[] response = new byte[1 + altitudeBytes.Length];
            response[0] = 0x02; // Response code for altitude
            Array.Copy(altitudeBytes, 0, response, 1, altitudeBytes.Length);
            serialController.WriteSerialAsync(response);
        }
        if(message.StartsWith("DC_MOTOR")){
            //Get the direction
            payloadCouplerDirection = int.Parse(message.Split(':')[1]);
            

        }
        //Process distance sensor message
        if (message=="DISTANCE_SENSOR:GET")
        {
            Vector3 motorCouplerPos = motorCouplerDistanceSensor.transform.position;
            Vector3 payloadCouplerPos = payloadCouplerDistanceRef.transform.position;

            float x_diff = motorCouplerPos.x - payloadCouplerPos.x;
            float y_diff = motorCouplerPos.y - payloadCouplerPos.y;
            float z_diff = motorCouplerPos.z - payloadCouplerPos.z;

            //Unity units are meters
            float distance = Mathf.Sqrt(x_diff * x_diff + y_diff * y_diff + z_diff * z_diff);

            float distanceMillimeters = distance * 1000;

            // Convert to uint16_t (ushort in C#)
            ushort distanceUInt16 = (ushort)distanceMillimeters;

            // Convert ushort to bytes
            byte[] distanceBytes = BitConverter.GetBytes(distanceUInt16);

            // Ensure bytes are in the correct order (little-endian for Arduino)
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(distanceBytes);
            }

            // Create response array with response code and distance bytes
            byte[] response = new byte[1 + distanceBytes.Length];
            response[0] = 0x06; // Response code for distance sensor
            Array.Copy(distanceBytes, 0, response, 1, distanceBytes.Length);

            // Send the response
            serialController.WriteSerialAsync(response);

        }
    }

    public void LoadCSVData(string filePath)
    {
        positions.Clear();

        using (var reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (!line.StartsWith("#"))
                {
                    var values = line.Split(',');
                    if (values.Length >= 2)
                    {
                        float time = float.Parse(values[0]);
                        float alt = float.Parse(values[1]) * 0.3048f; // Convert feet to meters
                        positions.Add(new Vector3(time, alt, 0)); // Create a Vector3 with time as x and altitude as y
                    }
                }
            }
        }
    }

    public void FollowData()
    {
        GetComponent<Rigidbody>().useGravity = false;
        startTime = Time.time;
        isFollowingData = true;
        currentIndex = 0;
        lerpTime = 0f;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RocketController))]
public class RocketControllerEditor : Editor
{
    float thrust = 8000;
    string filePath = "";
    string message = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        RocketController myScript = (RocketController)target;

        thrust = EditorGUILayout.FloatField("Thrust", thrust);

        if (GUILayout.Button("Add Thrust"))
        {
            myScript.AddThrust(thrust);
        }

        filePath = EditorGUILayout.TextField("CSV File Path", filePath);

        if (GUILayout.Button("Load CSV Data"))
        {
            myScript.LoadCSVData(filePath);
        }

        if (GUILayout.Button("Follow Data"))
        {
            myScript.FollowData();
        }
        // test the process message function
        //input field for message
        message = EditorGUILayout.TextField("Message", message);
        if (GUILayout.Button("Process Message"))
        {
            myScript.ProcessMessage(message, myScript.sc);
        }
    }
}
#endif
