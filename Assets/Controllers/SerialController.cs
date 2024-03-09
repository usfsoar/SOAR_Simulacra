using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SerialController : MonoBehaviour
{

    private Thread readThread;
    public string portName = "COM3";
    public int baudRate = 9600;
    protected SerialPort serialPort;
    protected bool isReading = false;
    public bool beginOnStart = false;
    // Start is called before the first frame update
    void Start()
    {
        if(beginOnStart)
        {
            StartReading();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    /// private async void ReadSerialAsync() but as a virtual method
    public virtual void ReadSerialAsync()
    {
    }
    public virtual async Task WriteSerialAsync(byte[] buffer)
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
public class DroneSerialControllerEditor : Editor
{
    string messageToSend = "";
    
    public override async void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();
        if (GUILayout.Button("Start Reading"))
        {
            ((SerialController)target).StartReading();
        }
        if (GUILayout.Button("Stop Reading"))
        {
            ((SerialController)target).StopReading();
        }
        serializedObject.ApplyModifiedProperties();
        GUILayout.Space(10);

        GUILayout.Label("Send a message:");
        messageToSend = EditorGUILayout.TextField("Message", messageToSend);
        if (GUILayout.Button("Send Message"))
        {
            //Convert the string to a byte array
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(messageToSend);
            await ((RocketSerialController)target).WriteSerialAsync(buffer);
        }
    }
}
#endif
