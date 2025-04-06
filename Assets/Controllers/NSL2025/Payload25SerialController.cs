using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;
using UnityEngine.Events;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class Payload25SerialController : SerialController
{
    public GameObject imu;
    public Payload25Controller payload;

    [Serializable]
    struct AltimeterData
    {
        public int noise;
        public float outlierChance;
    };
    AltimeterData altData = new AltimeterData { noise = 5, outlierChance = 0.02f };

    struct IMUModifications
    {
        public float noise;
        public float outlierChance;
    }
    IMUModifications imuMods = new IMUModifications { noise = 0.5f, outlierChance = 0.05f };

    // Concurrent Queues
    public static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    public static ConcurrentQueue<string> debugQueue = new ConcurrentQueue<string>();
    public static ConcurrentQueue<Tuple<int, int>> servoQueue = new ConcurrentQueue<Tuple<int, int>>();




    private Vector3 lastVelocity = Vector3.zero;

    public override async void ReadSerialAsync()
    {
        while (isReading && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    byte messageType = (byte)serialPort.ReadByte();
                    byte messageType2;

                    switch (messageType)
                    {
                        case 0x03:
                            messageType2 = (byte)serialPort.ReadByte();
                            switch (messageType2)
                            {
                                case 0x01: messageQueue.Enqueue("ALT"); break;
                                case 0x02: messageQueue.Enqueue("TMP"); break;
                                case 0x03: messageQueue.Enqueue("PRS"); break;
                                default:
                                    DebugShortcut("FALSE Alarm: 0x03");
                                    break;
                            }
                            break;

                        case 0x04:
                            messageType2 = (byte)serialPort.ReadByte();
                            switch (messageType2)
                            {
                                case 0x01: messageQueue.Enqueue("ACC"); break;
                                case 0x02: messageQueue.Enqueue("ACL"); break;
                                case 0x03: messageQueue.Enqueue("GRV"); break;
                                default: DebugShortcut("FALSE Alarm: 0x04"); break;
                            }
                            break;

                        case 0x05: // Handle incoming servo commands
                            messageType2 = (byte)serialPort.ReadByte();
                            if (messageType2 == 0x01)
                            {
                                byte[] intBuffer1 = new byte[4];
                                byte[] intBuffer2 = new byte[4];

                                serialPort.Read(intBuffer1, 0, 4);
                                serialPort.Read(intBuffer2, 0, 4);

                                int servoNumber = System.BitConverter.ToInt32(intBuffer1, 0);
                                int angle = System.BitConverter.ToInt32(intBuffer2, 0);

                                servoQueue.Enqueue(new Tuple<int, int>(servoNumber, angle));

                                // ✅ Send confirmation byte back to ESP
                                byte[] confirmation = { 0x05 };
                                WriteSerialAsync(confirmation);
                            }
                            break;

                        default:
                            debugQueue.Enqueue((char)messageType + serialPort.ReadLine());
                            break;
                    }
                }
            }
            catch (System.Exception e)
            {
                DebugShortcut(e.ToString(), LogType.Error);
            }
            Thread.Sleep(1); // Small sleep to prevent tight looping
        }
    }

    void Update()
    {
        // Process sensor messages
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message, this, imu, gameObject);
        }

        // Process debug messages
        while (debugQueue.TryDequeue(out string message))
        {
            DebugShortcut("PYLD-SERIAL: " + message);
        }
        while (servoQueue.TryDequeue(out Tuple<int, int> servoData))
        {
            int servoNumber = servoData.Item1;
            int angle = servoData.Item2;

            DebugShortcut($"Servo {servoNumber} angle received: {angle}");
            payload.RotateTo(servoNumber, angle);
        }

    }

    public void ProcessMessage(string message, Payload25SerialController plsc, GameObject imuObject, GameObject rocketObject)
    {
        byte[] response;

        Rigidbody rocketRb = rocketObject.GetComponent<Rigidbody>(); // Get Rocket Rigidbody
        Transform imuTf = imuObject.transform; // Get IMU Transform

        // Compute acceleration
        Vector3 linearAcceleration = (rocketRb.velocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = rocketRb.velocity; // Update velocity for next frame

        switch (message)
        {
            case "ALT":
                float altitude = rocketObject.transform.position.y;
                if (UnityEngine.Random.value < 0.2f)
                {
                    float noise = altData.noise;
                    altitude += (UnityEngine.Random.value < 0.5f) ? noise : -noise;
                }
                if (UnityEngine.Random.value < altData.outlierChance) altitude += UnityEngine.Random.Range(100, 1000);
                response = new byte[5];
                response[0] = 0x03;
                Array.Copy(BitConverter.GetBytes(altitude), 0, response, 1, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "TMP":
                float temperature = 20f + UnityEngine.Random.Range(-2f, 2f);
                response = new byte[5];
                response[0] = 0x03;
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

            case "ACC": // Total acceleration (Linear Acceleration + Gravity)
                Vector3 totalAcceleration = linearAcceleration + imuTf.rotation * Physics.gravity;
                totalAcceleration += new Vector3(
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise),
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise),
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise)
                );

                if (UnityEngine.Random.value < imuMods.outlierChance)
                {
                    totalAcceleration *= UnityEngine.Random.Range(2f, 5f); // Create a large spike in data
                }

                response = new byte[13];
                response[0] = 0x04;
                Array.Copy(BitConverter.GetBytes(totalAcceleration.x), 0, response, 1, 4);
                Array.Copy(BitConverter.GetBytes(totalAcceleration.y), 0, response, 5, 4);
                Array.Copy(BitConverter.GetBytes(totalAcceleration.z), 0, response, 9, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "ACL": // Linear acceleration (gravity removed)
                Vector3 noisyLinearAcc = linearAcceleration + new Vector3(
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise),
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise),
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise)
                );

                if (UnityEngine.Random.value < imuMods.outlierChance)
                {
                    noisyLinearAcc *= UnityEngine.Random.Range(2f, 5f);
                }

                response = new byte[13];
                response[0] = 0x04;
                Array.Copy(BitConverter.GetBytes(noisyLinearAcc.x), 0, response, 1, 4);
                Array.Copy(BitConverter.GetBytes(noisyLinearAcc.y), 0, response, 5, 4);
                Array.Copy(BitConverter.GetBytes(noisyLinearAcc.z), 0, response, 9, 4);
                plsc.WriteSerialAsync(response);
                break;

            case "GRV": // Gravity relative to IMU orientation
                Vector3 localGravity = imuTf.InverseTransformDirection(Physics.gravity) + new Vector3(
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise),
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise),
                    UnityEngine.Random.Range(-imuMods.noise, imuMods.noise)
                );

                if (UnityEngine.Random.value < imuMods.outlierChance)
                {
                    localGravity *= UnityEngine.Random.Range(2f, 5f);
                }

                response = new byte[13];
                response[0] = 0x04;
                Array.Copy(BitConverter.GetBytes(localGravity.x), 0, response, 1, 4);
                Array.Copy(BitConverter.GetBytes(localGravity.y), 0, response, 5, 4);
                Array.Copy(BitConverter.GetBytes(localGravity.z), 0, response, 9, 4);
                plsc.WriteSerialAsync(response);
                break;

            default:
                DebugShortcut("Unknown message type: " + message, LogType.Warning);
                break;
        }
    }
    public void SetAltimeterNoise(int noise)
    {
        altData.noise = noise;
        DebugShortcut($"Altimeter Noise Set to: +/- {altData.noise}");
    }
    public void SetAltimeterOutlierChance(float outlierChance)
    {
        altData.outlierChance = outlierChance;
        DebugShortcut($"Altimeter Noise Set to: +/- {altData.outlierChance}");
    }

    public void SetAltimeterNoise(float noise)
    {
        SetAltimeterNoise((int)noise);
    }

    public void SetAltimeterNoise(string noise)
    {
        if (int.TryParse(noise, out int parsedNoise))
        {
            SetAltimeterNoise(parsedNoise);
        }
        else
        {
            DebugShortcut($"Invalid altimeter noise value: {noise}", LogType.Error);
        }
    }

    public void SetAltimeterOutlierChance(string outlierChance)
    {
        if (float.TryParse(outlierChance, out float parsedChance))
        {
            if (parsedChance >= 0f && parsedChance <= 1f)
            {
                SetAltimeterOutlierChance(parsedChance);
            }
            else
            {
                DebugShortcut($"Altimeter outlier chance must be between 0 and 1: {outlierChance}", LogType.Error);
            }
        }
        else
        {
            DebugShortcut($"Invalid altimeter outlier chance value: {outlierChance}", LogType.Error);
        }
    }
    public void SetIMUNoise(float noise)
    {
        imuMods.noise = noise;
        DebugShortcut($"IMU Noise Set to: +/- {imuMods.noise}");
    }

    public void SetIMUOutlierChance(float chance)
    {
        if (chance >= 0f && chance <= 1f)
        {
            imuMods.outlierChance = chance;
            DebugShortcut($"IMU Outlier Chance Set to: {imuMods.outlierChance * 100}%");
        }
        else
        {
            DebugShortcut($"IMU Outlier Chance must be between 0 and 1: {chance}", LogType.Error);
        }
    }

    public void SetIMUNoise(string noise)
    {
        if (float.TryParse(noise, out float parsedNoise))
        {
            SetIMUNoise(parsedNoise);
        }
        else
        {
            DebugShortcut($"Invalid IMU noise value: {noise}", LogType.Error);
        }
    }

    public void SetIMUOutlierChance(string chance)
    {
        if (float.TryParse(chance, out float parsedChance))
        {
            SetIMUOutlierChance(parsedChance);
        }
        else
        {
            DebugShortcut($"Invalid IMU outlier chance value: {chance}", LogType.Error);
        }
    }


}
