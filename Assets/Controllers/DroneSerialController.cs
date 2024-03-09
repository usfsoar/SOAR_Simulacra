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
    public static ConcurrentQueue<float> altitudeQueue = new ConcurrentQueue<float>();
    public static ConcurrentQueue<UIParams> uiParamsQueue = new ConcurrentQueue<UIParams>();

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
                            messageQueue.Enqueue("FAKE_ALT:GET");
                            break;

                        case 0x03: // Altitude data message
                            //confirm the message
                            if ((byte)serialPort.ReadByte() != 0x03)
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
                                }
                            }
                            break;

                        case 0x04: // DC Motor Move Command
                            //Check for the second 0x04 byte confirming the message
                            if ((byte)serialPort.ReadByte() != 0x04)
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
                        case 0x05: //Distance sensor request
                            //Confirm the message
                            if ((byte)serialPort.ReadByte() != 0x05)
                            {
                                Debug.LogWarning("Invalid distance sensor message");
                                break;
                            }
                            //Enqueue the message for processing on the main thread
                            messageQueue.Enqueue("DISTANCE_SENSOR:GET");
                            break;
                        case 0x07:
                            //Confirm the message
                            byte confirmByte2 = (byte)serialPort.ReadByte();
                            if (confirmByte2 != 0x07)
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
