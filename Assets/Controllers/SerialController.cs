using System.Collections;
using System.Collections.Generic;

using UnityEngine.Events;
using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif
[System.Serializable] // Make the struct visible in the Inspector
public struct LogData
{
    public string message;
    public LogType logType;
}
public enum LogType
{
    Default,
    Warning,
    Error
}
public class SerialController : MonoBehaviour
{

    private Thread readThread;
    [SerializeField] public string portName = "COM3";
    [SerializeField] public int baudRate = 9600;
    protected SerialPort serialPort;
    protected bool isReading = false;
    public bool beginOnStart = false;
    // Start is called before the first frame update
    public UnityEvent<LogData> DebugLogEvent;

    public void DebugShortcut(string msg, LogType logType = LogType.Default)
    {
        switch (logType)
        {
            case LogType.Default:
                Debug.Log(msg);
                break;
            case LogType.Warning:
                Debug.Log(msg);
                break;
            case LogType.Error:
                Debug.Log(msg);
                break;
            default:
                Debug.Log(msg);
                break;
        }
        DebugLogEvent.Invoke(new LogData { message = msg, logType = logType });
    }
    void Start()
    {
        if (beginOnStart)
        {
            StartReading();
        }
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
            DebugShortcut("Serial port is not open. Cannot send message.", LogType.Error);
        }
    }
    public void StartReading()
    {
        try
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                DebugShortcut($"Closing previous serial port: {serialPort.PortName}");
                serialPort.Close();
                serialPort.Dispose();
            }

            DebugShortcut($"Attempting to open port: {portName}");

            serialPort = new SerialPort(portName, baudRate);

            if (!serialPort.IsOpen)
            {
                serialPort.Open();
                isReading = true;
                DebugShortcut($"Serial reading started on {portName}");

                readThread = new Thread(ReadSerialAsync);
                readThread.Priority = System.Threading.ThreadPriority.Highest;
                readThread.Start();
            }
        }
        catch (Exception e)
        {
            DebugShortcut(e.ToString(), LogType.Error);
        }
    }


    public void StopReading()
    {
        if (serialPort != null)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
            serialPort.Dispose();
            serialPort = null;
            isReading = false;
            DebugShortcut("Serial reading stopped");

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
    public void SetPort(string newPort)
    {
        portName = newPort;
        DebugShortcut($"New Port: {portName}");
    }
    public void SetBaudRate(int newBaudRate)
    {
        baudRate = newBaudRate;
        DebugShortcut($"New Baud Rate: {baudRate}");
    }
    public void SetBaudRate(string newBaudRate)
    {
        SetBaudRate(int.Parse(newBaudRate));
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
