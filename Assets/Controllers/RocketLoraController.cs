using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RocketLoraController : MonoBehaviour
{

    public static ConcurrentQueue<string> loraMessageQueue = new ConcurrentQueue<string>();

    public void SendLoraMessage(string message)
    {
        //Add the message to the queue
        loraMessageQueue.Enqueue(message);
    }
    public bool CheckLoraQueue()
    {
        //Check if the queue is not empty
        return loraMessageQueue.Count > 0;
    }
    public string GetLoraMessage()
    {
        //Get the message from the queue
        loraMessageQueue.TryDequeue(out string message);
        return message;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(RocketLoraController))]
public class LoraControllerEditor : Editor
{
    string message = "PING";
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RocketLoraController loraController = (RocketLoraController)target;
        message = EditorGUILayout.TextField("Message", message);

        if (GUILayout.Button("Send Lora Message"))
        {
            loraController.SendLoraMessage(message);
        }   
    }
}
#endif