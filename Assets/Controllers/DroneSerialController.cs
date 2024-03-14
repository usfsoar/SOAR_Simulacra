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

    public static ConcurrentQueue<UIDroneParams> uiParamsQueue = new ConcurrentQueue<UIDroneParams>();

    public static ConcurrentQueue<string> altitudeQueue = new ConcurrentQueue<string>();

    public static ConcurrentQueue<string> velocityQueue = new ConcurrentQueue<string>();


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

                                    uiParamsQueue.Enqueue(new UIDroneParams
                                    {
                                        altitude = altitude,
                                        ETState = state.ToString(),
                                        outlier = outlier
                                    });

                                    altitudeQueue.Enqueue("GET");
                                    
                                    Debug.Log("Altitude: " + altitude + "Max Altitude: " + maxAltitude + "State: " + state + "Outlier: " + outlier);
                                }
                            }
                            break;

                        case 0x03: // Request code for velocity
                            if ((byte)serialPort.ReadByte() != 0x03)
                            {
                                Debug.LogWarning("Invalid velocity message");
                                break;
                            }
                            velocityQueue.Enqueue("GET");
                            break;

                        case 0x04: // Velocity data message
                            //confirm the message
                            if ((byte)serialPort.ReadByte() != 0x04)
                            {
                                Debug.LogWarning("Invalid velocity message");
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
                                        velocity = velocity,
                                        state = state.ToString(),
                                        outlier = outlier
                                    });

                                    velocityQueue.Enqueue(new float
                                    {
                                        velocity = velocity,
                                        state = state.ToString(),
                                        outlier = outlier
                                    });

                                    Debug.log("Velocity: " + velocity + "State: " + state + "Outlier: " + outlier);
                                }
                            }
                            break;

                        case 0x10: //Request code for distance
                            //Confirm the message
                            if ((byte)serialPort.ReadByte() != 0x10)
                            {
                                Debug.LogWarning("Invalid distance sensor message");
                                break;
                            }
                            //Enqueue the message for processing on the main thread
                            messageQueue.Enqueue("DISTANCE_SENSOR:GET");
                            break;

                        case 0x11://Distance data message
                            //Confirm the message
                            byte confirmByte2 = (byte)serialPort.ReadByte();
                            if (confirmByte2 != 0x11)
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

                        case 0x15: // DC Motor Move Command
                            //Check for the second 0x04 byte confirming the message
                            if ((byte)serialPort.ReadByte() != 0x15)
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
