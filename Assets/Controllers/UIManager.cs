using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class UIParams
{
    public float altitude;
    public float maxAltitude;
    public string state;
    public bool outlier;
}
public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI altitudeValue;
    public TextMeshProUGUI stateValue;
    public TextMeshProUGUI maxAltitudeValue;
    public TextMeshProUGUI outlierValue;
    float lastOutlier = 0;

    void Update()
    {
        while (RocketSerialController.uiParamsQueue.TryDequeue(out UIParams uiParams))
        {
            UpdateAltitudeAndState(uiParams);
        }
    }

    public void UpdateAltitudeAndState(UIParams uiParams)
    {
        altitudeValue.text = uiParams.altitude.ToString();
        stateValue.text = uiParams.state;
        maxAltitudeValue.text = uiParams.maxAltitude.ToString();
        if(uiParams.outlier){
            lastOutlier = uiParams.altitude;
        }
        outlierValue.text = lastOutlier.ToString();
    }
}
