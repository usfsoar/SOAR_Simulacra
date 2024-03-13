using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DroneSerialController : SerialController
{
    

    // ConcurrentQueue for thread-safe communication
    public static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    public static ConcurrentQueue<UIParams> uiParamsQueue = new ConcurrentQueue<UIParams>();

    public static ConcurrentQueue<float> altitudeQueue = new ConcurrentQueue<float>();

    public static ConcurrentQueue<float> accelerationQueue = new ConcurrentQueue<float>();

    public static ConcurrentQueue<float> ang_accelerationQueue = new ConcurrentQueue<float>();


    public override async void ReadSerialAsync()
    {
        while (isReading && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    byte messageType = (byte)serialPort.ReadByte();
                    byte[] buffer;
                    switch (messageType)
                    {
                        case 0x01: // Request code for altitude
                            if ((byte)serialPort.ReadByte() != 0x01)
                            {
                                Debug.LogWarning("Invalid altitude message");
                                break;
                            }
                            messageQueue.Enqueue("FAKE_ALT:GET");
                            break;

                        case 0x02: // Altitude data message
                            //confirm the message
                            if ((byte)serialPort.ReadByte() != 0x02)
                            {
                                Debug.LogWarning("Invalid altitude message");
                                break;
                            }
                            if (serialPort.BytesToRead >= 13) // Check if enough bytes are available
                            {
                                buffer = new byte[13];
                                int bytesRead = serialPort.Read(buffer, 0, buffer.Length);
                                if (bytesRead == 13)
                                {
                                    float altitude = BitConverter.ToSingle(buffer, 0);
                                    float maxAltitude = BitConverter.ToSingle(buffer, 4);
                                    int state = BitConverter.ToInt32(buffer, 8);
                                    bool outlier = buffer[12] != 0; // Convert byte to bool

                                    uiParamsQueue.Enqueue(new UIParams
                                    {
                                        altitude = altitude,
                                        maxAltitude = maxAltitude,
                                        state = state.ToString(),
                                        outlier = outlier
                                    });

                                    altitudeQueue.Enqueue(new float
                                    {
                                        altitude = altitude,
                                        maxAltitude = maxAltitude,
                                        state = state.ToString(),
                                        outlier = outlier
                                    });
                                    
                                    Debug.log("Altitude: " + altitude + "Max Altitude: " + maxAltitude + "State: " + state + "Outlier: " + outlier);
                                }
                            }
                            break;

                        case 0x03: // Request code for acceleration
                            if ((byte)serialPort.ReadByte() != 0x03)
                            {
                                Debug.LogWarning("Invalid acceleration message");
                                break;
                            }
                            messageQueue.Enqueue("FAKE_ACC:GET");
                            break;

                        case 0x04: // Acceleration data message
                            //confirm the message
                            if ((byte)serialPort.ReadByte() != 0x04)
                            {
                                Debug.LogWarning("Invalid acceleration message");
                                break;
                            }
                            if (serialPort.BytesToRead >= 13) // Check if enough bytes are available
                            {
                                buffer = new byte[13];
                                int bytesRead = serialPort.Read(buffer, 0, buffer.Length);
                                if (bytesRead == 13)
                                {
                                    float altitude = BitConverter.ToSingle(buffer, 0);
                                    float maxAltitude = BitConverter.ToSingle(buffer, 4);
                                    int state = BitConverter.ToInt32(buffer, 8);
                                    bool outlier = buffer[12] != 0; // Convert byte to bool

                                    uiParamsQueue.Enqueue(new UIParams
                                    {
                                        altitude = altitude,
                                        maxAltitude = maxAltitude,
                                        state = state.ToString(),
                                        outlier = outlier
                                    });

                                    accelerationQueue.Enqueue(new float
                                    {
                                        altitude = altitude,
                                        maxAltitude = maxAltitude,
                                        state = state.ToString(),
                                        outlier = outlier
                                    });
                                }
                            }
                            break;

                        case 0x03: //Request code for distance
                            //Confirm the message
                            if ((byte)serialPort.ReadByte() != 0x03)
                            {
                                Debug.LogWarning("Invalid distance sensor message");
                                break;
                            }
                            //Enqueue the message for processing on the main thread
                            messageQueue.Enqueue("DISTANCE_SENSOR:GET");
                            break;

                        case 0x04://Distance data message
                            //Confirm the message
                            byte confirmByte2 = (byte)serialPort.ReadByte();
                            if (confirmByte2 != 0x04)
                            {
                                Debug.LogWarning("Invalid distance message");
                                break;
                            }
                            //Get the distance from 2 bytes
                            buffer = new byte[2];
                            serialPort.Read(buffer, 0, buffer.Length);
                            int distance = BitConverter.ToInt16(buffer, 0);
                            Debug.Log("Distance: " + distance);
                            break;

                        case 0x05: // DC Motor Move Command
                            //Check for the second 0x04 byte confirming the message
                            if ((byte)serialPort.ReadByte() != 0x05)
                            {
                                Debug.LogWarning("Invalid DC Motor message");
                                break;
                            }
                            //Get the direction
                            buffer = new byte[4];
                            serialPort.Read(buffer, 0, buffer.Length);
                            int direction = BitConverter.ToInt32(buffer, 0);
                            messageQueue.Enqueue("DC_MOTOR:" + direction);
                            break;
                        default:
                            //Assume string message
                            // Debug.Log("SERIAL: " + serialPort.ReadLine());
                            break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }
            Thread.Sleep(1); // Small sleep to prevent tight looping
        }
    }
    
}
