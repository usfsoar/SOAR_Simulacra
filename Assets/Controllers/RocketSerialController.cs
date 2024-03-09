using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RocketSerialController : SerialController
{
    // ConcurrentQueue for thread-safe communication
    public static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    public static ConcurrentQueue<float> altitudeQueue = new ConcurrentQueue<float>();
    public static ConcurrentQueue<UIParams> uiParamsQueue = new ConcurrentQueue<UIParams>();
    public RocketLoraController loraController;

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
                        case 0x08:
                            //Confirm the message
                            if((byte)serialPort.ReadByte() != 0x08)
                            {
                                Debug.LogWarning("Invalid lora available message");
                                break;
                            }
                            //Check the lora queue from the lora controller
                            bool res = loraController.CheckLoraQueue();
                            //Send the response with the code 0x09
                            byte[] response = new byte[2];
                            response[0] = 0x09;
                            response[1] = (byte)(res ? 0x01 : 0x00);
                            await WriteSerialAsync(response);
                            break;
                        case 0x10: //Get lora message from lora queue
                            //Confirm the message
                            messageType2 = (byte)serialPort.ReadByte();
                            switch(messageType2){
                                case 0x01: 
                                    string lora_res = loraController.GetLoraMessage();
                                    Debug.Log("LORA_SENT: " + lora_res);
                                    //Send the response with code 0x11
                                    byte[] lora_response = new byte[2 + lora_res.Length];
                                    lora_response[0] = 0x11;
                                    //Next byte is the length of the message
                                    lora_response[1] = (byte)lora_res.Length;
                                    byte[] lora_res_bytes = System.Text.Encoding.ASCII.GetBytes(lora_res);
                                    Array.Copy(lora_res_bytes, 0, lora_response, 2, lora_res_bytes.Length);
                                    await WriteSerialAsync(lora_response);
                                    break;
                                case 0x02: //incoming lora response
                                    //Next byte is the length of the message
                                    int lora_length = (byte)serialPort.ReadByte();
                                    buffer = new byte[lora_length];
                                    serialPort.Read(buffer, 0, buffer.Length);
                                    string lora_message = System.Text.Encoding.ASCII.GetString(buffer);
                                    Debug.Log("LORA_RES: " + lora_message);
                                    break;
                                default:
                                    Debug.LogWarning("Invalid lora message request");
                                    break;
                            }
                            //Get message from the lora queue, assume it's not empty
                            
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
