using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Payload25Handler : MonoBehaviour
{
    public Payload25SerialController plsc;
    public GameObject imu;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        while (Payload25SerialController.messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message, plsc, imu, gameObject);
        }
        while (Payload25SerialController.debugQueue.TryDequeue(out string message))
        {
            Debug.Log(message);
        }
    }

    public void ProcessMessage(string message, Payload25SerialController plsc, GameObject imuObject, GameObject rocketObject)
    {
        byte[] response;

        Rigidbody imuRb = imuObject.GetComponent<Rigidbody>(); // Get IMU Rigidbody
        Transform imuTf = imuObject.transform; // Get IMU Transform
        Transform rocketTf = rocketObject.transform; // Get Rocket Transform

        switch (message)
        {
            case "ALT":
                float altitude = rocketTf.position.y; // Use rocket's altitude
                if (UnityEngine.Random.value < 0.05f) // Add noise with a 5% chance
                {
                    altitude += 20;
                }
                response = new byte[5];
                response[0] = 0x03;
                Array.Copy(BitConverter.GetBytes(altitude), 0, response, 1, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "TMP":
                float temperature = 20f + UnityEngine.Random.Range(-2f, 2f);
                response = new byte[5];
                response[0] = 0x02;
                Array.Copy(BitConverter.GetBytes(temperature), 0, response, 1, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "PRS":
                float pressure = 1013.25f + UnityEngine.Random.Range(-5f, 5f);
                response = new byte[5];
                response[0] = 0x03;
                Array.Copy(BitConverter.GetBytes(pressure), 0, response, 1, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "ACC": // Acceleration from IMU's Rigidbody
                Vector3 acceleration = imuRb ? imuRb.velocity / Time.fixedDeltaTime : Vector3.zero;
                response = new byte[13];
                response[0] = 0x04;
                Array.Copy(BitConverter.GetBytes(acceleration.x), 0, response, 1, 4);
                Array.Copy(BitConverter.GetBytes(acceleration.y), 0, response, 5, 4);
                Array.Copy(BitConverter.GetBytes(acceleration.z), 0, response, 9, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "ACL": // Linear acceleration (removing gravity)
                Vector3 linearAccel = imuRb ? imuRb.velocity / Time.fixedDeltaTime - imuTf.rotation * Physics.gravity : Vector3.zero;
                response = new byte[13];
                response[0] = 0x04;
                Array.Copy(BitConverter.GetBytes(linearAccel.x), 0, response, 1, 4);
                Array.Copy(BitConverter.GetBytes(linearAccel.y), 0, response, 5, 4);
                Array.Copy(BitConverter.GetBytes(linearAccel.z), 0, response, 9, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "GRV": // Gravity in IMU's local frame
                Vector3 localGravity = imuTf.InverseTransformDirection(Physics.gravity);
                response = new byte[13];
                response[0] = 0x04;
                Array.Copy(BitConverter.GetBytes(localGravity.x), 0, response, 1, 4);
                Array.Copy(BitConverter.GetBytes(localGravity.y), 0, response, 5, 4);
                Array.Copy(BitConverter.GetBytes(localGravity.z), 0, response, 9, 4);
                plsc.WriteSerialAsync(response);
                break;

            default:
                Debug.Log("Unknown message type: " + message);
                break;
        }
    }
}
