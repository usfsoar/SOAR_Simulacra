using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Events;

public class Payload25Controller : MonoBehaviour
{
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
    [System.Serializable]
    public class Door
    {
        public int channel; // Unique channel number
        public Transform doorTransform;
        public float startingOffset;
        public Coroutine currentRoutine;
        public float currentAngle;
    }

    public Door[] doors; // No need to fix the size

    public float rotationSpeed = 30f; // Degrees per second

    public void RotateTo(int channel, int angle)
    {
        Door selectedDoor = doors.FirstOrDefault(d => d.channel == channel);

        if (selectedDoor == null)
        {
            Debug.LogError($"No door found for channel {channel}!");
            return;
        }

        if (angle > 44.5)
        {
            Debug.LogError("More angle than valid");
            return;
        }

        float targetAngle = selectedDoor.startingOffset - angle;

        // Ensure we start from the current rotation
        selectedDoor.currentAngle = selectedDoor.doorTransform.localRotation.eulerAngles.x;

        // Stop the existing coroutine if there's a new command
        if (selectedDoor.currentRoutine != null)
        {
            StopCoroutine(selectedDoor.currentRoutine);
        }

        // Start a new coroutine to smoothly rotate towards the new angle
        selectedDoor.currentRoutine = StartCoroutine(RotateDoor(selectedDoor, targetAngle));
    }

    private IEnumerator RotateDoor(Door door, float targetAngle)
    {
        while (Mathf.Abs(door.currentAngle - targetAngle) > 0.1f)
        {
            // Move smoothly towards the target
            door.currentAngle = Mathf.MoveTowards(door.currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            door.doorTransform.localRotation = Quaternion.Euler(door.currentAngle,
                                                                door.doorTransform.localRotation.eulerAngles.y,
                                                                door.doorTransform.localRotation.eulerAngles.z);
            yield return null;
        }

        // Ensure final angle is set precisely
        door.doorTransform.localRotation = Quaternion.Euler(targetAngle,
                                                            door.doorTransform.localRotation.eulerAngles.y,
                                                            door.doorTransform.localRotation.eulerAngles.z);
        door.currentRoutine = null;
    }
    public void SetMotorChannels(int ch1, int ch2, int ch3, int ch4)
    {
        doors[0].channel = ch1;
        doors[1].channel = ch2;
        doors[2].channel = ch3;
        doors[3].channel = ch4;
        DebugShortcut($"Channels: {doors[0].channel} , {doors[1].channel} , {doors[2].channel} , {doors[3].channel}");
    }
    public void SetMotorChannels(string channels)
    {
        // Split the string by commas
        string[] parts = channels.Split(',');

        // Ensure there are exactly 4 values
        if (parts.Length != 4)
        {
            DebugShortcut("Invalid channel format. Expected format: '1,3,4,5'", LogType.Error);
            return;
        }

        // Convert to integers safely
        int[] parsedChannels = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (!int.TryParse(parts[i].Trim(), out parsedChannels[i]))
            {
                DebugShortcut($"Invalid number in channels string: '{parts[i]}'", LogType.Error);
                return;
            }
        }

        // Call the main function with parsed values
        SetMotorChannels(parsedChannels[0], parsedChannels[1], parsedChannels[2], parsedChannels[3]);
    }

}
