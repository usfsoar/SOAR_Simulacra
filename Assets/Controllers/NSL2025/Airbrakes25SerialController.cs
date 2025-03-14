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

public class Airbrakes25SerialController : SerialController
{
    public bool alerted = false;

    public static ConcurrentQueue<string> debugQueue = new ConcurrentQueue<string>();
    void Update()
    {
        if (!alerted)
        {
            DebugShortcut("Airbrakes Serial Controller Not Implemented yet!", LogType.Warning);
            alerted = true;
        }

        while (debugQueue.TryDequeue(out string message))
        {
            DebugShortcut("AIRBRK-SERIAL: "+message);
        }
    }
    public override async void ReadSerialAsync()
    {
        while (isReading && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    byte messageType = (byte)serialPort.ReadByte();
                    // byte messageType2; //uncomment this once you start setting your protocol

                    switch (messageType)
                    {
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

}
