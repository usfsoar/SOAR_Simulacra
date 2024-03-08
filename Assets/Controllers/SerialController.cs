using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SerialController : MonoBehaviour
{
    public string portName = "COM3";
    public int baudRate = 9600;
    private SerialPort serialPort;
    private bool isReading = false;
    private Thread readThread;
    public GameObject rocket;

    // ConcurrentQueue for thread-safe communication
    public static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    public static ConcurrentQueue<float> altitudeQueue = new ConcurrentQueue<float>();
    public static ConcurrentQueue<UIParams> uiParamsQueue = new ConcurrentQueue<UIParams>();

    void Start()
    {
        StartReading();
    }

    //private async void ReadSerial()
    //{
    //    while (isReading && serialPort != null && serialPort.IsOpen)
    //    {
    //        try
    //        {
    //            string message = serialPort.ReadLine();
    //            Debug.Log("Received: " + message);
    //            // Enqueue the message for processing on the main thread
    //            // messageQueue.Enqueue(message);
    //            if(message =="FAKE_ALT:GET")
    //            {
    //                await WriteSerialAsync("FAKE_ALT:100");
    //            }
    //        }
    //        catch (System.Exception e)
    //        {
    //            Debug.LogError(e.ToString());
    //        }
    //    }
    //}
private async void ReadSerialAsync()
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


public async Task WriteSerialAsync(byte[] buffer)
{
    if (serialPort != null && serialPort.IsOpen)
    {
        await serialPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
    }
    else
    {
        Debug.LogError("Serial port is not open. Cannot send message.");
    }
}



    public void StartReading()
    {
        if (serialPort == null)
        {
            serialPort = new SerialPort(portName, baudRate);
        }

        if (!serialPort.IsOpen)
        {
            serialPort.Open();
            isReading = true;
            Debug.Log("Serial reading started on " + portName);

            // Start the read thread
            readThread = new Thread(ReadSerialAsync);
            readThread.Priority = System.Threading.ThreadPriority.Highest;

            readThread.Start();

        }
    }

    public void StopReading()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            isReading = false;
            Debug.Log("Serial reading stopped");

            // Stop the read thread
            if (readThread != null && readThread.IsAlive)
            {
                readThread.Abort();
            }

        }
    }

    void OnDestroy()
    {
        StopReading();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(SerialController))]
public class SerialControllerEditor : Editor
{
    SerializedProperty portName;
    SerializedProperty baudRate;
    string messageToSend = "";
    void OnEnable()
    {
        portName = serializedObject.FindProperty("portName");
        baudRate = serializedObject.FindProperty("baudRate");
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();
        EditorGUILayout.PropertyField(portName);
        EditorGUILayout.PropertyField(baudRate);
        if (GUILayout.Button("Start Reading"))
        {
            ((SerialController)target).StartReading();
        }
        if (GUILayout.Button("Stop Reading"))
        {
            ((SerialController)target).StopReading();
        }
        EditorGUILayout.Space();
        messageToSend = EditorGUILayout.TextField("Message", messageToSend);
        // if (GUILayout.Button("Send Message"))
        // {
        //     ((SerialController)target).WriteSerialAsync(messageToSend);
        // }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif