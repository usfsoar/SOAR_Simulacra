using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;



public class Canvas25Controller : MonoBehaviour
{
    public GameObject logEntryPrefab;
    public Transform contentParent;
    public ScrollRect scrollObj;

    public Color defaultColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color errorColor = Color.red;
    public void ReloadScene(){
        GameManager.Instance.ReloadScene();
    }

    public void AddLog(LogData logData)
    {
        // Instantiate a new log entry from the prefab
        GameObject newLogEntry = Instantiate(logEntryPrefab, contentParent);

        // Set the text of the log entry
        TextMeshProUGUI logText = newLogEntry.GetComponent<TextMeshProUGUI>(); // Use TextMeshProUGUI if using TextMeshPro
        if (logText != null)
        {
            logText.text = logData.message;

            // Set the text color based on the log type
            switch (logData.logType)
            {
                case LogType.Warning:
                    logText.color = warningColor;
                    break;
                case LogType.Error:
                    logText.color = errorColor;
                    break;
                default:
                    logText.color = defaultColor;
                    break;
            }
        }
        else
        {
            Debug.LogError("Log entry prefab does not have a Text component!");
        }

        // Optional: Scroll tothe bottom of the ScrollView
        if (scrollObj != null)
        {
            StartCoroutine(ScrollToBottom(scrollObj));
        }
    }
    private IEnumerator ScrollToBottom(ScrollRect scrollRect)
    {
        yield return new WaitForEndOfFrame(); // Wait for UI to update
        scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom
    }


}
