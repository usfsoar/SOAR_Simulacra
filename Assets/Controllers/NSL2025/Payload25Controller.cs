using UnityEngine;
using System.Collections;
using System.Linq;

public class Payload25Controller : MonoBehaviour
{
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
}
